using System;

namespace QS3D.Core.Domain
{
    /// <summary>
    /// Canonical Family/instance contract for the Móng Bè workflow.
    /// ElevationMode is deliberately separate from BottomLevelId/TopLevelId: those keys
    /// remain real Floor/Level identities, while this property describes which face of
    /// the raft is anchored to the working level/source plane.
    /// </summary>
    public static class RaftFoundationPropertySet
    {
        public const string SubtypeName = "Móng Bè";
        public const string WorkspaceSubtypeKey = "WorkspaceSubtype";
        public const string ElevationModeKey = "ElevationMode";
        public const string BottomLevelMode = "bottom_level";
        public const string TopLevelMode = "top_level";

        public static string NormalizeElevationMode(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0) return BottomLevelMode;
            if (string.Equals(text, BottomLevelMode, StringComparison.OrdinalIgnoreCase)) return BottomLevelMode;
            if (string.Equals(text, TopLevelMode, StringComparison.OrdinalIgnoreCase)) return TopLevelMode;
            throw new InvalidOperationException(
                "Cao độ Móng Bè chỉ nhận '" + BottomLevelMode + "' hoặc '" + TopLevelMode + "'.");
        }

        public static double ResolveBottomOffsetM(string? elevationMode, double thicknessM)
        {
            if (double.IsNaN(thicknessM) || double.IsInfinity(thicknessM) || thicknessM <= 0d)
                throw new ArgumentOutOfRangeException(nameof(thicknessM), "Chiều dày Móng Bè phải là số hữu hạn > 0.");
            return string.Equals(NormalizeElevationMode(elevationMode), TopLevelMode, StringComparison.Ordinal)
                ? -thicknessM
                : 0d;
        }

        public static bool IsRaftFamily(ProjectFamily? family)
        {
            if (family == null || family.Category != ElementCategory.Foundation) return false;
            if (family.Properties.TryGetValue(WorkspaceSubtypeKey, out var subtype) &&
                string.Equals((subtype ?? string.Empty).Trim(), SubtypeName, StringComparison.OrdinalIgnoreCase))
                return true;
            return HasSubtypeName(family.Name);
        }

        public static bool IsRaftElement(ProjectElement? element, ProjectFamily? family)
        {
            if (element == null || element.Category != ElementCategory.Foundation) return false;
            if (element.Properties.TryGetValue(WorkspaceSubtypeKey, out var subtype) &&
                string.Equals((subtype ?? string.Empty).Trim(), SubtypeName, StringComparison.OrdinalIgnoreCase))
                return true;
            return IsRaftFamily(family);
        }

        private static bool HasSubtypeName(string? familyName)
        {
            var name = (familyName ?? string.Empty).Trim();
            if (string.Equals(name, SubtypeName, StringComparison.OrdinalIgnoreCase)) return true;
            if (!name.StartsWith(SubtypeName, StringComparison.OrdinalIgnoreCase) || name.Length <= SubtypeName.Length) return false;
            var separator = name[SubtypeName.Length];
            return separator == '-' || separator == ' ' || separator == '_' || char.IsDigit(separator);
        }
    }
}
