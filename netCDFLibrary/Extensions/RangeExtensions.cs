using Microsoft.Research.Science.Data;

namespace netCDFLibrary.Extensions
{
    internal static class RangeExtensions
    {
        internal static (Microsoft.Research.Science.Data.Range, int) Convert(this System.Range range, int length)
        {

            int Offset = 0;
            int Length = 0;
            if (range.Start.Value != 0 && range.End.Value != 0)
            {
                if (range.Start.Value == range.End.Value)
                {
                    (Offset, Length) = range.GetOffsetAndLength(length);
                    if (Length == 0)
                    {
                        return (DataSet.Range(Offset), 1);
                    }
                    else
                    {
                        return (DataSet.Range(Offset, Length - 1), Length);
                    }
                }
                else
                {
                    Offset = range.Start.Value;
                    Length = range.End.Value;
                    return (DataSet.Range(Offset, Length - 1), Length - Offset);
                }
            }
            (Offset, Length) = range.GetOffsetAndLength(length);
            if (Length == 0)
            {
                return (DataSet.Range(Offset), 1);
            }
            else
            {
                return (DataSet.Range(Offset, Length - 1), Length);
            }
        }
    }
}
