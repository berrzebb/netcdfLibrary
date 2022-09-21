using netCDFLibrary.Data;

namespace netCDFLibrary.Extensions
{
    public static class NetCDFPrimitveDataExtension
    {
        public static IEnumerable<T> Select<T>(this NetCDFPrimitiveData data, Func<double, int, T> fn)
        {
            for (int i = 0; i < data.values.Length; i++)
            {
                yield return fn(data.values[i], i);
            }
        }
    }
}
