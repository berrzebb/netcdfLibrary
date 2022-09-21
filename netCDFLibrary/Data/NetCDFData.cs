using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using Microsoft.Research.Science.Data.Utilities;
using netCDFLibrary.Extensions;
using OpenCvSharp;
using Range = Microsoft.Research.Science.Data.Range;
using SystemRange = System.Range;
namespace netCDFLibrary.Data
{
    public record NetCDFBoundaries(double MinX, double MaxX, double MinY, double MaxY, double Width, double Height, bool isYFlip)
    {
        public readonly static NetCDFBoundaries Empty = new NetCDFBoundaries(0, 0, 0, 0, 0, 0, false);
    }

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
    public record DataSetDim(string name, Range range, int Length)
    {
        public static DataSetDim Create(string name, Range range, int length) => new DataSetDim(name, range, length);
        public static DataSetDim Create(string name, (Range range, int length) v) => new DataSetDim(name, v.range, v.length);

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
        public double this[(int row, int col) index]
        {
            get
            {
                return this[index.row, index.col];
            }
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
        protected Dictionary<string, string>? lookup;
        protected DataSetDim[] Dimensions;
        private readonly NetCDFVariable? source;
        private readonly NetCDFBoundaries boundaries = new NetCDFBoundaries(0,0,0,0, 0,0, false);
        public NetCDFData(NetCDFVariable? variable, Dictionary<string, string>? lookup, params Dim[] dims)
        {
            if (variable == null)
            {
                throw new InvalidOperationException("source null");
            }
            this.lookup = lookup;
            this.source = variable;

            this.dims = dims;
            this.Dimensions = dims.Select((v, i) =>
            {
                var (range, length) = v.range.Convert(this.source.Shape[i]);
                return DataSetDim.Create(v.name, range, length);
            }).ToArray();

            var YDim = this.Dimensions[^2];
            var XDim = this.Dimensions[^1];


            // Y 의 Min Max는 반전 될 수 있기 때문에 먼저 시작과 끝을 비교한다.
            this.boundaries = variable.source.GetBoundaries(XDim, YDim, lookup);
            this.IsYFlip = this.boundaries.isYFlip;
        }
        public static implicit operator NetCDFPrimitiveData(NetCDFData data) => data.ToPrimitive();
        private NetCDFPrimitiveData ToPrimitive()
        {
            if (this.source == null)
            {
                return NetCDFPrimitiveData.Empty;
            }
            var dataScale = this.source.DataScale;
            int RowLength = this.Dimensions[^2].Length;
            int ColLength = this.Dimensions[^1].Length;

            var data = this.source.DataSet.GetData<Array>(this.source.VariableId, this.Dimensions.Select(v => v.range).ToArray());
            MatType type = MatType.CV_16S;
            switch (Type.GetTypeCode(this.source.variable.TypeOfData))
            {
                case TypeCode.Single:
                    type = MatType.CV_32F;
                    break;
                case TypeCode.Double:
                    type = MatType.CV_64F;
                    break;
                default:
                    type = MatType.CV_16S;
                    break;
            }
            Mat src = new(RowLength, ColLength, type, data);
            Mat dest = new Mat();
            double alpha = dataScale.ScaleFactor;
            double beta = dataScale.AddOffset;
            src.ConvertTo(dest, MatType.CV_64F, alpha, beta);
            object MissingValue = double.NaN;
            if (type == MatType.CV_16S)
            {
                MissingValue = (dataScale.MissingValue * dataScale.ScaleFactor) + dataScale.AddOffset;
            } else
            {
                MissingValue = this.source.variable.GetMissingValue();
                if(MissingValue == null)
                {
                    MissingValue = double.NaN;
                }
            }
            var indexer = dest.GetGenericIndexer<double>();

            for (int row = 0; row < RowLength; row++)
            {
                for(int col = 0; col < ColLength; col++)
                {
                    var v = indexer[row, col];
                    if(v == (double)MissingValue || double.IsNaN(v) || v == 9.9999999338158125E+36)
                    {
                        indexer[row, col] = double.NaN;
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

        public NetCDFData this[Dictionary<string, string>? lookup,params Dim[] dims]
        {
            get => new NetCDFData(this, lookup, dims);
        }

        public void Dispose()
        {
            this.variable.Dispose();
        }
    }
}
