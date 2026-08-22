using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedGridAnnotationHealthService
    {
        private const string HandlesKey = "GeneratedGridAnnotationHandles";
        private const string BuiltLabelKey = "GeneratedGridAnnotationLabel";
        private const string OwnerProjectKey = "GeneratedGridAnnotationOwnerProjectId";
        private const string OwnerElementKey = "GeneratedGridAnnotationOwnerElementId";
        private const string OwnershipVersionKey = "GeneratedGridAnnotationOwnershipVersion";
        private const string BubbleRadiusKey = "GridBubbleRadiusM";
        private const string TextHeightKey = "GridTextHeightM";
        private const string OwnershipVersion = "1";
        private const int ExpectedHandleCount = 6;

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();

            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Grid annotation health cannot inspect a null project element.");
                if (element.Category != ElementCategory.Grid) continue;
                if (!element.Properties.TryGetValue(HandlesKey, out var rawHandles)) continue;

                if (string.IsNullOrWhiteSpace(rawHandles))
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_ANNOTATION_HANDLES_EMPTY",
                        HealthSeverity.Warning,
                        "Grid có metadata annotation nhưng GeneratedGridAnnotationHandles đang rỗng.",
                        element.Id));
                    continue;
                }

                var handlesText = rawHandles ?? string.Empty;
                var tokens = handlesText
                    .Split(new[] { ';' }, StringSplitOptions.None)
                    .Select(x => (x ?? string.Empty).Trim())
                    .ToList();
                if (tokens.All(x => x.Length > 0) &&
                    !string.Equals(handlesText, string.Join(";", tokens), StringComparison.Ordinal))
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_ANNOTATION_HANDLE_LIST_NON_CANONICAL",
                        HealthSeverity.Error,
                        "GeneratedGridAnnotationHandles không được có khoảng trắng quanh các Handle token.",
                        element.Id));
                }

                var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var handle in tokens)
                {
                    if (handle.Length == 0)
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_ANNOTATION_HANDLE_INVALID",
                            HealthSeverity.Error,
                            "Generated Grid annotation Handle không được rỗng.",
                            element.Id));
                        continue;
                    }

                    var isValidHex = long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
                    var identity = isValidHex
                        ? GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle)
                        : handle;
                    if (!distinct.Add(identity))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_ANNOTATION_HANDLE_DUPLICATE",
                            HealthSeverity.Error,
                            "GeneratedGridAnnotationHandles chứa Handle lặp: " + identity + ".",
                            element.Id));
                        continue;
                    }
                    if (!isValidHex)
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_ANNOTATION_HANDLE_INVALID",
                            HealthSeverity.Error,
                            "Generated Grid annotation Handle không phải hex hợp lệ: " + handle + ".",
                            element.Id));
                    }
                    if (element.SourceHandles.Any(x => string.Equals(
                        isValidHex ? GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(x) : (x ?? string.Empty).Trim(),
                        identity,
                        StringComparison.OrdinalIgnoreCase)))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_ANNOTATION_HANDLE_IN_SOURCE",
                            HealthSeverity.Error,
                            "Generated Grid annotation Handle không được nằm trong SourceHandles: " + identity + ".",
                            element.Id));
                    }
                }

                if (distinct.Count != ExpectedHandleCount)
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_ANNOTATION_HANDLE_COUNT",
                        HealthSeverity.Warning,
                        "Native Grid annotation hiện kỳ vọng " + ExpectedHandleCount + " generated entities (2 extension + 2 bubble + 2 text), metadata đang có " + distinct.Count + ".",
                        element.Id));
                }

                var currentLabel = Property(element, GridNamingService.GridLabelKey);
                var rawBuiltLabel = RawProperty(element, BuiltLabelKey);
                var builtLabel = rawBuiltLabel.Trim();
                if (!string.Equals(rawBuiltLabel, builtLabel, StringComparison.Ordinal))
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_ANNOTATION_BUILT_LABEL_NON_CANONICAL",
                        HealthSeverity.Error,
                        "GeneratedGridAnnotationLabel không được có khoảng trắng đầu/cuối.",
                        element.Id));
                }
                if (currentLabel.Length == 0)
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_ANNOTATION_WITHOUT_LABEL",
                        HealthSeverity.Error,
                        "Grid có generated annotation nhưng semantic GridLabel đang rỗng.",
                        element.Id));
                }
                else if (!string.Equals(currentLabel, builtLabel, StringComparison.Ordinal))
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_ANNOTATION_LABEL_STALE",
                        HealthSeverity.Error,
                        "Native Grid annotation không còn khớp GridLabel hiện tại; rebuild annotation. Built=" + builtLabel + ", current=" + currentLabel + ".",
                        element.Id));
                }

                ValidateOwner(project, element, issues);
                ValidateSizing(element, issues);
            }

            return issues.AsReadOnly();
        }

        private static void ValidateOwner(ProjectState project, ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var rawVersion = RawProperty(element, OwnershipVersionKey);
            var rawOwnerProject = RawProperty(element, OwnerProjectKey);
            var rawOwnerElement = RawProperty(element, OwnerElementKey);
            var version = rawVersion.Trim();
            var ownerProject = rawOwnerProject.Trim();
            var ownerElement = rawOwnerElement.Trim();

            if (!string.Equals(rawVersion, OwnershipVersion, StringComparison.Ordinal) &&
                string.Equals(version, OwnershipVersion, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_OWNERSHIP_VERSION_NON_CANONICAL", HealthSeverity.Error, "Generated Grid annotation ownership version phải dùng đúng canonical token 1.", element.Id));
            if (!string.Equals(rawOwnerProject, project.ProjectId, StringComparison.Ordinal) &&
                string.Equals(ownerProject, project.ProjectId, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_PROJECT_OWNER_NON_CANONICAL", HealthSeverity.Error, "Generated Grid annotation owner project id phải khớp chính xác project id canonical.", element.Id));
            if (!string.Equals(rawOwnerElement, element.Id, StringComparison.Ordinal) &&
                string.Equals(ownerElement, element.Id, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_ELEMENT_OWNER_NON_CANONICAL", HealthSeverity.Error, "Generated Grid annotation owner element id phải khớp chính xác semantic Grid id canonical.", element.Id));

            if (!string.Equals(version, OwnershipVersion, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_OWNERSHIP_VERSION", HealthSeverity.Error, "Generated Grid annotation ownership version không được hỗ trợ: " + version + ".", element.Id));
            if (!string.Equals(ownerProject, project.ProjectId, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_PROJECT_MISMATCH", HealthSeverity.Error, "Generated Grid annotation metadata không thuộc project hiện tại.", element.Id));
            if (!string.Equals(ownerElement, element.Id, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_ELEMENT_MISMATCH", HealthSeverity.Error, "Generated Grid annotation metadata không thuộc semantic Grid hiện tại.", element.Id));
        }

        private static void ValidateSizing(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var rawRadius = RawProperty(element, BubbleRadiusKey);
            if (!TryPositive(rawRadius, out var radius))
            {
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_BUBBLE_RADIUS_INVALID", HealthSeverity.Error, "GridBubbleRadiusM của generated annotation phải là số hữu hạn > 0.", element.Id));
                return;
            }
            var canonicalRadius = radius.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(rawRadius, canonicalRadius, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_BUBBLE_RADIUS_NON_CANONICAL", HealthSeverity.Error, "GridBubbleRadiusM phải dùng đúng round-trip invariant numeric spelling: " + canonicalRadius + ".", element.Id));

            var rawTextHeight = RawProperty(element, TextHeightKey);
            if (!TryPositive(rawTextHeight, out var textHeight))
            {
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_TEXT_HEIGHT_INVALID", HealthSeverity.Error, "GridTextHeightM của generated annotation phải là số hữu hạn > 0.", element.Id));
                return;
            }
            var canonicalTextHeight = textHeight.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(rawTextHeight, canonicalTextHeight, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_TEXT_HEIGHT_NON_CANONICAL", HealthSeverity.Error, "GridTextHeightM phải dùng đúng round-trip invariant numeric spelling: " + canonicalTextHeight + ".", element.Id));

            if (textHeight > radius * 1.8d)
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_TEXT_TOO_LARGE", HealthSeverity.Error, "GridTextHeightM vượt giới hạn 1.8 × GridBubbleRadiusM.", element.Id));
        }

        private static bool TryPositive(string raw, out double value)
        {
            return double.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d;
        }

        private static string RawProperty(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? raw ?? string.Empty : string.Empty;

        private static string Property(ProjectElement element, string key) => RawProperty(element, key).Trim();
    }
}
