namespace netCDFLibrary.Data
{
    public class CoordinateScaler
    {
        public static CoordinateScaler Empty = new CoordinateScaler(0, 0, 0, 0, 0, 0);

        public double MinX { get; }
        public double MaxX { get; }
        public double MinY { get; }
        public double MaxY { get; }
        public double Width { get; }
        public double Height { get; }
        public Range XRange { get; private set; } = ..;
        public Range YRange { get; private set; } = ..;
        public int MinXIndex { get; private set; } = 0;
        public int MinYIndex { get; private set; } = 0;
        public double XGap { get; private set; }
        public double YGap { get; private set; }
        public bool IsYFlip { get; private set; }

        public CoordinateScaler(double minX, double maxX, double minY, double maxY, double width, double height, bool isYFlip = false)
        {
            this.MinX = minX;
            this.MaxX = maxX;
            this.MinY = minY;
            this.MaxY = maxY;
            this.Width = width;
            this.Height = height;
            this.XGap = (this.MaxX - this.MinX) / this.Width;
            this.YGap = (this.MaxY - this.MinY) / this.Height;
            this.IsYFlip = isYFlip;
        }

        public CoordinateScaler(double minX, double maxX, double minY, double maxY, double width, double height, double XGap, double YGap, bool isYFlip = false)
        {
            this.MinX = minX;
            this.MaxX = maxX;
            this.MinY = minY;
            this.MaxY = maxY;
            this.Width = width;
            this.Height = height;
            this.XGap = XGap;
            this.YGap = YGap;
            this.IsYFlip = isYFlip;
        }

        public bool XContains(double X) => this.MinX <= X && this.MaxX > X;

        public bool YContains(double Y) => this.MinY <= Y && this.MaxY > Y;

        public bool Contains(double Y, double X) => this.XContains(X) && this.YContains(Y);

        public double XOffset(int col) => this.MinX + (col * this.XGap);

        public double YOffset(int row) => this.MinY + (row * this.YGap);

        public (double, double) GetOffset(int row, int col) => (this.YOffset(row), this.XOffset(col));

        public (double, double) this[int row, int col] => this.GetOffset(row, col);

        private double XNormalize(double value)
        {
            return (value > 180) ? value - 360 : value < -180 ? value + 360 : value;
        }

        private double YNormalize(double value)
        {
            return (value > 90) ? value - 180 : value < -90 ? value + 180 : value;
        }

        public int FindYIndex(double Y)
        {
            var actualY = Y;
            if (actualY <= this.MinY)
            {
                actualY = this.MinY;
            }

            if (actualY >= this.MaxY)
            {
                actualY = this.MaxY;
            }

            int YOffset = this.IsYFlip ? (int)this.Height - 1 : 0;

            var result = (int)((actualY - this.MinY) / this.YGap);
            if (result != 0 && YOffset != 0)
            {
                result = Math.Abs(YOffset - result);
            }
            if (result == (int)this.Height)
            {
                return result - 1;
            }
            return result;
        }

        public int FindXIndex(double X)
        {
            var actualX = X;
            if (actualX <= this.MinX)
            {
                actualX = this.MinX;
            }

            if (actualX >= this.MaxX)
            {
                actualX = this.MaxX;
            }
            var result = (int)((actualX - this.MinX) / this.XGap);
            if (result == (int)this.Width)
            {
                return result - 1;
            }
            return result;
        }

        public (int, int) FindIndex(double Y, double X)
        {
            return (this.FindYIndex(Y), this.FindXIndex(X));
        }

        public CoordinateScaler GetNearest(double minX, double minY, double maxX, double maxY)
        {
            var normMinX = this.XNormalize(minX);
            var normMinY = this.YNormalize(minY);

            var normMaxX = this.XNormalize(maxX);
            var normMaxY = this.YNormalize(maxY);
            // Arrange Boundaries
            var actualMinX = Math.Min(normMinX, normMaxX);
            var actualMaxX = Math.Max(normMinX, normMaxX);
            var actualMinY = Math.Min(normMinY, normMaxY);
            var actualMaxY = Math.Max(normMinY, normMaxY);

            //
            actualMinX = Math.Max(this.MinX, actualMinX);
            actualMaxX = Math.Min(this.MaxX, actualMaxX);

            actualMinY = Math.Max(this.MinY, actualMinY);
            actualMaxY = Math.Min(this.MaxY, actualMaxY);

            var minXIndex = this.FindXIndex(actualMinX);
            var maxXIndex = this.FindXIndex(actualMaxX);
            var minYIndex = this.FindYIndex(actualMinY);
            var maxYIndex = this.FindYIndex(actualMaxY);

            int width = Math.Abs(maxXIndex - minXIndex);
            int height = Math.Abs(maxYIndex - minYIndex);
            return new CoordinateScaler(actualMinX, actualMaxX, actualMinY, actualMaxY, width, height == 0 ? this.Height : height, this.XGap, this.YGap)
            {
                XRange = minXIndex..maxXIndex,
                YRange = minYIndex > maxYIndex ? maxYIndex..minYIndex : minYIndex..maxYIndex,
                MinXIndex = minXIndex,
                MinYIndex = minYIndex > maxYIndex ? maxYIndex : minYIndex
            };
        }
    }
}