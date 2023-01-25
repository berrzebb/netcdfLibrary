using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using Microsoft.Research.Science.Data.Utilities;
using netCDFLibrary.Extensions;
using Newtonsoft.Json;
using OpenCvSharp;
using Range = Microsoft.Research.Science.Data.Range;
using SystemRange = System.Range;

namespace netCDFLibrary.Data
{
    public record NetCDFBoundaries(double MinX, double MaxX, double MinY, double MaxY, double Width, double Height, bool isYFlip)
    {
        public static readonly NetCDFBoundaries Empty = new NetCDFBoundaries(0, 0, 0, 0, 0, 0, false);
    }

    public record MagnitudeValue(double direction, double value, double referenceValue)
    {
        public static readonly MagnitudeValue Empty = new MagnitudeValue(0, 0, 0);

        public static MagnitudeValue Create(double distance, double value, double referenceValue) => new MagnitudeValue(distance, value, referenceValue);
        public static MagnitudeValue Create(double distance, double value) => Create(distance, value, 0);
        public void Deconstruct(out double direction, out double value, out double referenceValue)
        {
            direction = this.direction;
            value = this.value;
            referenceValue = this.referenceValue;
        }
        public void Deconstruct(out double direction, out double value)
        {
            direction = this.direction;
            value = this.value;
        }
    }

    public static class MathExtensions
    {
        public static bool Equals(this double @this, double other, double TOLERANCE = 0.01) => Math.Abs(@this - other) < TOLERANCE;
    }

    public class CoordinateTransformatter
    {
        public Func<double, double, (double, double)> LL2XY { get; init; } = (x, y) => (x, y);
        public Func<double, double, (double, double)> XY2LL { get; init; } = (x, y) => (x, y);
    }

    public record Dim([JsonProperty] string name, [JsonProperty] SystemRange range)
    {
        public static Dim[] Create(Dim[] dims, params (string name, SystemRange range)[] name) => dims.Concat(name.Select(v => new Dim(v.name, v.range))).ToArray();
        public static Dim[] Create(Dim[] dims, params string[] name) => dims.Concat(name.Select(v => new Dim(v, SystemRange.StartAt(0)))).ToArray();
        public static Dim[] Create(Dim[] dims, params (string name, int index)[] dim) => dims.Concat(dim.Select(v => new Dim(v.name, new SystemRange(v.index, v.index)))).ToArray();

        public static Dim[] Create(params (string name, SystemRange range)[] name) => name.Select(v => new Dim(v.name, v.range)).ToArray();
        public static Dim[] Create(params string[] name) => name.Select(v => new Dim(v, SystemRange.StartAt(0))).ToArray();
        public static Dim[] Create(params (string name, int index)[] dim) => dim.Select(v => new Dim(v.name, new SystemRange(v.index, v.index))).ToArray();

        public static Dim[] Create(string name, SystemRange index) => Create((name, index));
        public static Dim[] Create(string name) => Create((name, 0..1));
    }
    public record DataSetDim(string name, Range range, int Length)
    {
        public static DataSetDim Create(string name, Range range, int length) => new(name, range, length);
        public static DataSetDim Create(string name, (Range range, int length) v) => new(name, v.range, v.length);
    }
    public record StatisticsData(double MinValue, double MaxValue)
    {
        public static readonly StatisticsData Empty = new StatisticsData(0, 0);
        private readonly Predicate<double> filter = v => true;
        public double MeanValue => (this.MaxValue + this.MinValue) / 2.0;

        public double Transform(double Value) => Value * (this.MaxValue - this.MinValue) + this.MinValue;
        public double Normalize(double Value) => (Value - this.MinValue) / (this.MaxValue - this.MinValue);
        public double[] Normalize(double[] values, double ignoreValue = 0) => values.Select(v => !this.filter(v) || v == ignoreValue ? 0 : this.Normalize(v)).ToArray();

