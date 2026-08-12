using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class SemanticScheduleHealthService
    {
        private const int MaxIssues = 768;
        private const int MaxExamples = 5;

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue(SemanticScheduleCatalog.MetadataKey, out var payload) || string.IsNullOrWhiteSpace(payload))
                return Array.Empty<ModelHealthIssue>();

            IReadOnlyList<SemanticScheduleDefinition> definitions;
            try
            {
                definitions = SemanticScheduleCatalog.Load(project);
            }
            catch (Exception ex) when (IsCatalogDataFailure(ex))
            {
                return new[]
                {
                    new ModelHealthIssue(
                        "SEMANTIC_SCHEDULE_CATALOG_INVALID",
                        HealthSeverity.Error,
                        "Catalog SemanticSchedule không hợp lệ và không thể chẩn đoán chi tiết.")
                };
            }

            var floorCounts = BuildIdentityCounts(project.Floors, x => x.Id);
            var zoneCounts = BuildIdentityCounts(project.Zones, x => x.Id);
            var elementCounts = BuildIdentityCounts(project.Elements, x => x.Id);
            var issues = new List<ModelHealthIssue>();

            foreach (var definition in definitions)
            {
                if (issues.Count >= MaxIssues) break;
                InspectReference(
                    definition.Id,
                    definition.FloorId,
                    floorCounts,
                    "Floor/Level",
                    "SEMANTIC_SCHEDULE_MISSING_FLOOR",
                    "SEMANTIC_SCHEDULE_AMBIGUOUS_FLOOR",
                    issues);
                InspectReference(
                    definition.Id,
                    definition.ZoneId,
                    zoneCounts,
                    "Zone",
                    "SEMANTIC_SCHEDULE_MISSING_ZONE",
                    "SEMANTIC_SCHEDULE_AMBIGUOUS_ZONE",
                    issues);
                InspectElementReferences(definition, elementCounts, issues);
                InspectTemplates(definition, issues);
            }

            if (issues.Count >= MaxIssues)
            {
                if (issues.Count == MaxIssues) issues.RemoveAt(issues.Count - 1);
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_SCHEDULE_HEALTH_TRUNCATED",
                    HealthSeverity.Warning,
                    "Chẩn đoán SemanticSchedule đã đạt giới hạn " + MaxIssues + " issue; cần sửa các lỗi hiện có rồi kiểm tra lại."));
            }

            return issues.AsReadOnly();
        }

        private static void InspectElementReferences(
            SemanticScheduleDefinition definition,
            IReadOnlyDictionary<string, int> elementCounts,
            ICollection<ModelHealthIssue> issues)
        {
            var referenced = definition.IncludeElementIds
                .Concat(definition.ExcludeElementIds)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var missing = new List<string>();
            var ambiguous = new List<string>();
            foreach (var id in referenced)
            {
                if (!elementCounts.TryGetValue(id, out var count)) missing.Add(id);
                else if (count > 1) ambiguous.Add(id);
            }

            if (missing.Count > 0)
                AddBounded(issues, new ModelHealthIssue(
                    "SEMANTIC_SCHEDULE_MISSING_ELEMENT",
                    HealthSeverity.Error,
                    "SemanticSchedule " + definition.Id + " tham chiếu Element không còn tồn tại: " + Summarize(missing) + "."));
            if (ambiguous.Count > 0)
                AddBounded(issues, new ModelHealthIssue(
                    "SEMANTIC_SCHEDULE_AMBIGUOUS_ELEMENT",
                    HealthSeverity.Error,
                    "SemanticSchedule " + definition.Id + " tham chiếu Element ID bị trùng: " + Summarize(ambiguous) + "."));
        }

        private static void InspectTemplates(SemanticScheduleDefinition definition, ICollection<ModelHealthIssue> issues)
        {
            var invalid = new List<string>();
            foreach (var column in definition.Columns)
            {
                try
                {
                    SemanticTagRenderer.ValidateTemplate(column.Template);
                }
                catch (Exception ex) when (IsTemplateFailure(ex))
                {
                    invalid.Add(column.Header);
                }
            }

            if (invalid.Count > 0)
                AddBounded(issues, new ModelHealthIssue(
                    "SEMANTIC_SCHEDULE_TEMPLATE_INVALID",
                    HealthSeverity.Error,
                    "SemanticSchedule " + definition.Id + " có template cột không hợp lệ: " + Summarize(invalid) + "."));
        }

        private static void InspectReference(
            string scheduleId,
            string rawId,
            IReadOnlyDictionary<string, int> counts,
            string label,
            string missingCode,
            string ambiguousCode,
            ICollection<ModelHealthIssue> issues)
        {
            var id = (rawId ?? string.Empty).Trim();
            if (id.Length == 0) return;
            if (!counts.TryGetValue(id, out var count))
            {
                AddBounded(issues, new ModelHealthIssue(
                    missingCode,
                    HealthSeverity.Error,
                    "SemanticSchedule " + scheduleId + " tham chiếu " + label + " không tồn tại: " + id + "."));
                return;
            }
            if (count > 1)
                AddBounded(issues, new ModelHealthIssue(
                    ambiguousCode,
                    HealthSeverity.Error,
                    "SemanticSchedule " + scheduleId + " tham chiếu " + label + " ID bị trùng: " + id + "."));
        }

        private static Dictionary<string, int> BuildIdentityCounts<T>(IEnumerable<T> values, Func<T, string> selector) where T : class
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                if (value == null)
                    throw new InvalidOperationException("Semantic Schedule health cannot inspect a null semantic identity entry.");
                var id = (selector(value) ?? string.Empty).Trim();
                if (id.Length == 0) continue;
                result[id] = result.TryGetValue(id, out var count) ? count + 1 : 1;
            }
            return result;
        }

        private static void AddBounded(ICollection<ModelHealthIssue> issues, ModelHealthIssue issue)
        {
            if (issues.Count < MaxIssues) issues.Add(issue);
        }

        private static string Summarize(IEnumerable<string> values)
        {
            var ordered = values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToArray();
            var shown = string.Join(", ", ordered.Take(MaxExamples));
            return ordered.Length > MaxExamples
                ? shown + " (+" + (ordered.Length - MaxExamples) + " mục khác)"
                : shown;
        }

        private static bool IsCatalogDataFailure(Exception ex)
        {
            return ex is InvalidDataException ||
                   ex is InvalidOperationException ||
                   ex is ArgumentException ||
                   ex is FormatException ||
                   ex is OverflowException;
        }

        private static bool IsTemplateFailure(Exception ex)
        {
            return ex is InvalidOperationException || ex is ArgumentException || ex is FormatException;
        }
    }
}
