using System.Collections.Generic;

namespace Certes.Cli
{
    public static class DictionaryExtensions
    {
        internal static TValue TryGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) =>
            dictionary.TryGetValue(key, out TValue val) ? val : default(TValue);
    }
}