        public static StatisticsData Create(double minValue, double maxValue) => new StatisticsData(minValue, maxValue);
    }

    public record NetCDFPrimitiveData(double minX, double minY, double maxX, double maxY, int width, int height, DataScale dataScale, bool isYFlip, double[] values, StatisticsData Statistics)
    {
        public static readonly NetCDFPrimitiveData Empty = new(0, 0, 0, 0, 0, 0, new DataScale(), false, Array.Empty<double>(), StatisticsData.Empty);

        private CoordinateScaler _scaler = CoordinateScaler.Empty;
        public double XGap => this._scaler.XGap;
        public double YGap => this._scaler.YGap;
        public (double, double) GetOffset(int row, int col) => this._scaler.GetOffset(row, col);
        public (int, int) FindIndex(double Y, double X) => this._scaler.FindIndex(Y, X);
        public int GetIndex(int row, int col) => col + (row * this.width);
        public static NetCDFPrimitiveData Create(double minX, double minY, double maxX, double maxY, int width, int height, DataScale dataScale, bool isYFlip, double[] values, StatisticsData statisticsData)
        {
            var ret = new NetCDFPrimitiveData(minX, minY, maxX, maxY, width, height, dataScale, isYFlip, values, statisticsData)
            {
                _scaler = new CoordinateScaler(minX, maxX, minY, maxY, width, height, isYFlip)
            };
            return ret;
        }

        public static NetCDFPrimitiveData Create(NetCDFPrimitiveData Source, double[] values, StatisticsData statistics)
        {
            var ret = new NetCDFPrimitiveData(Source)
            {
                values = values,
                Statistics = statistics
            };
            return ret;
        }
        public double this[(int row, int col) index] => this[index.row, index.col];

        public double this[int row, int col] => this[this.GetIndex(row, col)];
        public double this[int index]
        {
            get
            {
                if (index > this.values.Length)
                {
                    return double.NaN;
                }

                return this.values[index];
            }
        }
        public MagnitudeValue Magnitude(ref NetCDFPrimitiveData V, int[] index, Func<double, double>? transform = null)
        {
            double normalize(double value)
            {
                double remain = value % 360;
                if (remain < 0)
                {
                    remain += 360.0;
                }

                return remain;
            }

            double degrees(double radian)
            {
                return radian * (180 / Math.PI);
            }
            if (V.values.Length <= index[0] || this.values.Length <= index[1])
            {
                return MagnitudeValue.Empty;
            }

            var u = this.values[index[0]];
            if (double.IsNaN(u))
            {
                return MagnitudeValue.Empty;
            }

            var v = V.values[index[1]];
            if (double.IsNaN(v))
            {
                return MagnitudeValue.Empty;
            }

            var value = Math.Sqrt(Math.Pow(u, 2) + Math.Pow(v, 2));
            var direction = degrees(Math.Atan2(v, u));
            var adir = normalize(90 - normalize(direction));
            var trigFromDir = direction + 180;
            var cardinalDir = 90 - trigFromDir;
            var normDir = normalize(cardinalDir);

            if (transform != null)
            {
                value = transform(value);
            }

            return MagnitudeValue.Create(adir, value);
        }
        public MagnitudeValue Magnitude(ref NetCDFPrimitiveData V, int[] row, int[] col, Func<double, double>? transform = null)
        {
            if ((row[0] == -1 && col[0] == -1) || (row[1] == -1 && col[1] == -1))
            {
                return MagnitudeValue.Empty;
            }

            return this.Magnitude(ref V, new[] { this.GetIndex(row[0], col[0]), V.GetIndex(row[1], col[1]) }, transform);
        }
        public MagnitudeValue MagnitudeNormalized(ref NetCDFPrimitiveData V, int[] index, double referenceValue, Func<double, double>? transform = null)
        {
            var result = this.Magnitude(ref V, index, transform);

            return MagnitudeValue.Create(result.direction, result.value, result.value / referenceValue);
        }
        public MagnitudeValue MagnitudeNormalized(ref NetCDFPrimitiveData V, int[] row, int[] col, double referenceValue, Func<double, double>? transform = null)
        {
            var result = this.Magnitude(ref V, row, col, transform);

            return MagnitudeValue.Create(result.direction, result.value, result.value / referenceValue);
        }
    }

