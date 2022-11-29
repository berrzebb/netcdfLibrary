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

        public int FindYIndex(double Y)
        {
            if (Y < this.MinY || Y > this.MaxY)
            {
                return -1;
            }

            int YOffset = this.IsYFlip ? (int)this.Height : 0;

            var result = (int)((Y - this.MinY) / this.YGap);
            if(result != 0 && YOffset != 0)
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
            if (X < this.MinX || X > this.MaxX)
            {
                return -1;
            }
            var result = (int)((X - this.MinX) / this.XGap);
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
            
            // Arrange Boundaries
            var actualMinX = Math.Min(minX, maxX);
            var actualMaxX = Math.Max(minX, maxX);
            var actualMinY = Math.Min(minY, maxY);
            var actualMaxY = Math.Max(minY, maxY);

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
