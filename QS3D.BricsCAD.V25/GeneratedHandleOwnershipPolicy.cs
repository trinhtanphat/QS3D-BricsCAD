using System;

namespace QS3D.BricsCAD.V25
{
    internal static class GeneratedHandleOwnershipPolicy
    {
        public static bool IsOwnerSlot(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (string.Equals(normalized, "PhysicalOpeningCutSolidHandle", StringComparison.OrdinalIgnoreCase)) return true;
            if (!normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return false;
            return normalized.EndsWith("Handle", StringComparison.OrdinalIgnoreCase) ||
                   normalized.EndsWith("Handles", StringComparison.OrdinalIgnoreCase);
        }
    }
}
