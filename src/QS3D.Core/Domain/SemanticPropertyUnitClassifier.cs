using System;

namespace QS3D.Core.Domain
{
    public static class SemanticPropertyUnitClassifier
    {
        private static readonly string[] LinearMeterSuffixes =
        {
            "Length",
            "Width",
            "Height",
            "Depth",
            "Thickness",
            "Offset",
            "Elevation",
            "Perimeter",
            "Radius",
            "Diameter",
            "Spacing",
            "Cover",
            "Clearance",
            "Sagitta",
            "Chamfer"
        };

        public static bool IsLinearMeterProperty(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            var normalized = key.Trim();
            if (!normalized.EndsWith("M", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("Mm", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("M2", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("M3", StringComparison.OrdinalIgnoreCase))
                return false;

            var stem = normalized.Substring(0, normalized.Length - 1);
            foreach (var suffix in LinearMeterSuffixes)
                if (stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
    }
}
