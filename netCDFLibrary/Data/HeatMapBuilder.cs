using OpenCvSharp;

namespace netCDFLibrary.Data
{
    public record HeatMapOptions(Size kSize,int index = 0, bool isDilate = false, bool isYFlip = false, double sigmaX = 0, double sigmaY = 0, BorderTypes borderTypes = BorderTypes.Default);
    
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
        private static Mat generate_gausian_internal(ColorPalette palette, Mat src, HeatMapOptions? options = null)
        {
            if (options == null)
            {
                options = new HeatMapOptions(new Size(21, 21));
            }

            src.GetArray(out double[] data);
            Mat blobDistance = new Mat();
            Mat blobHeatmap = new Mat();
            Mat bwSrc = new Mat();
            Mat gaussian = new Mat();
            Mat element = Cv2.GetStructuringElement(MorphShapes.Ellipse, options.kSize);
            Mat dst = new Mat();
            Mat dst2 = new Mat();

            Cv2.Normalize(src, bwSrc, 0, 255, NormTypes.MinMax, MatType.CV_8UC1);
            Mat threshold = new Mat();

            if (options.isDilate)
            {
//                bwSrc.SaveImage($"grayed_{options.index}.png");
                Cv2.MorphologyEx(bwSrc, bwSrc, MorphTypes.Gradient, element, new Point(-1, -1), 1);
                Cv2.Blur(bwSrc, bwSrc, options.kSize);

                Cv2.Threshold(bwSrc, threshold, 0, 255, ThresholdTypes.Binary);
                Cv2.ApplyColorMap(bwSrc, dst, palette.Options.colorMap);
                // Find Contours
                Cv2.FindContours(bwSrc, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.Tree,
                    ContourApproximationModes.ApproxSimple, new Point(0, 0));
                for (int i = 0; i < contours.Length; i++)
                {
                    dst.DrawContours(contours, i, Scalar.Blue, 1);

                }

                Mat dalpha = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, palette.Options.alphaValue * 255);
                Mat aa = dalpha.BitwiseAnd(threshold);
                dst.CvtColor(ColorConversionCodes.RGB2BGR);
                var channel = Cv2.Split(dst);
                Cv2.Merge(new[] { channel[0], channel[1], channel[2], aa }, dst);

                return dst;
            }
            else
            {
                if (palette.Options.isReverseColor)
                {
                    // Reverse Color
                    Cv2.BitwiseNot(bwSrc, bwSrc);
                }


                // Find Contours
                Cv2.FindContours(bwSrc, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.Tree,
                    ContourApproximationModes.ApproxSimple, new Point(0, 0));
                Mat blobMask = Mat.Zeros(bwSrc.Size(), MatType.CV_8UC1);


                for (int i = 0; i < contours.Length; i++)
                {
                    Cv2.Threshold(bwSrc, threshold, 0, 255, ThresholdTypes.Binary);

                    bwSrc.CopyTo(blobDistance, threshold);


                    Cv2.ApplyColorMap(blobDistance, blobHeatmap, palette.Options.colorMap);

                    //Cv2.Dilate(blobHeatmap, blobHeatmap, element, iterations: 3, borderType: options.borderTypes);
                    Cv2.GaussianBlur(blobHeatmap, blobHeatmap, options.kSize, options.sigmaX, options.sigmaY,
                        options.borderTypes);
                    blobHeatmap.CopyTo(dst, threshold);
                    //dst.DrawContours(contours, i, 255, 5, LineTypes.AntiAlias, hierarchy);

                }
                var newData = new byte[data.Length];
                for (int i = 0; i < newData.Length; i++)
                {
                    newData[i] = double.IsNaN(data[i]) ? (byte)0 : (byte)(palette.Options.alphaValue * 255);
                }

                Mat alpha = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, newData);
                dst.CvtColor(ColorConversionCodes.RGB2BGR);
                var channel = Cv2.Split(dst);

                Cv2.Merge(new[] { channel[0], channel[1], channel[2], alpha }, dst);


                return dst;
            }

        }
        private static Mat generate_gausian_internal(ColorPalette palette, int Rows, int Cols, double[] data, HeatMapOptions? options = null)
        {
            Mat<double> src = new(Rows, Cols, data);
            return generate_gausian_internal(palette, src, options);
        }
        
        private static Mat generate_internal(ColorPalette palette, Mat src, HeatMapOptions? options = null)
        {
            if (options == null)
            {
                options = new HeatMapOptions(new Size(21,21));
            }

            src.GetArray(out double[] data);
            Mat bwSrc = new Mat();
            src.ConvertTo(bwSrc, MatType.CV_8UC3, palette.alpha, palette.beta);

            if (palette.Options.isReverseColor)
            {
                // Reverse Color
                Cv2.BitwiseNot(bwSrc, bwSrc);
            }
            Mat dst = new();

            Cv2.ApplyColorMap(bwSrc, dst, palette.Options.colorMap);

                Cv2.ImShow("dst", dst);
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
            var newData = new byte[data.Length];
            for (int i = 0; i < newData.Length; i++)
            {
                newData[i] = double.IsNaN(data[i]) ? (byte)0 : (byte)(palette.Options.alphaValue * 255);
            }
            Mat a = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, newData);
            Cv2.Merge(new[] { channel[0], channel[1], channel[2], a }, dst);


            return dst;
        }
        private static Mat generate_internal(ColorPalette palette, int Rows, int Cols, double[] data, HeatMapOptions? options = null)
        {
            Mat<double> src = new(Rows, Cols, data);
            return generate_internal(palette, src, options);
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
