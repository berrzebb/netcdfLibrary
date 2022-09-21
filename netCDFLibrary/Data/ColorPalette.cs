using System.Windows.Media;
using OpenCvSharp;

namespace netCDFLibrary.Data
{
    public class ColorPalette
    {
        private static readonly Dictionary<string, ColormapTypes> colorMaps = Enum.GetValues<ColormapTypes>().ToDictionary(v => v.ToString(), v => v);
        public static readonly string[] Palettes = colorMaps.Keys.ToArray();

        internal ColormapTypes colorMap;
        public int colorCount { get; set; }
        public double lower { get; set; }
        public double upper { get; set; }
        public bool isAutoFit { get; set; }
        public bool isContour { get; set; }

        public double alphaValue { get; set; }
        public double alpha2 => (this.alphaValue * 255) / (this.upper - this.lower);
        public double beta2 => -(this.alphaValue * 255) * this.lower / (this.upper - this.lower);

        public double alpha => 255.0 / (this.upper - this.lower);
        public double beta => -255.0 * this.lower / (this.upper - this.lower);

        public byte threshold { get; set; }
        public byte maxThreshold { get; set; }

        public LinearGradientBrush ColorBrush { get; private set; } = new LinearGradientBrush();
        private ColorPalette() {
        }
        private List<Color> ColorTable = new List<Color>();
        public Color this[double value]
        {
            get
            {
                var normalizedValue = (value - this.lower) / (this.upper - this.lower);
                var colorIndex = (int)(normalizedValue * this.colorCount);
                if(colorIndex > this.colorCount - 1)
                {
                    colorIndex = this.colorCount - 1;
                }
                return this.ColorTable[colorIndex];
            }
        }
        public Color GetColor(int idx)
        {
            if (idx > this.colorCount || idx > this.ColorTable.Count)
            {
                return Colors.Transparent;
            }

            return this.ColorTable[idx];
        }
        public void UpdatePalette()
        {
            Mat colorSrc = new Mat(new Size(1, this.colorCount), MatType.CV_64F);
            var colorSrcIndexer = colorSrc.GetGenericIndexer<double>();
            for (int i = 0; i < colorSrc.Rows; i++)
            {
                var ratio = (double)i / colorSrc.Rows;
                var value = ratio * (this.upper - this.lower) + this.lower;
                colorSrcIndexer[0, i] = value;
            }
            colorSrc.GetArray<double>(out double[] values);
            Mat bwSrc = new();
            colorSrc.ConvertTo(bwSrc, MatType.CV_8UC3, this.alpha, this.beta);

            Mat cb = new Mat();
            Cv2.ApplyColorMap(bwSrc, cb, this.colorMap);

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
                    Offset = (float)i / this.colorCount
                });
            }
            this.ColorBrush = new LinearGradientBrush()
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1),
                GradientStops = new GradientStopCollection(colors)
            };
        }
        public static ColorPalette Create(string colorMap, double lower = 0, double upper = 1, int colorCount = 255, double alphaValue = 1.0, bool isAutoFit = false, bool isContour = false,  byte threshold = 127, byte maxThreshold = 255) => Create(colorMaps.ContainsKey(colorMap) ? colorMaps[colorMap] : colorMaps["Viridis"], lower, upper, colorCount, alphaValue, isAutoFit, isContour, threshold, maxThreshold);
        public static ColorPalette Create(ColormapTypes colorMap, double lower = 0, double upper = 1, int colorCount = 255, double alphaValue = 1.0, bool isAutoFit = false,bool isContour = false, byte threshold = 127, byte maxThreshold = 255) {
            var palette = new ColorPalette()
            {
                colorMap = colorMap,
                lower = lower,
                upper = upper,
                colorCount = colorCount,
                alphaValue = alphaValue,
                isAutoFit = isAutoFit,
                isContour = isContour,
                threshold = threshold,
                maxThreshold = maxThreshold
            };
            palette.UpdatePalette();
            return palette;
    }
    }
}
