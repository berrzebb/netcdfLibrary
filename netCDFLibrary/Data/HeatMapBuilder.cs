using System.Windows.Media;
using OpenCvSharp;

namespace netCDFLibrary.Data
{
    public static class HeatMapBuilder
    {
        private static Mat generate_internal(ColorPalette palette, int Rows, int Cols, double[] data, bool isContour = false)
        {
            using Mat<double> src = new(Rows, Cols, data);
            using Mat<byte> bwSrc = new();

            if((palette.lower == -1 && palette.upper == -1) || palette.isAutoFit)
            {
                Cv2.MinMaxLoc(src, out double lower, out double upper);
                palette.lower = lower;
                palette.upper = upper;
            }
            src.ConvertTo(bwSrc, MatType.CV_8UC3, palette.alpha, palette.beta);
            // Min Max Normalized Matrix(0~1) => Black White Matrix(0~255)
            Mat dst = new();
            Mat filter = new Mat(Rows, Cols, MatType.CV_8U, 0);
            Cv2.ApplyColorMap(filter, filter, palette.colorMap);
            filter.GetArray(out Vec3b[] filterData);
            // Black White Matrix To Heatmap Matrix
            Cv2.ApplyColorMap(bwSrc, dst, palette.colorMap);
            Cv2.BitwiseXor(dst, filter, dst);
            dst.CvtColor(ColorConversionCodes.RGB2BGRA);

            var channel = Cv2.Split(dst);
            Mat dalpha = new Mat(Rows, Cols, MatType.CV_8UC1, palette.alphaValue * 255);
            Mat a = new Mat();
            src.ConvertTo(a, MatType.CV_8UC1, 255, 0);
            Mat aa = a.BitwiseAnd(dalpha);
            Cv2.Merge(channel.Concat(new[] { aa }).ToArray(), dst);
            dst.GetArray(out Vec4b[] vv);
            if (isContour)
            {
                // Create Threshold
                using Mat Thresold = new Mat();
                Cv2.Threshold(bwSrc, Thresold, 127, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                // Processing Contours
                using Mat hierachy = new();
                Thresold.FindContours(out Mat[] contours, hierachy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

                dst.DrawContours(contours, -1, Scalar.White, 1, LineTypes.AntiAlias);
            }

            return dst;
        }
        private static Mat generate_internal(ColorPalette palette, ref NetCDFPrimitiveData data, bool isContour = false)
        {
            return generate_internal(palette, data.height, data.width, data.values, isContour);
        }
        public static LinearGradientBrush generate_colorBar(ColormapTypes colorMap, int colorCount)
        {
            Mat ColorBar = new Mat(new Size(1, colorCount), MatType.CV_8UC3, new Scalar(255, 255, 255));
            Mat cb = new();
            for (int i = 0; i < ColorBar.Rows; i++)
            {
                byte v = (byte)(255 - 255 * i / ColorBar.Rows);

                ColorBar.Set(i, 0, new Vec3b(v, v, v));
            }
            ColorBar.ConvertTo(ColorBar, MatType.CV_8UC3);
            Cv2.ApplyColorMap(ColorBar, cb, colorMap);

            List<GradientStop> colors = new List<GradientStop>();
            for(int i = 0; i < cb.Rows; i++)
            {
                var v = cb.Get<Vec3b>(i, 0);
                colors.Add(new GradientStop()
                {
                    Color = Color.FromArgb(255, v[0], v[1], v[2]),
                    Offset = (float)i / colorCount
                });
            }
            return new LinearGradientBrush()
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1),
                GradientStops = new GradientStopCollection(colors)
            };
        }
        public static Mat generate_colorMap(ColormapTypes colorMap, string units, double Min, double Max, int colorCount = 255)
        {
            int num_bar_w = 60;
            int color_bar_w = 20;
            int vline = 10;
            Mat winMat = new Mat(new Size(num_bar_w + color_bar_w + vline, colorCount), MatType.CV_8UC3, new Scalar(255, 255, 255));

            //Scale
            Mat numWindow = new Mat(new Size(num_bar_w, colorCount), MatType.CV_8UC3, new Scalar(255, 255, 255));
            var step = colorCount / 10;
            for (int i = 0; i <= colorCount+step; i++)
            {
                if(i % step == 0)
                {
                    double j = colorCount - i;
                    double val = (i / (double)colorCount);
                    double v = Math.Round(val * (Max - Min) + Min, 2);
                    Cv2.PutText(numWindow, $"{v} {units}", new Point(10, numWindow.Rows - j - 5), HersheyFonts.HersheySimplex, 0.3, new Scalar(0, 0, 0), 1, LineTypes.AntiAlias, false);
                }
            }

            //color bar
            Mat ColorBar = new Mat(new Size(color_bar_w, colorCount), MatType.CV_8UC3, new Scalar(255, 255, 255));
            Mat cb = new();
            for (int i = 0; i < ColorBar.Rows; i++)
            {
                byte v = (byte)(255 - 255 * i / ColorBar.Rows);
                for (int j = 0; j < color_bar_w; j++)
                {
                    ColorBar.Set(i, j,new Vec3b(v, v, v));
                }
            }
            ColorBar.ConvertTo(ColorBar, MatType.CV_8UC3);
            Cv2.ApplyColorMap(ColorBar, cb, colorMap);
            numWindow.CopyTo(new Mat(winMat, new Rect(0 + color_bar_w, 0, num_bar_w, colorCount)));
            cb.CopyTo(new Mat(winMat, new Rect(0, 0,  color_bar_w, colorCount)));

            winMat.SaveImage($"{colorMap.ToString()}.png");
            //            dest = win_mat.clone();
            return winMat;
        }
        public static Mat Generate(ColorPalette palette, ref NetCDFPrimitiveData data, bool isYFlip = false, bool isContour = false)
        {

            Mat dst = generate_internal(palette, ref data, isContour);
            if (isYFlip)
            {
                dst = dst.Flip(FlipMode.X);
            }
            dst.SaveImage("test.png");
            return dst;
        }
        public static bool Generate(ColorPalette palette, ref NetCDFPrimitiveData data, string path, bool isYFlip = false, bool isContour = false)
        {

            Mat dst = Generate(palette, ref data, isYFlip, isContour);
            dst.SaveImage(path);
            return true;
        }
    }
}
