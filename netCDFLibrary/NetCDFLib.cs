using System.Diagnostics;
using System.Globalization;
using Microsoft.Research.Science.Data;
using netCDFLibrary.Data;

namespace netCDFLibrary
{
    public class NetCDFLib : IDisposable
    {
        private readonly DataSet dataSet;
        private readonly DateTime unixDate = new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public ReadOnlyDimensionList Dimensions => this.dataSet.Dimensions;
        public ReadOnlyVariableCollection Variables => this.dataSet.Variables;
        public NetCDFBoundaries GetBoundaries(string X = "", string Y = "")
        {
            Dimension XDim, YDim;
            if(string.IsNullOrEmpty(X) && string.IsNullOrEmpty(Y))
            {
                YDim = this.dataSet.Dimensions[^2];
                XDim = this.dataSet.Dimensions[^1];
            } else
            {
                YDim = this.dataSet.Dimensions[Y];
                XDim = this.dataSet.Dimensions[X];
            }
            var XIndex = this.Index<double>(XDim.Name);
            var YIndex = this.Index<double>(YDim.Name);

            // Y 의 Min Max는 반전 될 수 있기 때문에 먼저 시작과 끝을 비교한다.
            var IsYFlip = YIndex[0] < YIndex[YDim.Length - 1];
            return new NetCDFBoundaries(
                XIndex[0], XIndex[XDim.Length - 1],
                IsYFlip ? YIndex[0] : YIndex[YDim.Length - 1],
                IsYFlip ? YIndex[YDim.Length - 1] : YIndex[0],
                XDim.Length,
                YDim.Length,
                IsYFlip
            );
        }
        public NetCDFLib(string path)
        {
            this.dataSet = DataSet.Open($"{path}?openMode=readOnly");
            var Y = this.dataSet.Dimensions[^2];
            var X = this.dataSet.Dimensions[^1];

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
        public T[] Index<T>(string key)
        {
            if (this.dataSet == null)
            {
                return Array.Empty<T>();
            }
            if (!this.dataSet.Variables.Contains(key))
            {
                return Array.Empty<T>();
            }
            if (!this.Variables.Contains(key))
            {
                return Array.Empty<T>();
            }
            var variable = this.Variables[key];

            return variable.GetData().Cast<object>().Select(v => (T)Convert.ChangeType(v, typeof(T))).ToArray();
        }
        public NetCDFVariable this[string layer]
        {
            get
            {
                if (this.dataSet == null)
                {
                    throw new InvalidOperationException("dataset is null");
                }
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

        public DateTime[] TimeIndex(string timeKey = "time")
        {
            DateTime[] ret = Array.Empty<DateTime>();
            if (this.dataSet == null)
            {
                return ret;
            }

            var variables = this.Variables[timeKey];

            if (variables == null)
            {
                return ret;
            }
            var times = variables.TypeOfData.Name switch
            {
                "double" => (double[])variables.GetData(),
                "Int32" => variables.GetData().Cast<object>().Select(v => Convert.ToDouble(v)).ToArray(),
                _ => variables.GetData().Cast<object>().Select(v => Convert.ToDouble(v)).ToArray()
            };
            if (times == null)
            {
                return ret;
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
            if (variables.Metadata.ContainsKey("units"))
            {
                var units = (string)variables.Metadata["units"];
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
            return times.Select(time => unit switch
            {
                TimeUnit.Hours => referenceDate.AddHours(time),
                TimeUnit.Minutes => referenceDate.AddMinutes(time),
                TimeUnit.Seconds => referenceDate.AddSeconds(time),
                _ => referenceDate.AddHours(time),
            }).ToArray();
        }
        public void Dispose()
        {
            if (this.dataSet != null)
            {
                Debug.WriteLine("NetCDF DataSet Release");
                this.dataSet.Dispose();
            }
        }
    }
}