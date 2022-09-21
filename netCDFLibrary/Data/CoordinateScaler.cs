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
        public double XGap { get; private set; }
        public double YGap { get; private set; }
    public CoordinateScaler(double minX, double maxX, double minY, double maxY, double width, double height)
        {
            this.MinX = minX;
            this.MaxX = maxX;
            this.MinY = minY;
            this.MaxY = maxY;
            this.Width = width;
            this.Height = height;
            this.XGap = (this.MaxX - this.MinX) / this.Width;
            this.YGap = (this.MaxY - this.MinY) / this.Height;
        }
        public CoordinateScaler(double minX, double maxX, double minY, double maxY, double width, double height, double XGap, double YGap)
        {
            this.MinX = minX;
            this.MaxX = maxX;
            this.MinY = minY;
            this.MaxY = maxY;
            this.Width = width;
            this.Height = height;
            this.XGap = XGap;
            this.YGap = YGap;
        }

        public (double, double) this[int row, int col] => this.GetOffset(row, col);
        public double XOffset(int col) => this.MinX + (col * this.XGap);
        public double YOffset(int row) => this.MinY + (row * this.YGap);
        public (double, double) GetOffset(int row, int col) => (this.YOffset(row), this.XOffset(col));
        public (int, int) FindIndex(double Y, double X, bool isYFlip)
        {
            bool foundX = false ,foundY = false;
            int XIndex = 0;
            int YIndex = 0;
            for (int row = 0; row < this.Height; row++)
            {
                // 모든 범위를 찾으면 나오도록 한다.
                if (foundX && foundY)
                {
                    break;
                }
                int newRow = isYFlip ? row : (int)this.Height - row;
                var yOffset = this.YOffset(newRow);

                if (!foundY && Y < yOffset)
                {
                    YIndex = newRow - 1;
                    foundY = true;
                }
                if (!foundX)
                {
                    for (int col = 0; col < this.Width; col++)
                    {
                        var xOffset = this.XOffset(col);
                        if (!foundX && X < xOffset)
                        {
                            XIndex = col - 1;
                            foundX = true;
                            break;
                        }
                    }
                }
            }
            return (YIndex, XIndex);
        }
        public CoordinateScaler GetNearest(double minX, double minY, double maxX, double maxY, bool isYFlip)
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

            int minXIndex = (int)this.Width;
            int maxXIndex = (int)this.Width;
            int minYIndex = (int)this.Height;
            int maxYIndex = (int)this.Height;
            bool foundMinX = false;
            bool foundMaxX = false;
            bool foundMinY = false;
            bool foundMaxY = false;
            for (int row =0; row < this.Height; row++)
            {
                // 모든 범위를 찾으면 나오도록 한다.
                if (foundMinX && foundMaxX && foundMinX && foundMaxY)
                {
                    break;
                }

                var yOffset = this.YOffset(row);

                if (!foundMinY && actualMinY < yOffset)
                {
                    actualMinY = this.YOffset(row - 1);
                    minYIndex = row - 1;
                    if (isYFlip)
                    {
                        minYIndex = (int)this.Height - minYIndex;
                    }

                    foundMinY = true;
                }

                if (!foundMaxY && actualMaxY < yOffset)
                {
                    if (row == 0)
                    {
                        actualMaxY = this.XOffset(0);
                        maxYIndex = 0;
                    }
                    else
                    {
                        actualMaxY = this.YOffset(row - 1);
                        maxYIndex = row - 1;
                    }
                    if (isYFlip)
                    {
                        maxYIndex = (int)this.Height - maxYIndex;
                    }

                    foundMaxY = true;

                }

                if (foundMinX && foundMaxX)
                {
                    continue;
                }

                for (int col = 0; col < this.Width; col++)
                {
                    var xOffset = this.XOffset(col);
                    if(!foundMinX && actualMinX < xOffset)
                    {
                        actualMinX = this.XOffset(col - 1);
                        minXIndex = col - 1;
                        foundMinX = true;
                    }

                    if (!foundMaxX && actualMaxX < xOffset)
                    {
                        if(col == 0)
                        {
                            actualMaxX = this.XOffset(0);
                            maxXIndex = 0;
                            foundMaxX = true;
                        } else
                        {
                            actualMaxX = this.XOffset(col - 1);
                            maxXIndex = col - 1;
                            foundMaxX = true;
                        }
                    }

                }
            }
            int width = Math.Abs(maxXIndex - minXIndex);
            int height = Math.Abs(maxYIndex - minYIndex);
            return new CoordinateScaler(actualMinX, actualMaxX, actualMinY, actualMaxY, width, height, this.XGap, this.YGap)
            {
                XRange = minXIndex..maxXIndex,
                YRange = minYIndex > maxYIndex ? maxYIndex..minYIndex : minYIndex..maxYIndex
            };
        }
    }
}
