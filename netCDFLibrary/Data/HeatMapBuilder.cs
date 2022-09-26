using OpenCvSharp;

namespace netCDFLibrary.Data
{
    public record HeatMapOptions(bool isYFlip = false);
    
    public static class HeatMapBuilder
    {
        private static Mat generate_internal(ColorPalette palette, int Rows, int Cols, double[] data, HeatMapOptions? options = null)
        {
            if (options == null)
            {
                options = new HeatMapOptions();
            }

            using Mat<double> src = new(Rows, Cols, data);
            using Mat<byte> bwSrc = new();

            if(palette.Options.isAutoFit)
            {
                Cv2.MinMaxLoc(src, out double lower, out double upper);
                palette.Options.lower = lower;
                palette.Options.upper = upper;
            }
//            src.SaveImage("Original.png");
            src.ConvertTo(bwSrc, MatType.CV_8UC3, palette.alpha, palette.beta);
            if (palette.Options.isReverseColor)
            {
                // Reverse Color
                Cv2.BitwiseNot(bwSrc, bwSrc);
            }
            // Min Max Normalized Matrix(0~1) => Black White Matrix(0~255)
            Mat dst = new();
            Cv2.ApplyColorMap(bwSrc, dst, palette.Options.colorMap);
            // Black White Matrix To Heatmap Matrix

            if (palette.Options.isContour)
            {
                var Threshold = bwSrc.AdaptiveThreshold(255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, palette.Options.maxThreshold, 5);
                //var Threshold = grayscale.Threshold(palette.threshold, palette.maxThreshold, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                // Processing Contours
                using Mat hierachy = new();
                // Create Threshold
                Threshold.FindContours(out Mat[] contours, hierachy, RetrievalModes.Tree, ContourApproximationModes.ApproxNone);

                dst.DrawContours(contours, -1, Scalar.FromRgb(255, 255, 255), 1, LineTypes.AntiAlias);

            }
            
            dst.CvtColor(ColorConversionCodes.RGB2BGR);
            
            var channel = Cv2.Split(dst);
            Mat dalpha = new Mat(Rows, Cols, MatType.CV_8UC1, palette.Options.alphaValue * 255);
            Mat a = new Mat();
            src.ConvertTo(a, MatType.CV_8UC1, 255, 0);
            Mat aa = a.BitwiseAnd(dalpha);
            Cv2.Merge(channel.Concat(new[] { aa }).ToArray(), dst);
            dst.GetArray(out Vec4b[] vv);
            
            return dst;
        }
        private static Mat generate_internal(ColorPalette palette, ref NetCDFPrimitiveData data,HeatMapOptions? options = null)
        {
            return generate_internal(palette, data.height, data.width, data.values, options);
        }
        public static Mat Generate(ColorPalette palette, int Rows, int Cols, ref double[] data, HeatMapOptions? options = null)
        {
            options ??= new HeatMapOptions();
            Mat dst = generate_internal(palette, Rows, Cols, data, options);
            if (options.isYFlip)
            {
                dst = dst.Flip(FlipMode.X);
            }
            dst.SaveImage("test.png");
            return dst;
        }
        public static Mat Generate(ColorPalette palette, ref NetCDFPrimitiveData data, HeatMapOptions? options = null)
        {
            options ??= new HeatMapOptions();

            Mat dst = generate_internal(palette, ref data, options);
            if (options.isYFlip)
            {
                dst = dst.Flip(FlipMode.X);
            }
            dst.SaveImage("test.png");
            return dst;
        }
        public static bool Generate(ColorPalette palette, ref NetCDFPrimitiveData data, string path, HeatMapOptions? options = null)
        {

            Mat dst = Generate(palette, ref data, options);
            dst.SaveImage(path);
            return true;
        }
    }
}
