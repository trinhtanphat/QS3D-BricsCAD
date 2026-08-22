using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class LevelReferenceHealthService
    {
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var floors = new Dictionary<string, FloorDefinition>(StringComparer.OrdinalIgnoreCase);
            var duplicateFloorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in project.Floors)
            {
                if (floor == null)
                    throw new InvalidOperationException("Level-reference diagnostics cannot inspect a project containing a null Floor/Level entry.");
                if (string.IsNullOrWhiteSpace(floor.Id))
                    throw new InvalidOperationException("Level-reference diagnostics cannot inspect a Floor/Level with a blank semantic id.");
                var id = floor.Id.Trim();
                if (!floors.ContainsKey(id))
                {
                    floors[id] = floor;
                    continue;
                }
                if (duplicateFloorIds.Add(id))
                    issues.Add(new ModelHealthIssue("DUPLICATE_LEVEL_ID", HealthSeverity.Error, "Trùng mã Floor/Level: " + id + ".", id));
            }

            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Level-reference diagnostics cannot inspect a project containing a null semantic element.");
                var issueCountBefore = issues.Count;
                var bottomRaw = RawProperty(element, ProjectFloorService.BottomLevelIdKey);
                var topRaw = RawProperty(element, ProjectFloorService.TopLevelIdKey);
                var bottomId = bottomRaw.Trim();
                var topId = topRaw.Trim();
                var hasBottomOffset = HasProperty(element, ProjectFloorService.BottomLevelOffsetKey);
                var hasTopOffset = HasProperty(element, ProjectFloorService.TopLevelOffsetKey);

                if (!string.Equals(bottomRaw, bottomId, StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue(
                        "BOTTOM_LEVEL_REFERENCE_NON_CANONICAL",
                        HealthSeverity.Error,
                        "BottomLevelId phải dùng đúng canonical Floor/Level ID, không có khoảng trắng đầu/cuối.",
                        element.Id));
                if (!string.Equals(topRaw, topId, StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue(
                        "TOP_LEVEL_REFERENCE_NON_CANONICAL",
                        HealthSeverity.Error,
                        "TopLevelId phải dùng đúng canonical Floor/Level ID, không có khoảng trắng đầu/cuối.",
                        element.Id));

                if (bottomId.Length == 0)
                {
                    if (topId.Length > 0)
                        issues.Add(new ModelHealthIssue("TOP_LEVEL_REQUIRES_BOTTOM_LEVEL", HealthSeverity.Error, "TopLevelId yêu cầu BottomLevelId trên cùng cấu kiện.", element.Id));
                    if (hasBottomOffset)
                        issues.Add(new ModelHealthIssue("BOTTOM_LEVEL_OFFSET_WITHOUT_LEVEL", HealthSeverity.Error, "BottomLevelOffsetM chỉ hợp lệ khi có BottomLevelId.", element.Id));
                    if (hasTopOffset && topId.Length == 0)
                        issues.Add(new ModelHealthIssue("TOP_LEVEL_OFFSET_WITHOUT_LEVEL", HealthSeverity.Error, "TopLevelOffsetM chỉ hợp lệ khi có TopLevelId.", element.Id));
                    continue;
                }

                FloorDefinition? bottom = null;
                if (duplicateFloorIds.Contains(bottomId))
                {
                    issues.Add(new ModelHealthIssue("BOTTOM_LEVEL_REFERENCE_AMBIGUOUS", HealthSeverity.Error, "BottomLevelId trỏ tới mã Floor/Level bị trùng: " + bottomId + ".", element.Id));
                }
                else if (!floors.TryGetValue(bottomId, out bottom))
                {
                    issues.Add(new ModelHealthIssue("BOTTOM_LEVEL_REFERENCE_INVALID", HealthSeverity.Error, "BottomLevelId không trỏ tới Level/Tầng còn tồn tại: " + bottomId, element.Id));
                }

                var bottomOffsetValid = TryOffset(element, ProjectFloorService.BottomLevelOffsetKey, out var bottomOffset);
                if (!bottomOffsetValid)
                    issues.Add(new ModelHealthIssue("BOTTOM_LEVEL_OFFSET_INVALID", HealthSeverity.Error, "BottomLevelOffsetM phải là số hữu hạn.", element.Id));

                if (topId.Length == 0)
                {
                    if (hasTopOffset)
                        issues.Add(new ModelHealthIssue("TOP_LEVEL_OFFSET_WITHOUT_LEVEL", HealthSeverity.Error, "TopLevelOffsetM chỉ hợp lệ khi có TopLevelId.", element.Id));
                    AddNativeIntegrationPendingIfSemanticallyValid(issues, issueCountBefore, element);
                    continue;
                }

                FloorDefinition? top = null;
                if (duplicateFloorIds.Contains(topId))
                {
                    issues.Add(new ModelHealthIssue("TOP_LEVEL_REFERENCE_AMBIGUOUS", HealthSeverity.Error, "TopLevelId trỏ tới mã Floor/Level bị trùng: " + topId + ".", element.Id));
                }
                else if (!floors.TryGetValue(topId, out top))
                {
                    issues.Add(new ModelHealthIssue("TOP_LEVEL_REFERENCE_INVALID", HealthSeverity.Error, "TopLevelId không trỏ tới Level/Tầng còn tồn tại: " + topId, element.Id));
                }

                var topOffsetValid = TryOffset(element, ProjectFloorService.TopLevelOffsetKey, out var topOffset);
                if (!topOffsetValid)
                    issues.Add(new ModelHealthIssue("TOP_LEVEL_OFFSET_INVALID", HealthSeverity.Error, "TopLevelOffsetM phải là số hữu hạn.", element.Id));

                if (bottom != null && top != null && bottomOffsetValid && topOffsetValid)
                {
                    var bottomElevation = bottom.ElevationM + bottomOffset;
                    var topElevation = top.ElevationM + topOffset;
                    if (double.IsNaN(bottomElevation) || double.IsInfinity(bottomElevation) ||
                        double.IsNaN(topElevation) || double.IsInfinity(topElevation) || topElevation <= bottomElevation)
                    {
                        issues.Add(new ModelHealthIssue("LEVEL_RANGE_INVALID", HealthSeverity.Error, "Cao độ Level đỉnh + offset phải lớn hơn Level đáy + offset.", element.Id));
                    }
                }

                AddNativeIntegrationPendingIfSemanticallyValid(issues, issueCountBefore, element);
            }
            return issues.AsReadOnly();
        }

        private static void AddNativeIntegrationPendingIfSemanticallyValid(List<ModelHealthIssue> issues, int issueCountBefore, ProjectElement element)
        {
            if (issues.Count != issueCountBefore || LevelReferenceNativeIntegrationPolicy.IsQualified(element.Category)) return;
            issues.Add(new ModelHealthIssue(
                "LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING",
                HealthSeverity.Error,
                "Level reference hợp lệ về semantic nhưng native host/dependent placement của category này chưa được qualification dùng chung ElementVerticalPlacementService. Giữ Release blocked cho tới khi native integration + V25 proof hoàn tất.",
                element.Id));
        }

        private static string RawProperty(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? raw ?? string.Empty : string.Empty;

        private static bool HasProperty(ProjectElement element, string key) =>
            element.Properties.ContainsKey(key);

        private static bool TryOffset(ProjectElement element, string key, out double value)
        {
            value = 0d;
            if (!element.Properties.TryGetValue(key, out var raw)) return true;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            return double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
