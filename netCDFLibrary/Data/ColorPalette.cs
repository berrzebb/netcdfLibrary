using System.Windows.Media;
using OpenCvSharp;

namespace netCDFLibrary.Data
{
    public class ColorPaletteOptions
    {
        public ColormapTypes colorMap { get; set; } = ColormapTypes.Jet;
        public int colorCount { get; set; } = 255;
        public double lower { get; set; } = 0;
        public double upper { get; set; } = 1;
        public double alphaValue { get; set; } = 1.0;

        public bool isYFlip { get; set; } = false;
        public bool isAutoFit { get; set; } = true;
        public bool isContour { get; set; } = false;
        public bool isReverseColor { get; set; } = false;
        public byte threshold { get; set; }
        public byte maxThreshold { get; set; }

        public double DataRange => (this.upper - this.lower);
        public double DataOffset => this.lower / this.DataRange;
        public double Normalize(double value) => (value - this.lower) / this.DataRange;

        public double Normalize(double value, double outMin, double outMax)
        {
            if (double.IsNaN(value))
            {
                return value;
            }

            if (double.Equals(value, this.lower))
            {
                return outMin;
            }

            if (double.Equals(value, this.upper))
            {
                return outMax;
            }

            var ret = ((value - this.lower) / (this.upper - this.lower));
            if (ret < 0)
            {
                ret = 0;
            }

            if (ret > 1)
            {
                ret = 1;
            }

            ret = ret * (outMax - outMin) + outMin;
            return ret;
            
        }
            
        public double Transform(double ratio) => ratio * this.DataRange + this.lower;

        public IEnumerable<string> GenerateTicks(int tickCount = 7)
        {
            for(int i = 0; i < this.colorCount; i++) {
                if (i % tickCount == 0)
                {
                    var ratio = (double)i / this.colorCount;
                    var value = this.Transform(ratio);
                    yield return value.ToString("0.00#");
                }
            }
        }
        public ColorPaletteOptions(ColormapTypes colorMap, double lower = 0.0, double upper = 1.0, int colorCount = 255)
        {
            this.colorMap = colorMap;
            this.lower = lower;
            this.upper = upper;
            this.colorCount = colorCount;
        }
        public ColorPaletteOptions(string colorMap = "Jet", double lower = 0.0, double upper = 1.0, int colorCount = 255)
            : this(ColorPalette.colorMaps.ContainsKey(colorMap) ? ColorPalette.colorMaps[colorMap] : ColormapTypes.Jet, lower, upper, colorCount)
        {
        }
    }
    public class ColorPalette
    {
        internal static readonly Dictionary<string, ColormapTypes> colorMaps = Enum.GetValues<ColormapTypes>().ToDictionary(v => v.ToString(), v => v);
        public static readonly string[] Palettes = colorMaps.Keys.ToArray();

        public ColorPaletteOptions Options { get; set; } = new ColorPaletteOptions();

        public double alpha2 => (this.Options.alphaValue * 255) / this.Options.DataRange;
        public double beta2 => -(this.Options.alphaValue * 255) * this.Options.DataOffset;

        public double alpha => 255.0 / this.Options.DataRange;
        public double beta => -255.0 * this.Options.DataOffset;

        public double tickCount { get; init;  }
        public LinearGradientBrush ColorBrush { get; private set; } = new LinearGradientBrush();

        private ColorPalette() {
        }
        private List<Color> ColorTable = new List<Color>();
        public Color this[double value]
        {
            get
            {
                int colorIndex = this.GetColorIndex(value);

                return this.GetColor(colorIndex);
            }
        }
        public int GetColorIndex(double value)
        {
            var normalizedValue = this.Options.Normalize(value);
            var colorCount = this.Options.colorCount;
            var isReverse = this.Options.isReverseColor;
            var refCount = (!isReverse ? colorCount : 0);
            var colorIndex = Math.Abs((refCount - (int)(normalizedValue * colorCount)));
            if (colorIndex > colorCount - 1)
            {
                colorIndex = colorCount - 1;
            }
            return colorIndex;
        }
        public Color GetColor(int idx)
        {
            if (idx > this.Options.colorCount || idx > this.ColorTable.Count)
            {
                return Colors.Transparent;
            }

            return this.ColorTable[idx];
        }
        public void UpdatePalette(ColorPaletteOptions options)
        {
            this.Options = options;
            this.UpdatePalette();
        }
        public void UpdatePalette()
        {
            var colorCount = this.Options.colorCount;
            var colorMap = this.Options.colorMap;
            Mat colorSrc = new Mat(new Size(1, colorCount), MatType.CV_64F);
            var colorSrcIndexer = colorSrc.GetGenericIndexer<double>();
            for (int i = 0; i < colorSrc.Rows; i++)
            {
                var ratio = (double)i / colorSrc.Rows;
                var value =this.Options.Transform(ratio);
                colorSrcIndexer[0, i] = value;
            }
            Mat bwSrc = new();
            colorSrc.ConvertTo(bwSrc, MatType.CV_8UC3, this.alpha, this.beta);

            if (!this.Options.isReverseColor)
            {
                Cv2.BitwiseNot(bwSrc, bwSrc);
            }
            Mat cb = new Mat();
            Cv2.ApplyColorMap(bwSrc, cb, colorMap);

            this.ColorTable = new List<Color>(cb.Rows);
            for (int i = 0; i < cb.Rows; i++)
            {
                var v = cb.Get<Vec3b>(i, 0);
                this.ColorTable.Add(Color.FromArgb(255, v[2], v[1], v[0]));
            }
            List<GradientStop> colors = new List<GradientStop>();

            for (int i = 0; i < this.ColorTable.Count; i++)
            {

                colors.Add(new GradientStop()
                {
                    Color = this.ColorTable[i],
                    Offset = (float)i / colorCount
                });
            }
            this.ColorBrush = new LinearGradientBrush()
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1),
                GradientStops = new GradientStopCollection(colors)
            };
        }
        public static ColorPalette Create(ColorPaletteOptions options) {
            var palette = new ColorPalette();
            palette.UpdatePalette(options);
            return palette;
        }
    }
}
