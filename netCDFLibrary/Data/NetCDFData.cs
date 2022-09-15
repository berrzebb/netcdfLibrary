using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using OpenCvSharp;
using Range = Microsoft.Research.Science.Data.Range;
using SystemRange = System.Range;
namespace netCDFLibrary.Data
{
    public record NetCDFBoundaries(double MinX, double MaxX, double MinY, double MaxY, double Width, double Height, bool isYFlip);

    public class CoordinateTransformatter
    {
        public Func<double, double, (double, double)> LL2XY { get; init; } = (x, y) => (x, y);
        public Func<double, double, (double, double)> XY2LL { get; init; } = (x, y) => (x, y);
    }
    public record Dim(string name, SystemRange range)
    {
        public static Dim Create(string name, SystemRange index) => new Dim(name, index);
        public static Dim Create(string name) => new(name, 0..1);
        public static Dim[] Create(params (string name, SystemRange range)[] name) => name.Select(v => new Dim(v.name, v.range)).ToArray();
        public static Dim[] Create(params string[] name) => name.Select(v => new Dim(v, SystemRange.StartAt(0))).ToArray();
        public static Dim[] Create(params (string name, int index)[] dim) => dim.Select(v => new Dim(v.name, new SystemRange(v.index, v.index))).ToArray();
    }
    public record StatisticsData(double MinValue, double MaxValue, double AvgValue)
    {
        public readonly static StatisticsData Empty = new StatisticsData(0, 0, 0);
        private Predicate<double> filter = v => true;
        public double MeanValue => (this.MaxValue + this.MinValue) / 2.0;

