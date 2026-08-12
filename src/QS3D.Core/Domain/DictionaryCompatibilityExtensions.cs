using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    internal static class DictionaryCompatibilityExtensions
    {
        internal static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key)) return false;
            dictionary.Add(key, value);
            return true;
        }
    }
}
