using System.Collections.Generic;

namespace QS3D.BricsCAD.V25
{
    internal static class SaveAsLifecycleDictionaryExtensions
    {
        // Dictionary.TryAdd is not available on the V25 net48 target. V26's
        // runtime instance method wins there; this extension keeps shared source
        // behavior identical without changing the production target framework.
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key)) return false;
            dictionary.Add(key, value);
            return true;
        }
    }
}
