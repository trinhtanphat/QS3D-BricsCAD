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

            var index = 0;
            foreach (var raw in sourceHandles)
            {
                var handle = raw ?? string.Empty;
                if (string.IsNullOrWhiteSpace(handle))
                    throw new InvalidOperationException("Report provenance contains an empty stored SourceHandles entry at index " + index + ". Repair source ownership before reporting.");
                if (!string.Equals(handle, handle.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Report provenance contains a non-canonical stored SourceHandles entry at index " + index + ". Repair source ownership before reporting.");
                if (ContainsIgnoreCase(target, handle))
                    throw new InvalidOperationException("Report provenance contains duplicate stored SourceHandles identity: " + handle + ". Repair source ownership before reporting.");
                target.Add(handle);
                index++;
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
