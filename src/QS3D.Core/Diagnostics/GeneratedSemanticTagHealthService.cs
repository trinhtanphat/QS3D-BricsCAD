using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedSemanticTagHealthService
    {
        public const string HandlesKey = "GeneratedSemanticTagHandles";
        public const string TemplateKey = "GeneratedSemanticTagTemplate";
        public const string TextKey = "GeneratedSemanticTagText";
        public const string OwnerProjectKey = "GeneratedSemanticTagOwnerProjectId";
        public const string OwnerElementKey = "GeneratedSemanticTagOwnerElementId";
        public const string OwnershipVersionKey = "GeneratedSemanticTagOwnershipVersion";
        public const string TextHeightKey = "GeneratedSemanticTagTextHeightM";
        public const string PositionScopeKey = "GeneratedSemanticTagPositionScope";
        public const string PositionXKey = "GeneratedSemanticTagPositionX";
        public const string PositionYKey = "GeneratedSemanticTagPositionY";
        public const string PositionZKey = "GeneratedSemanticTagPositionZ";
        public const string RotationKey = "GeneratedSemanticTagRotationRad";
        public const string OwnershipVersion = "1";
        public const string DrawingLocalWcs = "DrawingLocalWcs";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();

            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Semantic tag health cannot inspect a null project element.");
                if (!element.Properties.TryGetValue(HandlesKey, out var rawHandles) || string.IsNullOrWhiteSpace(rawHandles)) continue;
                var handles = ParseHandles(element, rawHandles, issues);
                if (handles.Count == 0)
                    issues.Add(new ModelHealthIssue("SEMANTIC_TAG_HANDLE_INVALID", HealthSeverity.Error, "Semantic tag không còn generated handle hợp lệ.", element.Id));

                if (element.SourceHandles.Any(source => handles.Contains((source ?? string.Empty).Trim())))
                    issues.Add(new ModelHealthIssue("SEMANTIC_TAG_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated semantic tag handle không được nằm trong SourceHandles.", element.Id));

                RequireOwner(element, OwnerProjectKey, project.ProjectId, "SEMANTIC_TAG_PROJECT_MISMATCH", issues);
                RequireOwner(element, OwnerElementKey, element.Id, "SEMANTIC_TAG_ELEMENT_MISMATCH", issues);
                RequireOwnershipVersion(element, issues);

                var template = Property(element, TemplateKey);
                if (template.Length == 0)
                {
                    issues.Add(new ModelHealthIssue("SEMANTIC_TAG_TEMPLATE_MISSING", HealthSeverity.Error, "Generated semantic tag thiếu template đã dùng để render.", element.Id));
                }
                else
                {
                    try
                    {
                        var current = SemanticTagRenderer.Render(project, element, template);
                        var built = Property(element, TextKey);
                        if (!string.Equals(current, built, StringComparison.Ordinal))
                            issues.Add(new ModelHealthIssue("SEMANTIC_TAG_TEXT_STALE", HealthSeverity.Warning, "Semantic tag text không còn khớp semantic state hiện tại; chạy QS3DTAGREFRESH.", element.Id));
                    }
                    catch (Exception ex) when (IsDiagnosticDataFailure(ex))
                    {
                        issues.Add(new ModelHealthIssue(
                            "SEMANTIC_TAG_RENDER_INVALID",
                            HealthSeverity.Error,
                            "Không thể render lại semantic tag vì semantic/project data không hợp lệ.",
                            element.Id));
                    }
                }

                ValidatePositiveCanonical(element, TextHeightKey, "SEMANTIC_TAG_TEXT_HEIGHT_INVALID", "SEMANTIC_TAG_TEXT_HEIGHT_NON_CANONICAL", issues);
                ValidatePositionScope(element, issues);
                ValidateFiniteCanonical(element, PositionXKey, "SEMANTIC_TAG_POSITION_INVALID", "SEMANTIC_TAG_POSITION_NON_CANONICAL", issues);
                ValidateFiniteCanonical(element, PositionYKey, "SEMANTIC_TAG_POSITION_INVALID", "SEMANTIC_TAG_POSITION_NON_CANONICAL", issues);
                ValidateFiniteCanonical(element, PositionZKey, "SEMANTIC_TAG_POSITION_INVALID", "SEMANTIC_TAG_POSITION_NON_CANONICAL", issues);
                ValidateRotation(element, issues);
            }

            return issues.AsReadOnly();
        }

        private static HashSet<string> ParseHandles(ProjectElement element, string raw, ICollection<ModelHealthIssue> issues)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.None))
            {
                var handleText = token ?? string.Empty;
                var handle = handleText.Trim();
                if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                {
                    issues.Add(new ModelHealthIssue("SEMANTIC_TAG_HANDLE_INVALID", HealthSeverity.Error, "Semantic tag chứa generated handle không hợp lệ: " + handle, element.Id));
                    continue;
                }
                if (!string.Equals(handleText, handle, StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue("SEMANTIC_TAG_HANDLE_NON_CANONICAL", HealthSeverity.Error, HandlesKey + " không được có khoảng trắng đầu/cuối quanh từng generated handle.", element.Id));
                if (!result.Add(handle))
                    issues.Add(new ModelHealthIssue("SEMANTIC_TAG_HANDLE_DUPLICATE", HealthSeverity.Error, "Semantic tag generated handle bị lặp: " + handle, element.Id));
            }
            return result;
        }

        private static void RequireOwner(ProjectElement element, string key, string expected, string code, ICollection<ModelHealthIssue> issues)
        {
            if (!string.Equals(Property(element, key), expected, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Error, key + " không khớp semantic owner hiện tại.", element.Id));
        }

        private static void RequireOwnershipVersion(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var raw = element.Properties.TryGetValue(OwnershipVersionKey, out var stored) ? stored ?? string.Empty : string.Empty;
            var normalized = raw.Trim();
            if (!string.Equals(normalized, OwnershipVersion, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_OWNERSHIP_VERSION_INVALID", HealthSeverity.Error, OwnershipVersionKey + " không khớp semantic owner hiện tại.", element.Id));
                return;
            }
            if (!string.Equals(raw, OwnershipVersion, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_OWNERSHIP_VERSION_NON_CANONICAL", HealthSeverity.Error, OwnershipVersionKey + " phải dùng đúng writer-owned token: " + OwnershipVersion + ".", element.Id));
        }

        private static void ValidatePositiveCanonical(ProjectElement element, string key, string invalidCode, string canonicalCode, ICollection<ModelHealthIssue> issues)
        {
            if (!TryRawFinite(element, key, out var raw, out var value) || value <= 0d)
            {
                issues.Add(new ModelHealthIssue(invalidCode, HealthSeverity.Error, key + " phải là số hữu hạn > 0.", element.Id));
                return;
            }
            ValidateNumericCanonicality(element, key, raw, value, canonicalCode, issues);
        }

        private static void ValidateFiniteCanonical(ProjectElement element, string key, string invalidCode, string canonicalCode, ICollection<ModelHealthIssue> issues)
        {
            if (!TryRawFinite(element, key, out var raw, out var value))
            {
                issues.Add(new ModelHealthIssue(invalidCode, HealthSeverity.Error, key + " phải là số hữu hạn theo drawing-local WCS.", element.Id));
                return;
            }
            ValidateNumericCanonicality(element, key, raw, value, canonicalCode, issues);
        }

        private static void ValidateNumericCanonicality(ProjectElement element, string key, string raw, double value, string code, ICollection<ModelHealthIssue> issues)
        {
            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Error, key + " phải dùng đúng round-trip invariant numeric spelling: " + canonical + ".", element.Id));
        }

        private static void ValidatePositionScope(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var raw = element.Properties.TryGetValue(PositionScopeKey, out var stored) ? stored ?? string.Empty : string.Empty;
            var normalized = raw.Trim();
            if (!string.Equals(normalized, DrawingLocalWcs, StringComparison.Ordinal))
            {
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_POSITION_SCOPE_INVALID", HealthSeverity.Error, "Semantic tag position scope phải là DrawingLocalWcs.", element.Id));
                return;
            }
            if (!string.Equals(raw, DrawingLocalWcs, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_POSITION_SCOPE_NON_CANONICAL", HealthSeverity.Error, PositionScopeKey + " phải dùng đúng writer-owned token DrawingLocalWcs.", element.Id));
        }

        private static void ValidateRotation(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(RotationKey, out var raw) ||
                string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
            {
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_ROTATION_INVALID", HealthSeverity.Error, RotationKey + " phải là số hữu hạn.", element.Id));
                return;
            }

            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("SEMANTIC_TAG_ROTATION_NON_CANONICAL", HealthSeverity.Error, RotationKey + " phải dùng đúng round-trip invariant numeric spelling: " + canonical + ".", element.Id));
        }

        private static bool TryRawFinite(ProjectElement element, string key, out string raw, out double value)
        {
            raw = string.Empty;
            value = 0d;
            return element.Properties.TryGetValue(key, out raw) &&
                   raw != null &&
                   !string.IsNullOrWhiteSpace(raw) &&
                   double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }

        private static bool IsDiagnosticDataFailure(Exception exception)
        {
            return exception is InvalidOperationException ||
                   exception is ArgumentException ||
                   exception is FormatException ||
                   exception is OverflowException ||
                   exception is KeyNotFoundException ||
                   exception is NullReferenceException;
        }

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;
    }
}
