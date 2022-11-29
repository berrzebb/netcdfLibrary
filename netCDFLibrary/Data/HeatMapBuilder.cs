using OpenCvSharp;

namespace netCDFLibrary.Data
{
    public record HeatMapOptions(Size kSize,int index = 0, bool isDensity = false, bool isYFlip = false, double sigmaX = 0, double sigmaY = 0, BorderTypes borderTypes = BorderTypes.Default);
    
    public static class HeatMapBuilder
    {
        private static readonly int kSize = 9;
        private static void Show(ref Mat target, string title, bool isFlip = false)
        {
            if (isFlip)
            {
                target = target.Flip(FlipMode.X);
                Cv2.ImShow(title, target);
                target = target.Flip(FlipMode.X);
            }
            else
            {
                Cv2.ImShow(title, target);
            }
        }

        private static void ShowGroup(string title, bool isFlip = false, params Mat[] mats)
        {
            Mat dest = new Mat();
            Cv2.Merge(mats, dest);
            Show(ref dest, title, isFlip);
        }

        private static Mat Morphology(ref Mat src, Size kernel)
        {
            Mat element = Cv2.GetStructuringElement(MorphShapes.Ellipse, kernel);
            Mat ret = new Mat();
            Cv2.MorphologyEx(src, ret, MorphTypes.Gradient, element, new Point(-1, -1));
            Cv2.Blur(ret, ret, kernel);
            return ret;
        }

        private static Mat ReverseColor(ref Mat src, bool isReverse)
        {
            if (isReverse)
            {
                Cv2.BitwiseNot(src, src);
            }
            return src;
        }
        private static Mat Threshold(ref Mat src, double thresh, double maxVal)
        {
            return src.Threshold(thresh, maxVal, ThresholdTypes.Binary);
        }
        private static Mat generate_gausian_internal(ColorPalette palette, Mat src, HeatMapOptions? options = null)
        {
            options ??= new HeatMapOptions(new Size(21, 21));

            Mat graySrc = new Mat();
            src.ConvertTo(graySrc, MatType.CV_8UC1, palette.alpha, palette.beta);
            Mat target = new Mat();
            Mat dst = new Mat();

            Mat threshold;

            Mat alphaOrigin = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, palette.Options.alphaValue * 255);
            Mat alpha = new Mat();

            if (options.isDensity)
            {
                graySrc = Morphology(ref graySrc, options.kSize);

                threshold = Threshold(ref graySrc, 0, 255);

                graySrc.CopyTo(target);
            }
            else
            {
                threshold = Threshold(ref graySrc, 0, 255);

                Cv2.GaussianBlur(graySrc, target, options.kSize, options.sigmaX, options.sigmaY,
                    options.borderTypes);

                graySrc.CopyTo(target);
            }

            target = ReverseColor(ref target, palette.Options.isReverseColor);

            Cv2.ApplyColorMap(target, target, palette.Options.colorMap);

            target.CvtColor(ColorConversionCodes.RGB2BGR);
            var channel = Cv2.Split(target);
            alphaOrigin.CopyTo(alpha, threshold);
            Cv2.Merge(new[] { channel[0], channel[1], channel[2], alpha }, target);
            if (palette.Options.Contours.Count != 0)
            {
                foreach (var contourItem in palette.Options.Contours)
                {
                    Mat contourThreshold = new Mat();
                    Mat actualThreshold = new Mat();
                    Mat hierarchy = new Mat();
                    Cv2.Threshold(graySrc, contourThreshold, contourItem.Threshold, 255, ThresholdTypes.BinaryInv);
                    Cv2.CopyTo(contourThreshold, actualThreshold, threshold);
                    Cv2.FindContours(actualThreshold, out Mat[] contours,hierarchy,
                        RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
                    for (int i = 0; i < contours.Length; i++)
                    {
                        Cv2.DrawContours(target, contours, i, contourItem.Color, contourItem.Thickness,
                            LineTypes.AntiAlias);
                    }

                }
            }

            Cv2.CopyTo(target, dst);
            return dst;
        }
        private static Mat generate_gausian_internal(ColorPalette palette, int Rows, int Cols, double[] data, HeatMapOptions? options = null)
        {
            Mat<double> src = new(Rows, Cols, data);
            return generate_gausian_internal(palette, src, options);
        }
        private static Mat generate_internal(ColorPalette palette, ref NetCDFPrimitiveData data,HeatMapOptions? options = null)
        {
            return generate_gausian_internal(palette, data.height, data.width, data.values, options);
        }
        public static Mat Generate(ColorPalette palette, Mat source, HeatMapOptions? options = null)
        {
            options ??= new HeatMapOptions(new Size(21,21));
            Mat dst = generate_gausian_internal(palette, source, options);
            if (options.isYFlip)
            {
                dst = dst.Flip(FlipMode.X);
            }
            dst.SaveImage("test.png");
            return dst;
        }
        public static Mat Generate(ColorPalette palette, int Rows, int Cols, ref double[] data, HeatMapOptions? options = null)
        {
            options ??= new HeatMapOptions(new Size(21,21));
            Mat dst = generate_gausian_internal(palette, Rows, Cols, data, options);
            if (options.isYFlip)
            {
                dst = dst.Flip(FlipMode.X);
            }
            dst.SaveImage("test.png");
            return dst;
        }
        public static Mat Generate(ColorPalette palette, ref NetCDFPrimitiveData data, HeatMapOptions? options = null)
        {
            options ??= new HeatMapOptions(new Size(21,21));

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
