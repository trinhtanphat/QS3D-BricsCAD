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
            var liveHandleIdentities = liveSolidHandles == null
                ? null
                : new HashSet<string>(
                    liveSolidHandles
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity),
                    StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Curtain-frame diagnostics cannot inspect a project containing a null semantic element.");
                if (!element.Properties.TryGetValue(HandlesKey, out var raw)) continue;
                var rawHandles = raw ?? string.Empty;
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var validCount = 0;
                if (rawHandles.Length > 0)
                {
                    foreach (var item in rawHandles.Split(new[] { ';' }, StringSplitOptions.None))
                    {
                        var rawHandle = item ?? string.Empty;
                        var handle = rawHandle.Trim();
                        if (handle.Length > 0 && !string.Equals(rawHandle, handle, StringComparison.Ordinal))
                            issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_HANDLE_NON_CANONICAL", HealthSeverity.Error, HandlesKey + " không được có khoảng trắng quanh handle.", element.Id));
                        if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                        {
                            issues.Add(new ModelHealthIssue("INVALID_CURTAIN_FRAME_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " chứa handle không hợp lệ.", element.Id));
                            continue;
                        }
                        var handleIdentity = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
                        if (!local.Add(handleIdentity))
                        {
                            issues.Add(new ModelHealthIssue("DUPLICATE_CURTAIN_FRAME_GENERATED_HANDLE", HealthSeverity.Error, "Một curtain frame handle bị lặp trong cùng element: " + handle, element.Id));
                            continue;
                        }
                        validCount++;
                        var expected = element.Id + "/" + HandlesKey;
                        if (ownership.Conflicts.Contains(handleIdentity))
                            issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated curtain frame handle đang được nhiều project slot/element cùng claim: " + handle, element.Id));
                        else if (ownership.Owners.TryGetValue(handleIdentity, out var owner) && !string.Equals(owner, expected, StringComparison.OrdinalIgnoreCase))
                            issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated curtain frame xung đột owner/project handle khác: " + owner, element.Id));
                        if (element.SourceHandles.Any(x => string.Equals(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(x), handleIdentity, StringComparison.OrdinalIgnoreCase)))
                            issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated curtain frame handle không được nằm trong SourceHandles.", element.Id));
                        if (liveHandleIdentities != null && !liveHandleIdentities.Contains(handleIdentity))
                            issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated curtain frame Solid3d: " + handle, element.Id));
                    }
                }

                var count = Integer(element, "GeneratedCurtainFrameCount", issues, "CURTAIN_FRAME_COUNT_INVALID", true);
                var columns = Integer(element, "GeneratedCurtainFrameColumns", issues, "CURTAIN_FRAME_COLUMNS_INVALID");
                var rows = Integer(element, "GeneratedCurtainFrameRows", issues, "CURTAIN_FRAME_ROWS_INVALID");
                var baseCount = OptionalInteger(element, "GeneratedCurtainFrameBaseCount", true, issues, "CURTAIN_FRAME_BASE_COUNT_INVALID");
                var openingCount = OptionalInteger(element, "GeneratedCurtainFrameOpeningCount", true, issues, "CURTAIN_FRAME_OPENING_COUNT_INVALID") ?? 0;
                if (count.HasValue && count.Value != validCount)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedCurtainFrameCount không khớp số handle hợp lệ.", element.Id));

                var storedDepth = PositiveValue(element, "GeneratedCurtainFrameDepthM", "CURTAIN_FRAME_DEPTH_INVALID", "CURTAIN_FRAME_DEPTH_NON_CANONICAL", issues);
                var storedLength = PositiveValue(element, "GeneratedCurtainFrameSourceLengthM", "CURTAIN_FRAME_SOURCE_LENGTH_INVALID", "CURTAIN_FRAME_SOURCE_LENGTH_NON_CANONICAL", issues);
                var storedHeight = PositiveValue(element, "GeneratedCurtainFrameHeightM", "CURTAIN_FRAME_HEIGHT_INVALID", "CURTAIN_FRAME_HEIGHT_NON_CANONICAL", issues);
                CompareCurrent(element, "LengthM", storedLength, "CURTAIN_FRAME_SOURCE_LENGTH_STALE", issues);
                var family = project.FindFamily(element.FamilyId);
                CurtainWallFrameFingerprintInput? matchingCurrentConfig = null;
                try
                {
                    double currentHeight;
                    double currentBottom;
                    if (ElementVerticalPlacementService.HasAnyLevelConfiguration(element))
                    {
                        var probe = ElementVerticalPlacementService.Resolve(project, element, 0d, 1d, 0d);
                        var hasTop = element.Properties.TryGetValue(ProjectFloorService.TopLevelIdKey, out var topId) && !string.IsNullOrWhiteSpace(topId);
                        var placement = probe;
                        if (!hasTop)
                        {
                            var legacyHeight = Number(element, family, "HeightM", storedHeight ?? 3.6d, true);
                            placement = ElementVerticalPlacementService.Resolve(project, element, 0d, legacyHeight, 0d);
                        }
                        currentHeight = placement.HeightM;
                        currentBottom = placement.BottomElevationM;
                    }
                    else
                    {
                        currentHeight = Number(element, family, "HeightM", storedHeight ?? 3.6d, true);
                        currentBottom = Number(element, family, "BottomOffsetM", 0d, false);
                    }
                    CompareValue(element, "HeightM", currentHeight, storedHeight, "CURTAIN_FRAME_HEIGHT_STALE", issues);
                    matchingCurrentConfig = ValidateConfigFingerprint(project, element, storedLength, storedHeight, storedDepth, currentHeight, currentBottom, issues);
                }
                catch (Exception ex) when (IsConfigDataFailure(ex))
                {
                    issues.Add(new ModelHealthIssue(
                        "CURTAIN_FRAME_CONFIG_INVALID",
                        HealthSeverity.Warning,
                        "Không thể kiểm tra cao độ/config curtain frame hiện tại vì semantic/family config không hợp lệ.",
                        element.Id));
                }

                ValidateBaseFrameCount(element, count, columns, rows, baseCount, openingCount, matchingCurrentConfig, issues);

                var rawMode = element.Properties.TryGetValue("GeneratedCurtainFrameMode", out var modeRaw) ? modeRaw ?? string.Empty : string.Empty;
                var mode = rawMode.Trim();
                var lineMode = string.Equals(mode, "LineFrameOverlay", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "LineFrameOverlay.OpeningAware", StringComparison.OrdinalIgnoreCase);
                var pathMode = string.Equals(mode, "PathFrameOverlay", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "PathFrameOverlay.OpeningAware", StringComparison.OrdinalIgnoreCase);
                var openingAware = string.Equals(mode, "LineFrameOverlay.OpeningAware", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "PathFrameOverlay.OpeningAware", StringComparison.OrdinalIgnoreCase);
                var canonicalMode = lineMode
                    ? (openingAware ? "LineFrameOverlay.OpeningAware" : "LineFrameOverlay")
                    : pathMode
                        ? (openingAware ? "PathFrameOverlay.OpeningAware" : "PathFrameOverlay")
                        : string.Empty;
                if (canonicalMode.Length > 0 && !string.Equals(rawMode, canonicalMode, StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_MODE_NON_CANONICAL", HealthSeverity.Error, "GeneratedCurtainFrameMode phải dùng đúng writer-owned token: " + canonicalMode + ".", element.Id));
                if (!lineMode && !pathMode)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_MODE_INVALID", HealthSeverity.Warning, "GeneratedCurtainFrameMode thiếu hoặc không hợp lệ.", element.Id));
                else if (openingCount > 0 && !openingAware)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_OPENING_MODE_MISMATCH", HealthSeverity.Warning, "Curtain frame có linked opening nhưng metadata chưa ở opening-aware mode; rebuild curtain frames.", element.Id));
                else if (openingCount == 0 && openingAware)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_OPENING_MODE_MISMATCH", HealthSeverity.Warning, "Curtain frame opening-aware mode không khớp GeneratedCurtainFrameOpeningCount=0; rebuild curtain frames.", element.Id));

                if (pathMode)
                {
                    OptionalInteger(element, "GeneratedCurtainFramePathSegmentCount", false, issues, "CURTAIN_FRAME_PATH_SEGMENTS_INVALID");
                    OptionalInteger(element, "GeneratedCurtainFrameMappedFrameCount", true, issues, "CURTAIN_FRAME_MAPPED_COUNT_INVALID");
                    var rawSourceKind = element.Properties.TryGetValue("GeneratedCurtainFrameSourceKind", out var sourceKindRaw) ? sourceKindRaw ?? string.Empty : string.Empty;
                    var sourceKind = rawSourceKind.Trim();
                    if (!string.Equals(sourceKind, "OpenPolyline", StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID", HealthSeverity.Warning, "Path curtain frame cần GeneratedCurtainFrameSourceKind=OpenPolyline; rebuild curtain frames.", element.Id));
                    else if (!string.Equals(rawSourceKind, "OpenPolyline", StringComparison.Ordinal))
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_PATH_SOURCE_KIND_NON_CANONICAL", HealthSeverity.Error, "GeneratedCurtainFrameSourceKind phải dùng đúng writer-owned token OpenPolyline.", element.Id));
                }

                if (element.Category != ElementCategory.GlassWall)
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated curtain frame metadata chỉ hợp lệ trên GlassWall element.", element.Id));
                if (element.IsGeneratedCurtainFrameStale())
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GENERATED_STALE", HealthSeverity.Warning, "Curtain frame snapshot không còn khớp Family/Instance/source hiện tại; rebuild curtain frames trước khi phát hành bản vẽ.", element.Id));
            }
            return issues.AsReadOnly();
        }

        private static void ValidateBaseFrameCount(
            ProjectElement element,
            int? generatedCount,
            int? columns,
            int? rows,
            int? baseCount,
            int openingCount,
            CurtainWallFrameFingerprintInput? matchingCurrentConfig,
            List<ModelHealthIssue> issues)
        {
            if (!columns.HasValue || !rows.HasValue) return;

            int conceptualMaximum;
            try { conceptualMaximum = checked(columns.Value + rows.Value + 2); }
            catch (OverflowException)
            {
                issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GRID_COUNT_MISMATCH", HealthSeverity.Warning, "Curtain frame grid count bị overflow.", element.Id));
                return;
            }

            if (baseCount.HasValue)
            {
                if (baseCount.Value > conceptualMaximum)
                {
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GRID_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedCurtainFrameBaseCount vượt số vị trí frame tối đa của grid.", element.Id));
                    return;
                }

                if (matchingCurrentConfig != null)
                {
                    int expectedPhysical;
                    try { expectedPhysical = ExpectedPhysicalBaseFrameCount(columns.Value, rows.Value, matchingCurrentConfig); }
                    catch (OverflowException)
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GRID_COUNT_MISMATCH", HealthSeverity.Warning, "Curtain physical frame count bị overflow.", element.Id));
                        return;
                    }
                    if (baseCount.Value != expectedPhysical)
                        issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GRID_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedCurtainFrameBaseCount không khớp số physical frame có width > 0 của grid/config hiện tại.", element.Id));
                }
                return;
            }

            if (openingCount == 0 && generatedCount.HasValue && generatedCount.Value != conceptualMaximum)
                issues.Add(new ModelHealthIssue("CURTAIN_FRAME_GRID_COUNT_MISMATCH", HealthSeverity.Warning, "Legacy curtain frame count không khớp Columns+Rows+2.", element.Id));
        }

        private static int ExpectedPhysicalBaseFrameCount(int columns, int rows, CurtainWallFrameFingerprintInput config)
        {
            return checked(
                (config.PerimeterFrameWidthM > 0d ? 4 : 0) +
                (config.MullionWidthM > 0d ? columns - 1 : 0) +
                (config.TransomWidthM > 0d ? rows - 1 : 0));
        }

        private static CurtainWallFrameFingerprintInput? ValidateConfigFingerprint(
            ProjectState project,
            ProjectElement element,
            double? storedLength,
            double? storedHeight,
            double? storedDepth,
            double currentHeight,
            double currentBottom,
            List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue("GeneratedCurtainFrameConfigFingerprint", out var storedFingerprint) || string.IsNullOrWhiteSpace(storedFingerprint))
            {
                issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CONFIG_FINGERPRINT_MISSING", HealthSeverity.Warning, "Thiếu GeneratedCurtainFrameConfigFingerprint; rebuild curtain frames để nâng metadata.", element.Id));
                return null;
            }
            if (!storedLength.HasValue || !storedHeight.HasValue || !storedDepth.HasValue) return null;
            try
            {
                var family = project.FindFamily(element.FamilyId);
                var input = new CurtainWallFrameFingerprintInput
                {
                    LengthM = Number(element, family, "LengthM", storedLength.Value, true),
                    HeightM = currentHeight,
                    BottomOffsetM = currentBottom,
                    MaxPanelWidthM = Number(element, family, "CurtainMaxPanelWidthM", 1.2d, true),
                    MaxPanelHeightM = Number(element, family, "CurtainMaxPanelHeightM", 1.5d, true),
                    PerimeterFrameWidthM = Number(element, family, "CurtainPerimeterFrameWidthM", 0.05d, false, true),
                    MullionWidthM = Number(element, family, "CurtainMullionWidthM", 0.05d, false, true),
                    TransomWidthM = Number(element, family, "CurtainTransomWidthM", 0.05d, false, true),
                    FrameDepthM = Number(element, family, "CurtainFrameDepthM", storedDepth.Value, true)
                };
                var current = CurtainWallFrameFingerprint.Compute(input);
                var normalizedStored = storedFingerprint.Trim();
                if (!string.Equals(current, normalizedStored, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CONFIG_STALE", HealthSeverity.Warning, "Panel grid/frame depth/offset hiện tại không còn khớp generated curtain frames; rebuild curtain frames.", element.Id));
                    return null;
                }
                if (!string.Equals(current, storedFingerprint, StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue("CURTAIN_FRAME_CONFIG_FINGERPRINT_NON_CANONICAL", HealthSeverity.Error, "GeneratedCurtainFrameConfigFingerprint phải dùng đúng lowercase SHA-256 writer-owned spelling.", element.Id));
                return input;
            }
            catch (Exception ex) when (IsConfigDataFailure(ex))
            {
                issues.Add(new ModelHealthIssue(
                    "CURTAIN_FRAME_CONFIG_INVALID",
                    HealthSeverity.Warning,
                    "Không thể kiểm tra curtain-frame config hiện tại vì semantic/family config không hợp lệ.",
                    element.Id));
                return null;
            }
        }

        private static bool IsConfigDataFailure(Exception exception)
        {
            return exception is InvalidOperationException || exception is ArgumentException;
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

        private static int? Integer(ProjectElement element, string key, List<ModelHealthIssue> issues, string code, bool allowZero = false)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || (allowZero ? value < 0 : value < 1))
            {
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, key + " thiếu hoặc không hợp lệ.", element.Id));
                return null;
            }
            ValidateIntegerCanonicality(element, key, raw, value, issues);
            return value;
        }

        private static int? OptionalInteger(ProjectElement element, string key, bool allowZero, List<ModelHealthIssue> issues, string code)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return null;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || (allowZero ? value < 0 : value < 1))
            {
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, key + " không hợp lệ.", element.Id));
                return null;
            }
            ValidateIntegerCanonicality(element, key, raw, value, issues);
            return value;
        }

        private static void ValidateIntegerCanonicality(ProjectElement element, string key, string raw, int value, List<ModelHealthIssue> issues)
        {
            var canonical = value.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL", HealthSeverity.Error, key + " phải dùng đúng invariant integer spelling: " + canonical + ".", element.Id));
        }

        private static double? PositiveValue(ProjectElement element, string key, string code, string canonicalCode, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, key + " thiếu hoặc không hợp lệ.", element.Id));
                return null;
            }
            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue(canonicalCode, HealthSeverity.Error, key + " phải dùng đúng round-trip invariant numeric spelling: " + canonical + ".", element.Id));
            return value;
        }

        private static void CompareCurrent(ProjectElement element, string currentKey, double? stored, string code, List<ModelHealthIssue> issues)
        {
            if (!stored.HasValue || !element.Properties.TryGetValue(currentKey, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var current) || double.IsNaN(current) || double.IsInfinity(current) || current <= 0d) return;
            var tolerance = Math.Max(1e-8d, Math.Max(Math.Abs(current), Math.Abs(stored.Value)) * 1e-8d);
            if (Math.Abs(current - stored.Value) > tolerance)
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, "Curtain frame geometry không còn khớp " + currentKey + " hiện tại; rebuild curtain frames.", element.Id));
        }

        private static void CompareValue(ProjectElement element, string label, double current, double? stored, string code, List<ModelHealthIssue> issues)
        {
            if (!stored.HasValue || double.IsNaN(current) || double.IsInfinity(current) || current <= 0d) return;
            var tolerance = Math.Max(1e-8d, Math.Max(Math.Abs(current), Math.Abs(stored.Value)) * 1e-8d);
            if (Math.Abs(current - stored.Value) > tolerance)
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, "Curtain frame geometry không còn khớp " + label + " hiệu dụng; rebuild curtain frames.", element.Id));
        }

        private static OwnershipIndex BuildOwnershipIndex(ProjectState project)
        {
            var ownership = new OwnershipIndex();
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Curtain-frame diagnostics cannot build ownership for a project containing a null semantic element.");
                foreach (var handle in element.SourceHandles) Reserve(ownership, handle, element.Id + "/SourceHandles");
                foreach (var property in element.Properties)
                {
                    if (!GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)) continue;
                    ReserveProperty(ownership, element, property.Key);
                }
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
            var normalized = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
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
