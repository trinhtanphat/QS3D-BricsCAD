using System;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.Export
{
    /// <summary>
    /// Defines which ProjectElement properties are portable semantic interchange data.
    /// CAD/native ownership and handle-bearing metadata is drawing-local and must never
    /// be rebound from a source snapshot into the target drawing.
    /// </summary>
    internal static class ProjectInterchangeElementPropertyPolicy
    {
        internal static bool IsPortable(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)) return false;
            if (normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return false;
            if (normalized.StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return false;
            return normalized.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
