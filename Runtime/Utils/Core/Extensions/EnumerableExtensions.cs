using System.Collections.Generic;
using System.Linq;

namespace BrewedCode.Utils
{
    public static class EnumerableExtensions
    {
        // Referências: IEnumerable<T?> -> IEnumerable<T> (remove null)
        public static IEnumerable<T> WithNotNulls<T>(this IEnumerable<T> source)
            where T : class
            => source == null ? Enumerable.Empty<T>()
                : source.Where(x => x is not null)!.Select(x => x!);

        // Valores anuláveis: IEnumerable<T?> -> IEnumerable<T> (pega .Value)
        public static IEnumerable<T> WithNotNulls<T>(this IEnumerable<T?> source)
            where T : struct
            => source == null ? Enumerable.Empty<T>()
                : source.Where(x => x.HasValue).Select(x => x.Value);
    }
}
