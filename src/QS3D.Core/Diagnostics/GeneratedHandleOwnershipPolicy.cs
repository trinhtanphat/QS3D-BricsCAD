using System;
using System.Collections.Generic;

namespace QS3D.Core.Diagnostics
{
    public static class GeneratedHandleOwnershipPolicy
    {
        private static readonly IReadOnlyList<string> RebarSlots = Array.AsReadOnly(new[]
        {
            "GeneratedRebarHandles",
            "GeneratedShapeRebarHandles",
            "GeneratedTieRebarHandles",
            "GeneratedBeamStirrupHandles",
            "GeneratedSlabMeshHandles",
            "GeneratedWallMeshHandles",
            "GeneratedFoundationMeshHandles"
        });

        public static IReadOnlyList<string> RebarHandleKeys => RebarSlots;

        public static bool IsOwnerSlot(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (string.Equals(normalized, "PhysicalOpeningCutSolidHandle", StringComparison.OrdinalIgnoreCase)) return true;
            if (!normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return false;
            return normalized.EndsWith("Handle", StringComparison.OrdinalIgnoreCase) ||
                   normalized.EndsWith("Handles", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRebarOwnerSlot(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            foreach (var candidate in RebarSlots)
                if (string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
