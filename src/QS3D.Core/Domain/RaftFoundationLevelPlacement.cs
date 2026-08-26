using System;
using System.Globalization;

namespace QS3D.Core.Domain
{
    public sealed class RaftFoundationVerticalPlacement
    {
        public RaftFoundationVerticalPlacement(double bottomElevationM, double topElevationM)
        {
            BottomElevationM = bottomElevationM;
            TopElevationM = topElevationM;
        }

        public double BottomElevationM { get; }
        public double TopElevationM { get; }
        public double ThicknessM => TopElevationM - BottomElevationM;
    }

    public static class RaftFoundationLevelPlacement
    {
        private const double ElevationToleranceM = 1e-8d;

        public static bool EnsureDefaults(ProjectState project, ProjectFamily family)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (!RaftFoundationPropertySet.IsRaftFamily(family))
                throw new InvalidOperationException("Family không phải Móng Bè.");

            var hasBottomLevel = family.Properties.ContainsKey(ProjectFloorService.BottomLevelIdKey);
            var hasTopLevel = family.Properties.ContainsKey(ProjectFloorService.TopLevelIdKey);
            var hasBottomOffset = family.Properties.ContainsKey(ProjectFloorService.BottomLevelOffsetKey);
            var hasTopOffset = family.Properties.ContainsKey(ProjectFloorService.TopLevelOffsetKey);
            var hasAnyCanonicalRelation = hasBottomLevel || hasTopLevel || hasBottomOffset || hasTopOffset;

            if (hasAnyCanonicalRelation)
            {
                if (!hasBottomLevel || !hasTopLevel || !hasBottomOffset || !hasTopOffset)
                    throw new InvalidOperationException("Móng Bè có quan hệ cao độ chưa đầy đủ. Cần BottomLevelId, TopLevelId, BottomLevelOffsetM và TopLevelOffsetM.");
                Resolve(project, family);
                return false;
            }

            var activeFloorId = (project.ActiveFloorId ?? string.Empty).Trim();
            if (activeFloorId.Length == 0)
                throw new InvalidOperationException("Móng Bè cần một Tầng/Level đang hoạt động để khởi tạo quan hệ cao độ.");
            var activeFloor = project.FindFloor(activeFloorId);
            if (activeFloor == null)
                throw new InvalidOperationException("Tầng/Level đang hoạt động không còn tồn tại: " + activeFloorId + ".");

            var thicknessM = RequirePositive(family, RaftFoundationPropertySet.ThicknessKey, "Chiều dày Móng Bè");
            var legacyBottomOffsetM = OptionalFinite(family, RaftFoundationPropertySet.BottomOffsetKey, 0d, "BottomOffsetM");

            family.Properties[ProjectFloorService.BottomLevelIdKey] = activeFloor.Id;
            family.Properties[ProjectFloorService.TopLevelIdKey] = activeFloor.Id;
            family.Properties[ProjectFloorService.BottomLevelOffsetKey] = legacyBottomOffsetM.ToString("R", CultureInfo.InvariantCulture);
            family.Properties[ProjectFloorService.TopLevelOffsetKey] = (legacyBottomOffsetM + thicknessM).ToString("R", CultureInfo.InvariantCulture);
            Resolve(project, family);
            return true;
        }

        public static RaftFoundationVerticalPlacement Resolve(ProjectState project, ProjectFamily family)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (!RaftFoundationPropertySet.IsRaftFamily(family))
                throw new InvalidOperationException("Family không phải Móng Bè.");

            var bottomLevelId = RequiredCanonicalText(family, ProjectFloorService.BottomLevelIdKey, "Cốt đáy");
            var topLevelId = RequiredCanonicalText(family, ProjectFloorService.TopLevelIdKey, "Cốt đỉnh");
            var bottomOffsetM = ProjectFloorService.ResolveOffsetM(null, family, ProjectFloorService.BottomLevelOffsetKey);
            var topOffsetM = ProjectFloorService.ResolveOffsetM(null, family, ProjectFloorService.TopLevelOffsetKey);
            var bottomElevationM = ProjectFloorService.ResolveAbsoluteElevationM(project, bottomLevelId, bottomOffsetM, "Cốt đáy Móng Bè");
            var topElevationM = ProjectFloorService.ResolveAbsoluteElevationM(project, topLevelId, topOffsetM, "Cốt đỉnh Móng Bè");

            if (!IsFinite(bottomElevationM) || !IsFinite(topElevationM) || !(topElevationM > bottomElevationM))
                throw new InvalidOperationException("Cao độ Móng Bè không hợp lệ: cốt đỉnh phải lớn hơn cốt đáy.");

            var thicknessM = RequirePositive(family, RaftFoundationPropertySet.ThicknessKey, "Chiều dày Móng Bè");
            var spanM = topElevationM - bottomElevationM;
            if (Math.Abs(spanM - thicknessM) > ElevationToleranceM)
                throw new InvalidOperationException(
                    "Quan hệ cao độ Móng Bè không khớp chiều dày. Span=" + spanM.ToString("R", CultureInfo.InvariantCulture) +
                    " m, ThicknessM=" + thicknessM.ToString("R", CultureInfo.InvariantCulture) + " m.");

            return new RaftFoundationVerticalPlacement(bottomElevationM, topElevationM);
        }

        private static string RequiredCanonicalText(ProjectFamily family, string key, string caption)
        {
            if (!family.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(caption + " chưa được chọn.");
            var trimmed = raw.Trim();
            if (!string.Equals(raw, trimmed, StringComparison.Ordinal))
                throw new InvalidOperationException(caption + " có identity không canonical (thừa khoảng trắng).");
            return trimmed;
        }

        private static double RequirePositive(ProjectFamily family, string key, string caption)
        {
            if (!family.Properties.TryGetValue(key, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                !IsFinite(value) || !(value > 0d))
                throw new InvalidOperationException(caption + " phải là số hữu hạn > 0.");
            return value;
        }

        private static double OptionalFinite(ProjectFamily family, string key, double fallback, string caption)
        {
            if (!family.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !IsFinite(value))
                throw new InvalidOperationException(caption + " phải là số hữu hạn.");
            return value;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
