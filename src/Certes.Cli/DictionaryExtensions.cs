using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Cli
{
    public static class DictionaryExtensions
    {
        internal static TValue TryGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue val;
            return dictionary.TryGetValue(key, out val) ? val : default(TValue);
        }
    }
}
