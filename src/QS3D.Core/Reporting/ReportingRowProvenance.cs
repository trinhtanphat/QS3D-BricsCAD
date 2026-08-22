using System;
using System.Collections.Generic;

namespace QS3D.Core.Reporting
{
    internal static class ReportingRowProvenance
    {
        internal static void AppendSourceHandles(IList<string> target, IEnumerable<string> sourceHandles)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (sourceHandles == null) throw new ArgumentNullException(nameof(sourceHandles));

            foreach (var raw in sourceHandles)
            {
                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length == 0 || ContainsIgnoreCase(target, handle)) continue;
                target.Add(handle);
            }
        }

        private static bool ContainsIgnoreCase(IEnumerable<string> values, string candidate)
        {
            foreach (var value in values)
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
