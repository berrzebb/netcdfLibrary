using System.Globalization;
using Microsoft.Research.Science.Data;

using Microsoft.Research.Science.Data.Utilities;
using netCDFLibrary.Data;
using netCDFLibrary.Extensions;

namespace netCDFLibrary
{
    public class NetCDFLib : IDisposable
    {
        private readonly static object[] Empty = Array.Empty<object>();
        private readonly DataSet dataSet;
        private readonly DateTime unixDate = new(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public ReadOnlyDimensionList Dimensions => this.dataSet.Dimensions;
        public ReadOnlyVariableCollection Variables => this.dataSet.Variables;

        public NetCDFBoundaries GetBoundaries(Dim X, Dim Y, Dictionary<string, string>? lookup = null)
        {
            if (!this.Dimensions.Contains(X.name) || !this.Dimensions.Contains(Y.name))
            {
                return NetCDFBoundaries.Empty;
            }

            var XIndex = this.Dimensions[X.name];
            var YIndex = this.Dimensions[Y.name];
                        

            var XDim = DataSetDim.Create(X.name, X.range.Convert(XIndex.Length));
            var YDim = DataSetDim.Create(Y.name, Y.range.Convert(YIndex.Length));
            return this.GetBoundaries(XDim, YDim, lookup);
        }
        public NetCDFBoundaries GetBoundaries(DataSetDim X, DataSetDim Y, Dictionary<string, string>? lookup = null)
        {
            object[]? YIndex = Array.Empty<object>();
            object[]? XIndex = Array.Empty<object>();
            if (lookup != null)
            {
                var xName = lookup.ContainsKey(X.name) ? lookup[X.name] : X.name;
                var yName = lookup.ContainsKey(Y.name) ? lookup[Y.name] : Y.name;

                Variable? XVariable = null;
                Variable? YVariable = null;
                if (this.Variables.Contains(xName))
                {
                    XVariable = this.Variables[xName];
                }
                if (this.Variables.Contains(yName))
                {
                    YVariable = this.Variables[yName];
                }
                // Lookup에 해당하는 참조 변수가 없다면 비어 있는 바운더리 반환
                if(XVariable == null || YVariable == null)
                {
                    return NetCDFBoundaries.Empty;
                }
                // ROMS 데이터는 수직으로 같은 값 수평으로 같은 값을 읽어 오도록 구성 되어 있음.

                var XArr = XVariable.GetData(new[] { 0, X.range.Origin }, new[] { 1, X.range.Count });
                XIndex = XArr.Cast<object>().ToArray();
                var YArr = YVariable.GetData(new[] { Y.range.Origin, 0 }, new[] { Y.range.Count, 1 });
                YIndex = YArr.Cast<object>().ToArray();
            }
            else
            {
                YIndex = this.Index(Y);
                XIndex = this.Index(X);
            }


            // Y 의 Min Max는 반전 될 수 있기 때문에 먼저 시작과 끝을 비교한다.
            var MinY = (double)Convert.ChangeType(YIndex[0], typeof(double));
            var MaxY = (double)Convert.ChangeType(YIndex[Y.Length - 1], typeof(double));
            var MinX = (double)Convert.ChangeType(XIndex[0], typeof(double));
            var MaxX = (double)Convert.ChangeType(XIndex[X.Length - 1], typeof(double));

            var IsYFlip = MinY > MaxY;
            return new NetCDFBoundaries(
                MinX, MaxX,
                Math.Min(MinY, MaxY),
                Math.Max(MinY, MaxY),
                X.Length,
                Y.Length,
                IsYFlip
            );
        }
        public NetCDFLib(string path)
        {
            this.dataSet = DataSet.Open($"{path}?openMode=readOnly");
        }
        public void ShowMetadata()
        {
            if (this.dataSet == null)
            {
                return;
            }

            foreach (var metadata in this.dataSet.Metadata)
            {
                Console.WriteLine(metadata);
            }
        }
        public object[] Index(string key)
        {
            return this.Index(key, ..);
        }
        public object[] Index(DataSetDim dim)
        {
            if (this.dataSet == null || !this.dataSet.Variables.Contains(dim.name) || !this.Variables.Contains(dim.name))
            {
                return Empty;
            }
            var variable = this.Variables[dim.name];

            return variable.GetData(new[] { dim.range.Origin }, new[] { dim.range.Count }).Cast<object>().ToArray();
        }
        public object[] Index(string key, System.Range range)
        {
            if (this.dataSet == null || !this.dataSet.Variables.Contains(key) || !this.Variables.Contains(key) || !this.Dimensions.Contains(key))
            {
                return Empty;
            }
            var variable = this.Variables[key];
            var dimension = this.Dimensions[key];
            var (r, l) = range.Convert(dimension.Length);

            return variable.GetData(new[] { r.Origin }, new[] { r.Count }).Cast<object>().ToArray();
        }
        public NetCDFVariable this[string layer]
        {
            get
            {
                if (this.dataSet == null)
                {
                    throw new InvalidOperationException("dataset is null");
                }

                var item = this.dataSet.Variables[layer];
                return new NetCDFVariable(this, this.Variables[layer]);
            }
        }
        public bool Contains(string name) {
            if (this.dataSet == null)
            {
                return false;
            }

            return this.Variables.Contains(name);
        }

        public TimeIndexer? TimeIndex(string timeKey = "time")
        {
            if (this.dataSet == null)
            {
                return TimeIndexer.Empty;
            }

            var variables = this.Variables[timeKey];

            if (variables == null)
            {
                return TimeIndexer.Empty;                
            }
            var times = variables.TypeOfData.Name switch
            {
                "Double" => (double[])variables.GetData(),
                "Int32" => ((int[])variables.GetData()).Select(v => Convert.ToDouble(v) ).ToArray(),
                _ => ((object[])variables.GetData()).Select(v => Convert.ToDouble(v)).ToArray()
            };
            if (times == null)
            {
                return TimeIndexer.Empty;
            }
            Calendar calendar = new GregorianCalendar();
            TimeUnit unit = TimeUnit.Hours;
            DateTime referenceDate = DateTime.Now;
            if (variables.Metadata.ContainsKey("calendar"))
            {
                var calendarType = variables.Metadata["calendar"];
                calendar = calendarType switch
                {
                    "julian" => new JulianCalendar(),
                    "gregorian" => new GregorianCalendar(),
                    "proleptic_gregorian" => new GregorianCalendar(),
                    _ => new GregorianCalendar(),
                };
            }
            var units = variables.Metadata.GetUnits();
            if (!string.IsNullOrEmpty(units))
            {
                if (units.Contains(" since "))
                {
                    var item = units.Split(" since ");
                    unit = item[0] switch
                    {
                        "hours" => TimeUnit.Hours,
                        "minutes" => TimeUnit.Minutes,
                        "seconds" => TimeUnit.Seconds,
                        _ => TimeUnit.Hours
                    };
                    if (DateTime.TryParse(item[1], out referenceDate))
                    {
                        referenceDate = new DateTime(referenceDate.Year, referenceDate.Month, referenceDate.Day, calendar);
                    }
                }
            }
            return TimeIndexer.Create(times.Select(time => unit switch
            {
                TimeUnit.Hours => referenceDate.AddHours(time),
                TimeUnit.Minutes => referenceDate.AddMinutes(time),
                TimeUnit.Seconds => referenceDate.AddSeconds(time),
                _ => referenceDate.AddHours(time),
            }).ToArray());
        }
        public void Dispose()
        {
            if (this.dataSet != null)
            {
                //Debug.WriteLine("NetCDF DataSet Release");
                this.dataSet.Dispose();
            }
        }
    }
}