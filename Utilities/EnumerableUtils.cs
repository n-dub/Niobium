using System.Collections.Generic;
using System.Linq;

namespace Utilities
{
    public static class EnumerableUtils
    {
        public static IEnumerable<(TKey, TValue)> Deconstruct<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> d)
        {
            return d.Select(x => (x.Key, x.Value));
        }
    }
}
