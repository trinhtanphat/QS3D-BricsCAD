using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedCurtainFrameHealthService
    {
        private const string HandlesKey = "GeneratedCurtainFrameHandles";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var owners = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var validCount = 0;
                foreach (var item in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var handle = (item ?? string.Empty).Trim();
                    if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue("INVALID_CURTAIN_FRAME_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!local.Add(handle))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_CURTAIN_FRAME_GENERATED_HANDLE", HealthSeverity.Error, "Một curtain frame handle bị lặp trong cùng element: " + handle, element.Id));
                        continue;
                    }
                    validCount++;
                    var expected = element.Id + "/" + HandlesKey;
                    if (owners.TryGetValue(handle, out var owner) && !string.Equals(owner, expected, StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated curtain frame xung đột owner/project handle khác: " + owner, element.Id));
                    if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated curtain frame handle không được nằm trong SourceHandles.", element.Id));
                    if (liveSolidHandles != null && !liveSolidHandles.Contains(handle))
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated curtain frame Solid3d: " + handle, element.Id));
                }

                var count = Integer(element, "GeneratedCurtainFrameCount", issues, "CURTAIN_FRAME_COUNT_INVALID");
                var columns = Integer(element, "GeneratedCurtainFrameColumns", issues, "CURTAIN_FRAME_COLUMNS_INVALID");
                var rows = Integer(element, "GeneratedCurtainFrameRows", issues, "CURTAIN_FRAME_ROWS_INVALID");
                if (count.HasValue && count.Value != validCount)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedCurtainFrameCount không khớp số handle hợp lệ.", element.Id));
                if (count.HasValue && columns.HasValue && rows.HasValue)
                {
                    int expectedCount;
                    try { expectedCount = checked(columns.Value + rows.Value + 2); }
                    catch (OverflowException) { expectedCount = -1; }
                    if (expectedCount < 0 || count.Value != expectedCount)
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GRID_COUNT_MISMATCH", HealthSeverity.Warning, "Số frame không khớp Columns+Rows+2.", element.Id));
                }

                ValidatePositive(element, "GeneratedCurtainFrameDepthM", "CURTAIN_FRAME_DEPTH_INVALID", issues);
                var storedLength = PositiveValue(element, "GeneratedCurtainFrameSourceLengthM", "CURTAIN_FRAME_SOURCE_LENGTH_INVALID", issues);
                var storedHeight = PositiveValue(element, "GeneratedCurtainFrameHeightM", "CURTAIN_FRAME_HEIGHT_INVALID", issues);
                CompareCurrent(element, "LengthM", storedLength, "CURTAIN_FRAME_SOURCE_LENGTH_STALE", issues);
                CompareCurrent(element, "HeightM", storedHeight, "CURTAIN_FRAME_HEIGHT_STALE", issues);

                if (!element.Properties.TryGetValue("GeneratedCurtainFrameMode", out var mode) || !string.Equals(mode, "LineFrameOverlay", StringComparison.OrdinalIgnoreCase))
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_MODE_INVALID", HealthSeverity.Warning, "GeneratedCurtainFrameMode thiếu hoặc không hợp lệ.", element.Id));
                if (element.Category != ElementCategory.GlassWall)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated curtain frame metadata chỉ hợp lệ trên GlassWall element.", element.Id));
                if (element.Dirty != ElementDirtyFlags.None)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_STALE", HealthSeverity.Warning, "GlassWall đang dirty nhưng vẫn còn curtain frame solids; rebuild trước khi phát hành bản vẽ.", element.Id));
            }
            return issues;
        }

        private static int? Integer(ProjectElement element, string key, List<ModelHealthIssue> issues, string code)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 1)
            {
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, key + " thiếu hoặc không hợp lệ.", element.Id));
                return null;
            }
            return value;
        }

        private static void ValidatePositive(ProjectElement element, string key, string code, List<ModelHealthIssue> issues) => PositiveValue(element, key, code, issues);

        private static double? PositiveValue(ProjectElement element, string key, string code, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, key + " thiếu hoặc không hợp lệ.", element.Id));
                return null;
            }
            return value;
        }

        private static void CompareCurrent(ProjectElement element, string currentKey, double? stored, string code, List<ModelHealthIssue> issues)
        {
            if (!stored.HasValue || !element.Properties.TryGetValue(currentKey, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var current) || double.IsNaN(current) || double.IsInfinity(current) || current <= 0d) return;
            var tolerance = Math.Max(1e-8d, Math.Max(Math.Abs(current), Math.Abs(stored.Value)) * 1e-8d);
            if (Math.Abs(current - stored.Value) > tolerance)
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, "Curtain frame geometry không còn khớp " + currentKey + " hiện tại; rebuild curtain frames.", element.Id));
        }

        private static Dictionary<string, string> BuildOwnershipIndex(ProjectState project)
        {
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles) Reserve(owners, handle, element.Id + "/SourceHandles");
                foreach (var key in new[] { "GeneratedSolidHandle", "PhysicalOpeningCutSolidHandle", "GeneratedRebarHandles", "GeneratedShapeRebarHandles", "GeneratedTieRebarHandles", "GeneratedBeamStirrupHandles", "GeneratedSlabMeshHandles", "GeneratedWallMeshHandles", HandlesKey })
                    ReserveProperty(owners, element, key);
            }
            return owners;
        }

        private static void ReserveProperty(Dictionary<string, string> owners, ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                Reserve(owners, handle, element.Id + "/" + key);
        }

        private static void Reserve(Dictionary<string, string> owners, string? handle, string token)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0 || owners.ContainsKey(normalized)) return;
            owners[normalized] = token;
        }
    }
}
