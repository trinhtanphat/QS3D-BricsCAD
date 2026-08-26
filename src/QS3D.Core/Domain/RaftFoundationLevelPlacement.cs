using System;
using System.Collections.Generic;
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
            var activeFloor = FindFloor(project, activeFloorId, "Tầng/Level đang hoạt động");
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

            ValidateUniqueFloorIds(project);
            var bottomLevelId = RequiredCanonicalText(family, ProjectFloorService.BottomLevelIdKey, "Cốt đáy");
            var topLevelId = RequiredCanonicalText(family, ProjectFloorService.TopLevelIdKey, "Cốt đỉnh");
            var bottomOffsetM = RequiredFinite(family, ProjectFloorService.BottomLevelOffsetKey, "BottomLevelOffsetM");
            var topOffsetM = RequiredFinite(family, ProjectFloorService.TopLevelOffsetKey, "TopLevelOffsetM");
            var bottomFloor = FindFloor(project, bottomLevelId, "Cốt đáy Móng Bè");
            var topFloor = FindFloor(project, topLevelId, "Cốt đỉnh Móng Bè");
            var bottomElevationM = AddFinite(bottomFloor.ElevationM, bottomOffsetM, "Cốt đáy Móng Bè");
            var topElevationM = AddFinite(topFloor.ElevationM, topOffsetM, "Cốt đỉnh Móng Bè");

            if (!(topElevationM > bottomElevationM))
                throw new InvalidOperationException("Cao độ Móng Bè không hợp lệ: cốt đỉnh phải lớn hơn cốt đáy.");

            var thicknessM = RequirePositive(family, RaftFoundationPropertySet.ThicknessKey, "Chiều dày Móng Bè");
            var spanM = topElevationM - bottomElevationM;
            if (!IsFinite(spanM) || Math.Abs(spanM - thicknessM) > ElevationToleranceM)
                throw new InvalidOperationException(
                    "Quan hệ cao độ Móng Bè không khớp chiều dày. Span=" + spanM.ToString("R", CultureInfo.InvariantCulture) +
                    " m, ThicknessM=" + thicknessM.ToString("R", CultureInfo.InvariantCulture) + " m.");

            return new RaftFoundationVerticalPlacement(bottomElevationM, topElevationM);
        }

        private static FloorDefinition FindFloor(ProjectState project, string floorId, string caption)
        {
            return project.FindFloor(floorId)
                ?? throw new InvalidOperationException(caption + " tham chiếu Level không tồn tại: " + floorId + ".");
        }

        private static void ValidateUniqueFloorIds(ProjectState project)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in project.Floors)
            {
                if (floor == null) throw new InvalidOperationException("Project có Floor/Level null.");
                if (!ids.Add(floor.Id))
                    throw new InvalidOperationException("Project có Floor/Level id trùng: " + floor.Id + ".");
            }
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
            var value = RequiredFinite(family, key, caption);
            if (!(value > 0d)) throw new InvalidOperationException(caption + " phải > 0.");
            return value;
        }

        private static double RequiredFinite(ProjectFamily family, string key, string caption)
        {
            if (!family.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(caption + " chưa được nhập.");
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !IsFinite(value))
                throw new InvalidOperationException(caption + " phải là số invariant hữu hạn canonical.");
            return value == 0d ? 0d : value;
        }

        private static double OptionalFinite(ProjectFamily family, string key, double fallback, string caption)
        {
            if (!family.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !IsFinite(value))
                throw new InvalidOperationException(caption + " phải là số invariant hữu hạn canonical.");
            return value == 0d ? 0d : value;
        }

        private static double AddFinite(double left, double right, string caption)
        {
            var value = left + right;
            if (!IsFinite(value)) throw new InvalidOperationException(caption + " phải hữu hạn.");
            if ((right != 0d && value == left) || (left != 0d && value == right))
                throw new InvalidOperationException(caption + " mất độ chính xác khi cộng Level + offset.");
            return value == 0d ? 0d : value;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
