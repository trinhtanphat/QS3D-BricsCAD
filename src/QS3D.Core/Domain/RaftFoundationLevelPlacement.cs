using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Domain
{
    public sealed class RaftFoundationVerticalPlacement
    {
        public RaftFoundationVerticalPlacement(double bottomElevationM, double topElevationM)
        {
            if (!IsFinite(bottomElevationM) || !IsFinite(topElevationM) || !(topElevationM > bottomElevationM))
                throw new ArgumentOutOfRangeException(nameof(topElevationM), "Cao độ Móng Bè phải hữu hạn và đỉnh phải cao hơn đáy.");
            BottomElevationM = bottomElevationM == 0d ? 0d : bottomElevationM;
            TopElevationM = topElevationM == 0d ? 0d : topElevationM;
        }

        public double BottomElevationM { get; }
        public double TopElevationM { get; }
        public double ThicknessM => TopElevationM - BottomElevationM;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    /// Móng Bè has one authoritative Level relationship. bottom_level anchors the bottom face;
    /// top_level anchors the top face. The inactive relationship must be empty so geometry,
    /// metadata and quantity cannot disagree about which face owns the selected Level.
    /// </summary>
    public static class RaftFoundationLevelPlacement
    {
        public static bool EnsureDefaults(ProjectState project, ProjectFamily family)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (!RaftFoundationPropertySet.IsRaftFamily(family))
                throw new InvalidOperationException("Family không phải Móng Bè.");

            var thicknessM = RequirePositive(family.Properties, RaftFoundationPropertySet.ThicknessKey, "Dày Móng Bè");
            var mode = RaftFoundationPropertySet.NormalizeElevationMode(Property(family.Properties, RaftFoundationPropertySet.ElevationModeKey));
            var activeKey = RaftFoundationPropertySet.ActiveLevelKey(mode);
            var oppositeKey = RaftFoundationPropertySet.OppositeLevelKey(mode);
            var levelId = Property(family.Properties, activeKey);
            if (levelId.Length == 0) levelId = Property(family.Properties, oppositeKey);
            if (levelId.Length == 0) levelId = (project.ActiveFloorId ?? string.Empty).Trim();
            if (levelId.Length == 0)
                throw new InvalidOperationException("Móng Bè cần một Tầng/Level đang hoạt động trước khi Add Family.");
            var floor = FindFloor(project, levelId, "Cao độ đầu Móng Bè");
            ValidateUniqueFloorIds(project);

            var before = Snapshot(family.Properties);
            var candidate = Snapshot(family.Properties);
            candidate[RaftFoundationPropertySet.ElevationModeKey] = mode;
            candidate[activeKey] = floor.Id;
            candidate.Remove(oppositeKey);
            candidate.Remove(ProjectFloorService.BottomLevelOffsetKey);
            candidate.Remove(ProjectFloorService.TopLevelOffsetKey);
            candidate[RaftFoundationPropertySet.BottomOffsetKey] =
                RaftFoundationPropertySet.ResolveBottomOffsetM(mode, thicknessM).ToString("R", CultureInfo.InvariantCulture);

            // Validate the complete intended state while it is still detached from the live Family.
            // No Family/project mutation may occur before both this validation and revision admission.
            ResolveCore(project, candidate, null, family.Name);
            RequireRevisionHeadroom(project, family, before, candidate);

            family.Properties[RaftFoundationPropertySet.ElevationModeKey] = mode;
            family.Properties[activeKey] = floor.Id;
            family.Properties.Remove(oppositeKey);
            family.Properties.Remove(ProjectFloorService.BottomLevelOffsetKey);
            family.Properties.Remove(ProjectFloorService.TopLevelOffsetKey);
            family.Properties[RaftFoundationPropertySet.BottomOffsetKey] =
                RaftFoundationPropertySet.ResolveBottomOffsetM(mode, thicknessM).ToString("R", CultureInfo.InvariantCulture);
            return !DictionaryEqual(before, family.Properties);
        }

        public static RaftFoundationVerticalPlacement Resolve(ProjectState project, ProjectFamily family)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (!RaftFoundationPropertySet.IsRaftFamily(family))
                throw new InvalidOperationException("Family không phải Móng Bè.");
            return ResolveCore(project, family.Properties, null, family.Name);
        }

        public static RaftFoundationVerticalPlacement Resolve(ProjectState project, ProjectElement element, ProjectFamily? family)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!RaftFoundationPropertySet.IsRaftElement(element, family))
                throw new InvalidOperationException("Cấu kiện không phải Móng Bè.");
            return ResolveCore(project, element.Properties, family?.Properties, element.Id);
        }

        public static RaftFoundationVerticalPlacement ApplyFamilyPlacementToElement(
            ProjectState project,
            ProjectFamily family,
            ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (element == null) throw new ArgumentNullException(nameof(element));
            var placement = Resolve(project, family);
            var mode = RaftFoundationPropertySet.NormalizeElevationMode(Property(family.Properties, RaftFoundationPropertySet.ElevationModeKey));
            var activeKey = RaftFoundationPropertySet.ActiveLevelKey(mode);
            var oppositeKey = RaftFoundationPropertySet.OppositeLevelKey(mode);
            var levelId = RequiredText(family.Properties, activeKey, "Cao độ đầu Móng Bè");
            var thicknessM = RequirePositive(family.Properties, RaftFoundationPropertySet.ThicknessKey, "Dày Móng Bè");

            element.SetProperty(RaftFoundationPropertySet.WorkspaceSubtypeKey, RaftFoundationPropertySet.SubtypeName);
            element.SetProperty(RaftFoundationPropertySet.ThicknessKey, thicknessM.ToString("R", CultureInfo.InvariantCulture));
            element.SetProperty(RaftFoundationPropertySet.ElevationModeKey, mode);
            element.SetProperty(activeKey, levelId);
            element.Properties.Remove(oppositeKey);
            element.Properties.Remove(ProjectFloorService.BottomLevelOffsetKey);
            element.Properties.Remove(ProjectFloorService.TopLevelOffsetKey);
            element.SetProperty(
                RaftFoundationPropertySet.BottomOffsetKey,
                RaftFoundationPropertySet.ResolveBottomOffsetM(mode, thicknessM).ToString("R", CultureInfo.InvariantCulture));
            var copied = Resolve(project, element, family);
            if (!NearlyEqual(copied.BottomElevationM, placement.BottomElevationM) ||
                !NearlyEqual(copied.TopElevationM, placement.TopElevationM))
                throw new InvalidOperationException("Placement Móng Bè trên element lệch khỏi Family nguồn.");
            return copied;
        }

        public static string ResolveMode(IDictionary<string, string> properties, IDictionary<string, string>? fallback = null)
        {
            var raw = Property(properties, RaftFoundationPropertySet.ElevationModeKey);
            if (raw.Length == 0 && fallback != null) raw = Property(fallback, RaftFoundationPropertySet.ElevationModeKey);
            return RaftFoundationPropertySet.NormalizeElevationMode(raw);
        }

        private static RaftFoundationVerticalPlacement ResolveCore(
            ProjectState project,
            IDictionary<string, string> properties,
            IDictionary<string, string>? fallback,
            string owner)
        {
            ValidateUniqueFloorIds(project);
            var mode = ResolveMode(properties, fallback);
            var activeKey = RaftFoundationPropertySet.ActiveLevelKey(mode);
            var oppositeKey = RaftFoundationPropertySet.OppositeLevelKey(mode);
            var levelId = Property(properties, activeKey);
            if (levelId.Length == 0 && fallback != null) levelId = Property(fallback, activeKey);
            if (levelId.Length == 0)
                throw new InvalidOperationException(owner + ": Cao độ đầu chưa chọn Level cho " + mode + ".");

            var opposite = Property(properties, oppositeKey);
            if (opposite.Length == 0 && fallback != null && !properties.ContainsKey(oppositeKey)) opposite = Property(fallback, oppositeKey);
            if (opposite.Length != 0)
                throw new InvalidOperationException(owner + ": Móng Bè chỉ được giữ một Level binding; " + oppositeKey + " phải trống khi Cách đặt=" + mode + ".");

            var thicknessM = Number(properties, fallback, RaftFoundationPropertySet.ThicknessKey, "Dày Móng Bè");
            if (!(thicknessM > 0d)) throw new InvalidOperationException(owner + ": Dày Móng Bè phải > 0.");
            var level = FindFloor(project, levelId, "Cao độ đầu Móng Bè");
            double bottom;
            double top;
            if (string.Equals(mode, RaftFoundationPropertySet.TopLevelMode, StringComparison.Ordinal))
            {
                top = level.ElevationM;
                bottom = top - thicknessM;
            }
            else
            {
                bottom = level.ElevationM;
                top = bottom + thicknessM;
            }
            if (!IsFinite(bottom) || !IsFinite(top) || !(top > bottom))
                throw new InvalidOperationException(owner + ": cao độ Móng Bè sau khi resolve không hợp lệ.");
            return new RaftFoundationVerticalPlacement(bottom, top);
        }

        private static FloorDefinition FindFloor(ProjectState project, string floorId, string caption)
        {
            var normalized = (floorId ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidOperationException(caption + " chưa được chọn.");
            return project.FindFloor(normalized)
                ?? throw new InvalidOperationException(caption + " tham chiếu Level không tồn tại: " + normalized + ".");
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

        private static void RequireRevisionHeadroom(
            ProjectState project,
            ProjectFamily family,
            IDictionary<string, string> before,
            IDictionary<string, string> candidate)
        {
            if (!project.Families.Any(x => ReferenceEquals(x, family))) return;
            var requiredMutations = CountPropertyMutations(before, candidate);
            if (requiredMutations > long.MaxValue - project.ChangeVersion)
                throw new InvalidOperationException(
                    "Móng Bè defaults require " + requiredMutations +
                    " project revision advance(s), but the project revision has insufficient remaining capacity.");
        }

        private static long CountPropertyMutations(
            IDictionary<string, string> before,
            IDictionary<string, string> candidate)
        {
            long requiredMutations = 0L;
            foreach (var pair in before)
            {
                if (!candidate.TryGetValue(pair.Key, out var candidateValue) ||
                    !string.Equals(pair.Value, candidateValue, StringComparison.Ordinal))
                    requiredMutations++;
            }
            foreach (var pair in candidate)
            {
                if (!before.ContainsKey(pair.Key)) requiredMutations++;
            }
            return requiredMutations;
        }

        private static string RequiredText(IDictionary<string, string> properties, string key, string caption)
        {
            var value = Property(properties, key);
            if (value.Length == 0) throw new InvalidOperationException(caption + " chưa được chọn.");
            return value;
        }

        private static double RequirePositive(IDictionary<string, string> properties, string key, string caption)
        {
            var value = Number(properties, null, key, caption);
            if (!(value > 0d)) throw new InvalidOperationException(caption + " phải > 0.");
            return value;
        }

        private static double Number(
            IDictionary<string, string> properties,
            IDictionary<string, string>? fallback,
            string key,
            string caption)
        {
            var raw = Property(properties, key);
            if (raw.Length == 0 && fallback != null) raw = Property(fallback, key);
            if (raw.Length == 0 ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !IsFinite(value))
                throw new InvalidOperationException(caption + " phải là số invariant hữu hạn.");
            return value == 0d ? 0d : value;
        }

        private static string Property(IDictionary<string, string> properties, string key) =>
            properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;

        private static Dictionary<string, string> Snapshot(IDictionary<string, string> source) =>
            new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);

        private static bool DictionaryEqual(IDictionary<string, string> left, IDictionary<string, string> right)
        {
            if (left.Count != right.Count) return false;
            foreach (var pair in left)
                if (!right.TryGetValue(pair.Key, out var value) || !string.Equals(pair.Value, value, StringComparison.Ordinal)) return false;
            return true;
        }

        private static bool NearlyEqual(double left, double right) => Math.Abs(left - right) <= 1e-9d;
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
