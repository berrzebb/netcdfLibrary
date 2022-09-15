using netCDFLibrary.Data;

namespace netCDFLibrary.Extensions
{
    public static class NetCDFPrimitveDataExtension
    {
        public static IEnumerable<T> Select<T>(this NetCDFPrimitiveData data, Func<int, double, T> fn)
        {
            for (int i = 0; i < data.values.Length; i++)
            {
                yield return fn(i, data.values[i]);
            }
        }
    }
}
