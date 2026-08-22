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

                var tokens = (rawHandles ?? string.Empty)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();
                if (tokens.Count == 0)
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_ANNOTATION_HANDLES_EMPTY",
                        HealthSeverity.Warning,
                        "Grid có metadata annotation nhưng GeneratedGridAnnotationHandles đang rỗng.",
                        element.Id));
                    continue;
                }

                var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var handle in tokens)
                {
                    if (!distinct.Add(handle))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_ANNOTATION_HANDLE_DUPLICATE",
                            HealthSeverity.Error,
                            "GeneratedGridAnnotationHandles chứa Handle lặp: " + handle + ".",
                            element.Id));
                        continue;
                    }
                    if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_ANNOTATION_HANDLE_INVALID",
                            HealthSeverity.Error,
                            "Generated Grid annotation Handle không phải hex hợp lệ: " + handle + ".",
                            element.Id));
                    }
                    if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_ANNOTATION_HANDLE_IN_SOURCE",
                            HealthSeverity.Error,
                            "Generated Grid annotation Handle không được nằm trong SourceHandles: " + handle + ".",
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
                var builtLabel = Property(element, BuiltLabelKey);
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
            var version = Property(element, OwnershipVersionKey);
            var ownerProject = Property(element, OwnerProjectKey);
            var ownerElement = Property(element, OwnerElementKey);

            if (!string.Equals(version, OwnershipVersion, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_OWNERSHIP_VERSION", HealthSeverity.Error, "Generated Grid annotation ownership version không được hỗ trợ: " + version + ".", element.Id));
            if (!string.Equals(ownerProject, project.ProjectId, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_PROJECT_MISMATCH", HealthSeverity.Error, "Generated Grid annotation metadata không thuộc project hiện tại.", element.Id));
            if (!string.Equals(ownerElement, element.Id, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_ELEMENT_MISMATCH", HealthSeverity.Error, "Generated Grid annotation metadata không thuộc semantic Grid hiện tại.", element.Id));
        }

        private static void ValidateSizing(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!TryPositive(Property(element, BubbleRadiusKey), out var radius))
            {
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_BUBBLE_RADIUS_INVALID", HealthSeverity.Error, "GridBubbleRadiusM của generated annotation phải là số hữu hạn > 0.", element.Id));
                return;
            }
            if (!TryPositive(Property(element, TextHeightKey), out var textHeight))
            {
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_TEXT_HEIGHT_INVALID", HealthSeverity.Error, "GridTextHeightM của generated annotation phải là số hữu hạn > 0.", element.Id));
                return;
            }
            if (textHeight > radius * 1.8d)
                issues.Add(new ModelHealthIssue("GRID_ANNOTATION_TEXT_TOO_LARGE", HealthSeverity.Error, "GridTextHeightM vượt giới hạn 1.8 × GridBubbleRadiusM.", element.Id));
        }

        private static bool TryPositive(string raw, out double value)
        {
            return double.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d;
        }

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;
    }
}
