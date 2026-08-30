using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedCurtainPanelHealthService
    {
        public const string HandlesKey = "GeneratedCurtainPanelHandles";
        public const string BuildStateKey = "GeneratedCurtainPanelBuildState";
        public const string BuildCompleteValue = "Complete";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? livePanelHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            ISet<string>? liveHandleIndex = null;
            if (livePanelHandles != null)
            {
                var reportedCount = livePanelHandles.Count;
                if (reportedCount < 0)
                    throw new InvalidOperationException("Curtain panel live handle input reported a negative Count.");

                var index = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var observedCount = 0;
                foreach (var handle in livePanelHandles)
                {
                    observedCount++;
                    var normalized = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
                    if (normalized.Length > 0) index.Add(normalized);
                }
                if (observedCount != reportedCount)
                    throw new InvalidOperationException("Curtain panel live handle Count does not match traversal count.");
                liveHandleIndex = index;
            }

            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Curtain-panel diagnostics cannot inspect a project containing a null semantic element.");
                var hasHandles = element.Properties.TryGetValue(HandlesKey, out var raw) && !string.IsNullOrWhiteSpace(raw);
                var hasBuildState = element.Properties.TryGetValue(BuildStateKey, out var buildState);
                if (!hasHandles && !hasBuildState) continue;
                var normalizedBuildState = (buildState ?? string.Empty).Trim();
                if (!hasBuildState || !string.Equals(normalizedBuildState, BuildCompleteValue, StringComparison.OrdinalIgnoreCase))
                    Add(issues, "CURTAIN_PANEL_BUILD_STATE_INVALID", HealthSeverity.Warning, BuildStateKey + " must be Complete, including for a valid zero-piece panel build.", element);
                else if (!string.Equals(buildState, BuildCompleteValue, StringComparison.Ordinal))
                    Add(issues, "CURTAIN_PANEL_BUILD_STATE_NON_CANONICAL", HealthSeverity.Error, BuildStateKey + " must use exact writer-owned spelling: " + BuildCompleteValue + ".", element);
                var handles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (hasHandles)
                {
                    foreach (var token in (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.None))
                    {
                        var handleText = token ?? string.Empty;
                        var handle = handleText.Trim();
                        if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                        {
                            Add(issues, "INVALID_CURTAIN_PANEL_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " contains an invalid hexadecimal handle.", element);
                            continue;
                        }

                        var identity = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
                        var isCanonicalOwnerToken = string.Equals(handleText, identity, StringComparison.Ordinal);
                        if (!isCanonicalOwnerToken)
                            Add(issues, "CURTAIN_PANEL_GENERATED_HANDLE_NON_CANONICAL", HealthSeverity.Error, HandlesKey + " must use exact canonical generated-handle spelling: " + identity + ".", element);
                        if (!handles.Add(identity))
                        {
                            Add(issues, "DUPLICATE_CURTAIN_PANEL_GENERATED_HANDLE", HealthSeverity.Error, "A generated curtain panel handle is repeated in the same owner: " + handle + ".", element);
                            continue;
                        }
                        if (isCanonicalOwnerToken)
                        {
                            try
                            {
                                if (!GeneratedHandleOwnershipPolicy.TryFindOwner(project, identity, out var owner, out var ownerKey) ||
                                    !ReferenceEquals(owner, element) ||
                                    !string.Equals(ownerKey, HandlesKey, StringComparison.OrdinalIgnoreCase))
                                    Add(issues, "CURTAIN_PANEL_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated curtain panel ownership is not exclusive: " + handle + ".", element);
                            }
                            catch (InvalidOperationException)
                            {
                                Add(issues, "CURTAIN_PANEL_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated curtain panel ownership is ambiguous: " + handle + ".", element);
                            }
                        }
                        if (element.SourceHandles.Any(x => string.Equals(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(x), identity, StringComparison.OrdinalIgnoreCase)))
                            Add(issues, "CURTAIN_PANEL_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "A generated curtain panel handle cannot also be a source handle.", element);
                        if (liveHandleIndex != null && !liveHandleIndex.Contains(identity))
                            Add(issues, "CURTAIN_PANEL_GENERATED_SOLID_MISSING", HealthSeverity.Error, "A generated curtain panel solid is missing: " + handle + ".", element);
                    }
                }

                var count = Integer(element, "GeneratedCurtainPanelCount", true, issues, "CURTAIN_PANEL_COUNT_INVALID");
                var baseCount = Integer(element, "GeneratedCurtainPanelBaseCount", false, issues, "CURTAIN_PANEL_BASE_COUNT_INVALID");
                var columns = Integer(element, "GeneratedCurtainPanelColumns", false, issues, "CURTAIN_PANEL_COLUMNS_INVALID");
                var rows = Integer(element, "GeneratedCurtainPanelRows", false, issues, "CURTAIN_PANEL_ROWS_INVALID");
                var openingCount = Integer(element, "GeneratedCurtainPanelOpeningCount", true, issues, "CURTAIN_PANEL_OPENING_COUNT_INVALID") ?? 0;
                if (count.HasValue && count.Value != handles.Count)
                    Add(issues, "CURTAIN_PANEL_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedCurtainPanelCount does not match the unique valid handle count.", element);
                if (columns.HasValue && rows.HasValue)
                {
                    try
                    {
                        if (!baseCount.HasValue || baseCount.Value != checked(columns.Value * rows.Value))
                            Add(issues, "CURTAIN_PANEL_GRID_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedCurtainPanelBaseCount does not match Columns*Rows.", element);
                    }
                    catch (OverflowException)
                    {
                        Add(issues, "CURTAIN_PANEL_GRID_COUNT_MISMATCH", HealthSeverity.Warning, "Curtain panel grid count overflowed.", element);
                    }
                }

                Positive(element, "GeneratedCurtainPanelDepthM", issues, "CURTAIN_PANEL_DEPTH_INVALID");
                Positive(element, "GeneratedCurtainPanelSourceLengthM", issues, "CURTAIN_PANEL_SOURCE_LENGTH_INVALID");
                Positive(element, "GeneratedCurtainPanelHeightM", issues, "CURTAIN_PANEL_HEIGHT_INVALID");
                NonNegativeRoundTrip(element, "GeneratedCurtainPanelAreaM2", issues, "CURTAIN_PANEL_AREA_INVALID", "CURTAIN_PANEL_AREA_NON_CANONICAL");
                Fingerprint(element, issues);
                Mode(element, openingCount, issues);

                if (element.Category != ElementCategory.GlassWall)
                    Add(issues, "CURTAIN_PANEL_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated curtain panels are valid only on a GlassWall element.", element);
                if (element.IsGeneratedCurtainPanelStale())
                    Add(issues, "CURTAIN_PANEL_GENERATED_STALE", HealthSeverity.Warning, "Generated curtain panels no longer match the current semantic/opening state; rebuild them before release.", element);
            }
            return issues.AsReadOnly();
        }

        private static void Mode(ProjectElement element, int openingCount, List<ModelHealthIssue> issues)
        {
            var rawMode = element.Properties.TryGetValue("GeneratedCurtainPanelMode", out var raw) ? raw ?? string.Empty : string.Empty;
            var mode = rawMode.Trim();
            var canonicalMode = string.Empty;
            if (string.Equals(mode, "LinePanelSolids", StringComparison.OrdinalIgnoreCase)) canonicalMode = "LinePanelSolids";
            else if (string.Equals(mode, "LinePanelSolids.OpeningAware", StringComparison.OrdinalIgnoreCase)) canonicalMode = "LinePanelSolids.OpeningAware";
            else if (string.Equals(mode, "PathPanelSolids", StringComparison.OrdinalIgnoreCase)) canonicalMode = "PathPanelSolids";
            else if (string.Equals(mode, "PathPanelSolids.OpeningAware", StringComparison.OrdinalIgnoreCase)) canonicalMode = "PathPanelSolids.OpeningAware";

            var line = canonicalMode.StartsWith("LinePanelSolids", StringComparison.Ordinal);
            var path = canonicalMode.StartsWith("PathPanelSolids", StringComparison.Ordinal);
            var openingAware = canonicalMode.EndsWith(".OpeningAware", StringComparison.Ordinal);
            if (canonicalMode.Length == 0)
                Add(issues, "CURTAIN_PANEL_MODE_INVALID", HealthSeverity.Warning, "GeneratedCurtainPanelMode is missing or invalid.", element);
            else
            {
                if (!string.Equals(rawMode, canonicalMode, StringComparison.Ordinal))
                    Add(issues, "CURTAIN_PANEL_MODE_NON_CANONICAL", HealthSeverity.Error, "GeneratedCurtainPanelMode must use exact writer-owned spelling: " + canonicalMode + ".", element);
                if ((openingCount > 0) != openingAware)
                    Add(issues, "CURTAIN_PANEL_OPENING_MODE_MISMATCH", HealthSeverity.Warning, "Curtain panel opening-aware mode does not match GeneratedCurtainPanelOpeningCount.", element);
            }
            if (!path) return;
            Integer(element, "GeneratedCurtainPanelPathSegmentCount", false, issues, "CURTAIN_PANEL_PATH_SEGMENTS_INVALID");
            Integer(element, "GeneratedCurtainPanelMappedCount", true, issues, "CURTAIN_PANEL_MAPPED_COUNT_INVALID");
            AtLeastRoundTrip(element, "GeneratedCurtainPanelPathSagittaM", 1e-6d, issues, "CURTAIN_PANEL_PATH_SAGITTA_INVALID", "CURTAIN_PANEL_PATH_SAGITTA_NON_CANONICAL");
            if (!element.Properties.TryGetValue("GeneratedCurtainPanelSourceKind", out var kind) || !string.Equals((kind ?? string.Empty).Trim(), "OpenPolyline", StringComparison.OrdinalIgnoreCase))
                Add(issues, "CURTAIN_PANEL_PATH_SOURCE_KIND_INVALID", HealthSeverity.Warning, "Path curtain panels require GeneratedCurtainPanelSourceKind=OpenPolyline.", element);
            else if (!string.Equals(kind, "OpenPolyline", StringComparison.Ordinal))
                Add(issues, "CURTAIN_PANEL_PATH_SOURCE_KIND_NON_CANONICAL", HealthSeverity.Error, "GeneratedCurtainPanelSourceKind must use exact writer-owned spelling: OpenPolyline.", element);
        }

        private static void Fingerprint(ProjectElement element, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue("GeneratedCurtainPanelConfigFingerprint", out var raw) || raw == null)
            {
                Add(issues, "CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID", HealthSeverity.Warning, "GeneratedCurtainPanelConfigFingerprint must be a SHA-256 hexadecimal digest.", element);
                return;
            }
            var normalized = raw.Trim();
            if (normalized.Length != 64 || normalized.Any(x => !Uri.IsHexDigit(x)))
            {
                Add(issues, "CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID", HealthSeverity.Warning, "GeneratedCurtainPanelConfigFingerprint must be a SHA-256 hexadecimal digest.", element);
                return;
            }
            if (!string.Equals(raw, normalized.ToLowerInvariant(), StringComparison.Ordinal))
                Add(issues, "CURTAIN_PANEL_CONFIG_FINGERPRINT_NON_CANONICAL", HealthSeverity.Error, "GeneratedCurtainPanelConfigFingerprint must use exact lowercase SHA-256 writer-owned spelling.", element);
        }

        private static int? Integer(ProjectElement element, string key, bool allowZero, List<ModelHealthIssue> issues, string code)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || (allowZero ? value < 0 : value < 1))
            {
                Add(issues, code, HealthSeverity.Warning, key + " is missing or invalid.", element);
                return null;
            }
            var canonical = value.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                Add(issues, "CURTAIN_PANEL_INTEGER_METADATA_NON_CANONICAL", HealthSeverity.Error, key + " must use exact invariant integer spelling: " + canonical + ".", element);
            return value;
        }

        private static void Positive(ProjectElement element, string key, List<ModelHealthIssue> issues, string code)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                Add(issues, code, HealthSeverity.Warning, key + " is missing or invalid.", element);
                return;
            }
            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                Add(issues, "CURTAIN_PANEL_FLOAT_METADATA_NON_CANONICAL", HealthSeverity.Error, key + " must use exact invariant round-trip spelling: " + canonical + ".", element);
        }

        private static void NonNegativeRoundTrip(ProjectElement element, string key, List<ModelHealthIssue> issues, string invalidCode, string nonCanonicalCode)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                Add(issues, invalidCode, HealthSeverity.Warning, key + " is missing or invalid.", element);
                return;
            }
            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                Add(issues, nonCanonicalCode, HealthSeverity.Error, key + " must use exact invariant round-trip spelling: " + canonical + ".", element);
        }

        private static void AtLeastRoundTrip(ProjectElement element, string key, double minimum, List<ModelHealthIssue> issues, string invalidCode, string nonCanonicalCode)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
            {
                Add(issues, invalidCode, HealthSeverity.Warning, key + " is missing or invalid.", element);
                return;
            }
            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                Add(issues, nonCanonicalCode, HealthSeverity.Error, key + " must use exact invariant round-trip spelling: " + canonical + ".", element);
        }

        private static void Add(ICollection<ModelHealthIssue> issues, string code, HealthSeverity severity, string message, ProjectElement element) =>
            issues.Add(new ModelHealthIssue(code, severity, message, element.Id));
    }
}