        public double Transform(double Value) => Value * (this.MaxValue - this.MinValue) + this.MinValue;
        public double Normalize(double Value) => (Value - this.MinValue) / (this.MaxValue - this.MinValue);
        public double[] Normalize(double[] values, double ignoreValue = 0) => values.Select(v => !this.filter(v) || v == ignoreValue ? 0 : this.Normalize(v)).ToArray();
        public static StatisticsData Create(double[] values, Predicate<double> Filter)
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            double SumValue = 0;
            double Count = 0;
            foreach (double value in values)
            {
                if (Filter(value))
                {
                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);
                    SumValue += value;
                    Count++;
                }
            }
            double avgValue = SumValue / Count;
            return new StatisticsData(minValue, maxValue, avgValue)
            {
                filter = Filter
            };
        }
        public static StatisticsData Create(double minValue, double maxValue, double avgValue) => new StatisticsData(minValue, maxValue, avgValue);
    }

    public record NetCDFPrimitiveData(double minX, double minY, double maxX, double maxY, int width, int height, DataScale dataScale, bool isYFlip, double[] values, StatisticsData Statistics)
    {
        public static readonly NetCDFPrimitiveData Empty = new(0, 0, 0, 0, 0, 0, new DataScale(),false, Array.Empty<double>(), StatisticsData.Empty);

        private CoordinateScaler scaler = CoordinateScaler.Empty;
        public double XGap => this.scaler.XGap;
        public double YGap => this.scaler.YGap;
        public (double, double) GetOffset(int row, int col) => this.scaler.GetOffset(row, col);
        public (int, int) FindIndex(double Y, double X) => this.scaler.FindIndex(Y, X, this.isYFlip);
        private int GetIndex(int row, int col) => col + (row * this.width);
        public static NetCDFPrimitiveData Create(double minX, double minY, double maxX, double maxY, int width, int height, DataScale dataScale, bool isYFlip, double[] values, StatisticsData statisticsData)
        {
            var ret = new NetCDFPrimitiveData(minX, minY, maxX, maxY, width, height, dataScale,isYFlip, values, statisticsData);
            ret.scaler = new CoordinateScaler(minX, maxX, minY, maxY, width, height);
            return ret;
        }

        public double this[int row, int col]
        {
            get
            {
                if (row > this.height || col > this.width || row < 0 || col < 0 || this.values.Length == 0 || this.values == null)
                {
                    return 0.0;
                }

                return this.values[col + (row * this.width)];
            }
        }
        public void ForEach(Action<int, int, int, double> act)
        {
            for (int row = 0; row < this.height; row++)
            {
                for (int col = 0; col < this.width; col++)
                {
                    var index = this.GetIndex(row, col);
                    act(row, col, index, this.values[index]);
                }
            }
        }
        public void Transform(Func<int, int, int, double, double> transform)
        {
            for (int row = 0; row < this.height; row++)
            {
                for (int col = 0; col < this.width; col++)
                {
                    var index = this.GetIndex(row, col);
                    this.values[index] = transform(row, col, index, this.values[index]);
                }
            }
        }
        public (double, double) Magnitude(ref NetCDFPrimitiveData V, int index)
        {
            var u = this.values[index];
            var v = V.values[index];
            if (double.IsNaN(u) || double.IsNaN(v))
            {
                return (0, 0);
            }

            return (((Math.Atan2(v, u) * (180 / Math.PI)) + 450) % 360.0, Math.Sqrt(Math.Pow(u, 2) + Math.Pow(v, 2)));
        }
        public (double, double) Magnitude(ref NetCDFPrimitiveData V, int row, int col)
        {
            var u = this[row, col];
            var v = V[row, col];
            return (((Math.Atan2(v, u) * (180 / Math.PI)) + 450) % 360.0, Math.Sqrt(Math.Pow(u, 2) + Math.Pow(v, 2)));
        }
        public (double, double, double) MagnitudeNormalized(ref NetCDFPrimitiveData V, int row, int col, double referenceValue)
        {
            var (dir, spd) = this.Magnitude(ref V, row, col);
            return (dir, spd, spd / referenceValue);
        }
    }
    public class NetCDFData {
        public SystemRange Rows { get; protected set; }
        public SystemRange Cols { get; protected set; }

        public bool IsYFlip { get; protected set; }
        public double MinX => this.boundaries.MinX;
        public double MinY => this.boundaries.MinY;
        public double MaxX => this.boundaries.MaxX;
        public double MaxY => this.boundaries.MaxY;

        protected Dim[] dims;
        protected (string, Range, int)[] Dimensions;
        private readonly NetCDFVariable? source;
        private readonly NetCDFBoundaries boundaries = new NetCDFBoundaries(0,0,0,0, 0,0, false);
        public NetCDFData(NetCDFVariable? variable, params Dim[] dims)
        {
            if (variable == null)
            {
                throw new InvalidOperationException("source null");
            }
            this.source = variable;

            this.dims = dims;
            this.Dimensions = dims.Select((v, i) =>
            {
                int Offset = 0;
                int Length = 0;
                if(v.range.Start.Value != 0 && v.range.End.Value != 0)
                {
                    if (v.range.Start.Value == v.range.End.Value)
                    {
                        (Offset, Length) = v.range.GetOffsetAndLength(this.source.Shape[i]);
                        if(Length == 0)
                        {
                            return (v.name, DataSet.Range(Offset), 1);
                        } else
                        {
                            return (v.name, DataSet.Range(Offset, Length - 1), Length);
                        }
                    } else
                    {
                        Offset = v.range.Start.Value;
                        Length = v.range.End.Value;
                        return (v.name, DataSet.Range(Offset, Length - 1), Length - Offset);
                    }
                }
                (Offset, Length) = v.range.GetOffsetAndLength(this.source.Shape[i]);
                if(Length == 0)
                {
                    return (v.name, DataSet.Range(Offset), 1);
                } else
                {
                    return (v.name, DataSet.Range(Offset, Length - 1), Length);
                }
            }).ToArray();

            var (YName, YRange, YCount) = this.Dimensions[^2];
            var (XName, XRange, XCount) = this.Dimensions[^1];

            var YIndex = variable.source.Index<double>(YName)[dims[^2].range];
            var XIndex = variable.source.Index<double>(XName)[dims[^1].range];

            // Y 의 Min Max는 반전 될 수 있기 때문에 먼저 시작과 끝을 비교한다.
            this.IsYFlip = YIndex[0] < YIndex[YCount - 1];
            this.boundaries = new NetCDFBoundaries(
                XIndex[0], XIndex[XCount - 1],
                this.IsYFlip ? YIndex[0] : YIndex[YCount - 1],
            this.IsYFlip ? YIndex[YCount - 1] : YIndex[0],
            XCount,
            YCount,
            this.IsYFlip
            );
        }
        public static implicit operator NetCDFPrimitiveData(NetCDFData data) => data.ToPrimitive();
        private NetCDFPrimitiveData ToPrimitive()
        {
            if (this.source == null)
            {
                return NetCDFPrimitiveData.Empty;
            }
            var dataScale = this.source.DataScale;
            int RowLength = this.Dimensions[^2].Item3;
            int ColLength = this.Dimensions[^1].Item3;
            var data = this.source.DataSet.GetData<Array>(this.source.VariableId, this.Dimensions.Select(v => v.Item2).ToArray());
            Mat src = new(RowLength, ColLength, MatType.CV_16S, data);
            Mat dest = new Mat();
            src.ConvertTo(dest, MatType.CV_64F, dataScale.ScaleFactor, dataScale.AddOffset);
            var MissingValue = (dataScale.MissingValue * dataScale.ScaleFactor) + dataScale.AddOffset;
            for(int row = 0; row < RowLength; row++)
            {
                for(int col = 0; col < ColLength; col++)
                {
                    var v = dest.Get<double>(row, col);
                    if(v == MissingValue)
                    {
                        dest.Set(row, col, double.NaN);
                    }
                }
            }

            dest.MinMaxIdx(out double min, out double max);
            var mean = dest.Mean();
            dest.GetArray(out double[] values);

            var statistics = StatisticsData.Create(min, max, mean.ToDouble());

            return NetCDFPrimitiveData.Create(this.MinX, this.MinY, this.MaxX, this.MaxY,  ColLength, RowLength, dataScale, this.IsYFlip, values, statistics);
        }
        public void Deconstruct(out NetCDFPrimitiveData primitiveData)
        {
            primitiveData = this.ToPrimitive();
        }
    }
    public class NetCDFVariable : IDisposable
    {
        internal readonly NetCDFLib source;
        internal readonly Variable variable;
        public DataScale DataScale { get; }
        public int[] Shape => this.variable.GetShape();
        public int VariableId => this.variable.ID;
        public DataSet DataSet => this.variable.DataSet;
        public ReadOnlyDimensionList Dimensions => this.variable.Dimensions;
        public string? Name => this.DataScale.Name;
        public string? LongName => this.DataScale.LongName;
        public string? Units => this.DataScale.Units;
        public NetCDFVariable(NetCDFLib source, Variable variable) : this(source, variable, new DataScale(variable)) { }
        public NetCDFVariable(NetCDFLib source, Variable variable, DataScale dataScale)
        {
            this.source = source;
            this.variable = variable;
            this.DataScale = dataScale;
        }

        public NetCDFData this[params Dim[] dims]
        {
            get => new NetCDFData(this, dims);
        }

        public void Dispose()
        {
            this.variable.Dispose();
        }
    }
}
