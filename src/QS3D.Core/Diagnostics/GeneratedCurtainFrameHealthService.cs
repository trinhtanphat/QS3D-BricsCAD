using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedCurtainFrameHealthService
    {
        private const string HandlesKey = "GeneratedCurtainFrameHandles";

        private sealed class OwnershipIndex
        {
            public Dictionary<string, string> Owners { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Conflicts { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var ownership = BuildOwnershipIndex(project);
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
                    if (ownership.Conflicts.Contains(handle))
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated curtain frame handle đang được nhiều project slot/element cùng claim: " + handle, element.Id));
                    else if (ownership.Owners.TryGetValue(handle, out var owner) && !string.Equals(owner, expected, StringComparison.OrdinalIgnoreCase))
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

                var storedDepth = PositiveValue(element, "GeneratedCurtainFrameDepthM", "CURTAIN_FRAME_DEPTH_INVALID", issues);
                var storedLength = PositiveValue(element, "GeneratedCurtainFrameSourceLengthM", "CURTAIN_FRAME_SOURCE_LENGTH_INVALID", issues);
                var storedHeight = PositiveValue(element, "GeneratedCurtainFrameHeightM", "CURTAIN_FRAME_HEIGHT_INVALID", issues);
                CompareCurrent(element, "LengthM", storedLength, "CURTAIN_FRAME_SOURCE_LENGTH_STALE", issues);
                CompareCurrent(element, "HeightM", storedHeight, "CURTAIN_FRAME_HEIGHT_STALE", issues);
                ValidateConfigFingerprint(project, element, storedLength, storedHeight, storedDepth, issues);

                if (!element.Properties.TryGetValue("GeneratedCurtainFrameMode", out var mode) || !string.Equals(mode, "LineFrameOverlay", StringComparison.OrdinalIgnoreCase))
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_MODE_INVALID", HealthSeverity.Warning, "GeneratedCurtainFrameMode thiếu hoặc không hợp lệ.", element.Id));
                if (element.Category != ElementCategory.GlassWall)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated curtain frame metadata chỉ hợp lệ trên GlassWall element.", element.Id));
                if (element.IsGeneratedCurtainFrameStale())
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_STALE", HealthSeverity.Warning, "Curtain frame snapshot không còn khớp Family/Instance/source hiện tại; rebuild curtain frames trước khi phát hành bản vẽ.", element.Id));
            }
            return issues;
        }

        private static void ValidateConfigFingerprint(ProjectState project, ProjectElement element, double? storedLength, double? storedHeight, double? storedDepth, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue("GeneratedCurtainFrameConfigFingerprint", out var storedFingerprint) || string.IsNullOrWhiteSpace(storedFingerprint))
            {
                issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CONFIG_FINGERPRINT_MISSING", HealthSeverity.Warning, "Thiếu GeneratedCurtainFrameConfigFingerprint; rebuild curtain frames để nâng metadata.", element.Id));
                return;
            }
            if (!storedLength.HasValue || !storedHeight.HasValue || !storedDepth.HasValue) return;
            var family = project.FindFamily(element.FamilyId);
            try
            {
                var current = CurtainWallFrameFingerprint.Compute(new CurtainWallFrameFingerprintInput
                {
                    LengthM = Number(element, family, "LengthM", storedLength.Value, true),
                    HeightM = Number(element, family, "HeightM", storedHeight.Value, true),
                    BottomOffsetM = Number(element, family, "BottomOffsetM", 0d, false),
                    MaxPanelWidthM = Number(element, family, "CurtainMaxPanelWidthM", 1.2d, true),
                    MaxPanelHeightM = Number(element, family, "CurtainMaxPanelHeightM", 1.5d, true),
                    PerimeterFrameWidthM = Number(element, family, "CurtainPerimeterFrameWidthM", 0.05d, false, true),
                    MullionWidthM = Number(element, family, "CurtainMullionWidthM", 0.05d, false, true),
                    TransomWidthM = Number(element, family, "CurtainTransomWidthM", 0.05d, false, true),
                    FrameDepthM = Number(element, family, "CurtainFrameDepthM", storedDepth.Value, true)
                });
                if (!string.Equals(current, storedFingerprint.Trim(), StringComparison.OrdinalIgnoreCase))
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CONFIG_STALE", HealthSeverity.Warning, "Panel grid/frame depth/offset hiện tại không còn khớp generated curtain frames; rebuild curtain frames.", element.Id));
            }
            catch (Exception ex)
            {
                issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CONFIG_INVALID", HealthSeverity.Warning, "Không thể kiểm tra curtain-frame config hiện tại: " + ex.Message, element.Id));
            }
        }

        private static double Number(ProjectElement element, ProjectFamily? family, string key, double fallback, bool positive, bool nonNegative = false)
        {
            var raw = element.Properties.TryGetValue(key, out var own) && !string.IsNullOrWhiteSpace(own)
                ? own
                : family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)
                    ? inherited
                    : null;
            var value = fallback;
            if (raw != null && (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value)))
                throw new InvalidOperationException(key + " không phải số hữu hạn.");
            if (positive && value <= 0d) throw new InvalidOperationException(key + " phải > 0.");
            if (nonNegative && value < 0d) throw new InvalidOperationException(key + " phải >= 0.");
            return value;
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

        private static OwnershipIndex BuildOwnershipIndex(ProjectState project)
        {
            var ownership = new OwnershipIndex();
            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles) Reserve(ownership, handle, element.Id + "/SourceHandles");
                foreach (var key in new[] { "GeneratedSolidHandle", "PhysicalOpeningCutSolidHandle", "GeneratedRebarHandles", "GeneratedShapeRebarHandles", "GeneratedTieRebarHandles", "GeneratedBeamStirrupHandles", "GeneratedSlabMeshHandles", "GeneratedWallMeshHandles", HandlesKey })
                    ReserveProperty(ownership, element, key);
            }
            return ownership;
        }

        private static void ReserveProperty(OwnershipIndex ownership, ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                Reserve(ownership, handle, element.Id + "/" + key);
        }

        private static void Reserve(OwnershipIndex ownership, string? handle, string token)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0) return;
            if (ownership.Owners.TryGetValue(normalized, out var existing))
            {
                if (!string.Equals(existing, token, StringComparison.OrdinalIgnoreCase)) ownership.Conflicts.Add(normalized);
                return;
            }
            ownership.Owners[normalized] = token;
        }
    }
}