    public class NetCDFData
    {
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
        private readonly NetCDFBoundaries boundaries;

        public NetCDFData(NetCDFVariable? variable, Dictionary<string, string>? lookup, params Dim[] dims)
        {
            this.lookup = lookup;
            this.source = variable ?? throw new InvalidOperationException("source null");

            this.dims = dims;
            this.Dimensions = dims.Select((v, i) =>
            {
                var (range, length) = v.range.Convert(this.source.Shape[i]);
                return DataSetDim.Create(v.name, range, length);
            }).ToArray();

            var YDim = this.Dimensions[^2];
            var XDim = this.Dimensions[^1];

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
            int rowLength = this.Dimensions[^2].Length;
            int colLength = this.Dimensions[^1].Length;

            var data = this.source.DataSet.GetData<Array>(this.source.VariableId, this.Dimensions.Select(v => v.range).ToArray());
            MatType type = Type.GetTypeCode(this.source.variable.TypeOfData) switch
            {
                TypeCode.Single => MatType.CV_32F,
                TypeCode.Double => MatType.CV_64F,
                _ => MatType.CV_16S
            };
            Mat src = new(rowLength, colLength, type, data);
            Mat dest = new Mat();
            double alpha = dataScale.ScaleFactor;
            double beta = dataScale.AddOffset;
            src.ConvertTo(dest, MatType.CV_64F, alpha, beta);
            double missingValue;
            if (type == MatType.CV_16S)
            {
                missingValue = (dataScale.MissingValue * dataScale.ScaleFactor) + dataScale.AddOffset;
            }
            else
            {
                missingValue = Convert.ToDouble(this.source.variable.GetMissingValue());
                if (double.IsNaN(missingValue) || missingValue == 0)
                {
                    missingValue = dataScale.MissingValue;
                }
            }
            var indexer = dest.GetGenericIndexer<double>();

            for (int row = 0; row < rowLength; row++)
            {
                for (int col = 0; col < colLength; col++)
                {
                    var v = indexer[row, col];
                    if (
                        v.Equals(missingValue, 0.01) ||
                        v.Equals(dataScale.FillValue, 0.01) ||
                        v.Equals(9.969209968386869E+36, 0.01) ||
                        double.IsNaN(v)
                    )
                    {
                        indexer[row, col] = double.NaN;
                    }
                }
            }

            dest.MinMaxIdx(out double min, out double max);
            dest.GetArray(out double[] values);

            var statistics = StatisticsData.Create(min, max);

            return NetCDFPrimitiveData.Create(this.MinX, this.MinY, this.MaxX, this.MaxY, colLength, rowLength, dataScale, this.IsYFlip, values, statistics);
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

        public NetCDFVariable(NetCDFLib source, Variable variable) : this(source, variable, new DataScale(variable))
        {
        }

        public NetCDFVariable(NetCDFLib source, Variable variable, DataScale dataScale)
        {
            this.source = source;
            this.variable = variable;
            this.DataScale = dataScale;
        }

        public NetCDFData this[Dictionary<string, string>? lookup, params Dim[] dims]
        {
            get => new(this, lookup, dims);
        }

        public Array GetData()
        {
            return this.variable.GetData();
        }

        public Array GetData(int[] origin, int[] shape)
        {
            return this.variable.GetData(origin, shape);
        }

        public Array GetData(int[] origin, int[] shape, int[] count)
        {
            return this.variable.GetData(origin, shape, count);
        }

        public void Dispose()
        {
            this.variable.Dispose();
        }
    }
}