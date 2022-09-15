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
        public double alphaValue { get; set; }
        public double alpha2 => (this.alphaValue * 255) / (this.upper - this.lower);
        public double beta2 => -(this.alphaValue * 255) * this.lower / (this.upper - this.lower);

        public double alpha => 255.0 / (this.upper - this.lower);
        public double beta => -255.0 * this.lower / (this.upper - this.lower);

        public LinearGradientBrush ColorBrush { get; private set; } = new LinearGradientBrush();
        private ColorPalette() {
        }
        private List<Color> ColorTable = new List<Color>();
        public Color this[double value]
        {
            get
            {
                Mat src = new Mat(1, 1, MatType.CV_64F, value);
                src.ConvertTo(src, MatType.CV_8UC3, this.alpha2, this.beta2);
                Cv2.ApplyColorMap(src, src, this.colorMap);
                var c = src.Get<Vec3b>(0, 0);
                return Color.FromArgb(255, c[0], c[1], c[2]);
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
        public static ColorPalette Create(string colorMap, double lower = 0, double upper = 1, int colorCount = 255, double alphaValue = 1.0, bool isAutoFit = false) => Create(colorMaps.ContainsKey(colorMap) ? colorMaps[colorMap] : colorMaps["virdis"], lower, upper, colorCount, alphaValue, isAutoFit);
        public static ColorPalette Create(ColormapTypes colorMap, double lower = 0, double upper = 1, int colorCount = 255, double alphaValue = 1.0, bool isAutoFit = false) {
            var palette = new ColorPalette()
            {
                colorMap = colorMap,
                lower = lower,
                upper = upper,
                colorCount = colorCount,
                alphaValue = alphaValue,
                isAutoFit = isAutoFit
            };
            Mat ColorBar = new Mat(new Size(1, colorCount), MatType.CV_8UC3, new Scalar(255, 255, 255));
            for (int i = 0; i < ColorBar.Rows; i++)
            {
                byte v = (byte)(255 - 255 * i / ColorBar.Rows);

                ColorBar.Set(i, 0, new Vec3b(v, v, v));
            }
            ColorBar.ConvertTo(ColorBar, MatType.CV_8UC3);
            Mat cb = new Mat();
            Cv2.ApplyColorMap(ColorBar, cb, colorMap);

            palette.ColorTable = new List<Color>(cb.Rows);
            for(int i = 0; i < cb.Rows; i++)
            {
                var v = cb.Get<Vec3b>(i, 0);
                palette.ColorTable.Add(Color.FromArgb(255, v[0], v[1], v[2]));
            }
            List<GradientStop> colors = new List<GradientStop>();
            for (int i = 0; i < palette.ColorTable.Count; i++)
            {

                colors.Add(new GradientStop()
                {
                    Color = palette.ColorTable[i],
                    Offset = (float)i / colorCount
                });
            }
            palette.ColorBrush = new LinearGradientBrush()
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1),
                GradientStops = new GradientStopCollection(colors)
            };
            return palette;
    }
    }
}
