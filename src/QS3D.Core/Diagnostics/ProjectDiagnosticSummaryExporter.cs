using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Diagnostics
{
    public static class ProjectDiagnosticSummaryExporter
    {
        public const string FormatName = "QS3D.DiagnosticSummary";
        public const int FormatVersion = 1;
        public const int MaxIssueCount = 1000000;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static string BuildSemantic(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return Build(project, new ComprehensiveModelHealthService().Inspect(project));
        }

        public static string Build(ProjectState project, IEnumerable<ModelHealthIssue> issues)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (issues == null) throw new ArgumentNullException(nameof(issues));

            var normalizedIssues = MaterializeIssues(issues);
            if (normalizedIssues.Any(x => x == null))
                throw new InvalidOperationException("Diagnostic summary cannot contain a null health issue.");
            foreach (var issue in normalizedIssues)
            {
                if (!Enum.IsDefined(typeof(HealthSeverity), issue.Severity))
                    throw new InvalidOperationException("Diagnostic summary contains an undefined health severity: " + (int)issue.Severity + ".");
            }

            var health = normalizedIssues
                .GroupBy(x => new { x.Severity, Code = CanonicalCode(x.Code) })
                .OrderByDescending(x => x.Key.Severity)
                .ThenBy(x => x.Key.Code, StringComparer.Ordinal)
                .Select(x => new HealthCount(x.Key.Severity, x.Key.Code, x.Count()))
                .ToList();

            var categories = project.Elements
                .Where(x => x != null)
                .GroupBy(x => x.Category)
                .OrderBy(x => x.Key)
                .Select(x => new CategoryCount(x.Key.ToString(), x.Count()))
                .ToList();

            var nullElements = project.Elements.Count(x => x == null);
            var dirtyElements = project.Elements.Count(x => x != null && x.Dirty != ElementDirtyFlags.None);
            var nullFamilies = project.Families.Count(x => x == null);
            var nullFloors = project.Floors.Count(x => x == null);
            var nullZones = project.Zones.Count(x => x == null);
            var nullRules = project.QuantityRules.Count(x => x == null);

            var sb = new StringBuilder(4096);
            sb.Append("{\n");
            JsonString(sb, 1, "format", FormatName, true);
            JsonNumber(sb, 1, "formatVersion", FormatVersion, true);
            JsonNumber(sb, 1, "schemaVersion", project.SchemaVersion, true);
            sb.Append("  \"counts\": {\n");
            JsonNumber(sb, 2, "zones", project.Zones.Count, true);
            JsonNumber(sb, 2, "floors", project.Floors.Count, true);
            JsonNumber(sb, 2, "families", project.Families.Count, true);
            JsonNumber(sb, 2, "elements", project.Elements.Count, true);
            JsonNumber(sb, 2, "quantityRules", project.QuantityRules.Count, true);
            JsonNumber(sb, 2, "dirtyElements", dirtyElements, true);
            JsonNumber(sb, 2, "nullZones", nullZones, true);
            JsonNumber(sb, 2, "nullFloors", nullFloors, true);
            JsonNumber(sb, 2, "nullFamilies", nullFamilies, true);
            JsonNumber(sb, 2, "nullElements", nullElements, true);
            JsonNumber(sb, 2, "nullQuantityRules", nullRules, false);
            sb.Append("  },\n");

            sb.Append("  \"elementCategories\": [");
            if (categories.Count > 0) sb.Append('\n');
            for (var i = 0; i < categories.Count; i++)
            {
                var row = categories[i];
                sb.Append("    {\"category\":\"").Append(Escape(row.Category)).Append("\",\"count\":")
                    .Append(row.Count.ToString(CultureInfo.InvariantCulture)).Append('}');
                sb.Append(i + 1 < categories.Count ? ",\n" : "\n");
            }
            sb.Append("  ],\n");

            sb.Append("  \"health\": {\n");
            JsonNumber(sb, 2, "errors", health.Where(x => x.Severity == HealthSeverity.Error).Sum(x => x.Count), true);
            JsonNumber(sb, 2, "warnings", health.Where(x => x.Severity == HealthSeverity.Warning).Sum(x => x.Count), true);
            JsonNumber(sb, 2, "info", health.Where(x => x.Severity == HealthSeverity.Info).Sum(x => x.Count), true);
            sb.Append("    \"byCode\": [");
            if (health.Count > 0) sb.Append('\n');
            for (var i = 0; i < health.Count; i++)
            {
                var row = health[i];
                sb.Append("      {\"severity\":\"").Append(Escape(row.Severity.ToString()))
                    .Append("\",\"code\":\"").Append(Escape(row.Code))
                    .Append("\",\"count\":").Append(row.Count.ToString(CultureInfo.InvariantCulture)).Append('}');
                sb.Append(i + 1 < health.Count ? ",\n" : "\n");
            }
            sb.Append("    ]\n");
            sb.Append("  }\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        private static List<ModelHealthIssue> MaterializeIssues(IEnumerable<ModelHealthIssue> issues)
        {
            var expectedCount = ValidateKnownIssueCounts(issues, null);
            var result = new List<ModelHealthIssue>(Math.Min(MaxIssueCount, expectedCount ?? 256));
            using (var enumerator = issues.GetEnumerator())
            {
                while (true)
                {
                    ValidateKnownIssueCounts(issues, expectedCount);
                    var hasNext = enumerator.MoveNext();
                    ValidateKnownIssueCounts(issues, expectedCount);

                    if (!hasNext)
                        break;

                    if (expectedCount.HasValue && result.Count >= expectedCount.Value)
                        throw new InvalidOperationException("Diagnostic summary issue source yielded more items than its admitted known Count.");
                    if (result.Count >= MaxIssueCount)
                        throw new InvalidOperationException("Diagnostic summary supports at most " + MaxIssueCount.ToString(CultureInfo.InvariantCulture) + " health issues.");

                    var current = enumerator.Current;
                    ValidateKnownIssueCounts(issues, expectedCount);
                    result.Add(current);
                }
            }

            ValidateKnownIssueCounts(issues, expectedCount);
            if (expectedCount.HasValue && result.Count != expectedCount.Value)
                throw new InvalidOperationException("Diagnostic summary issue source yielded fewer items than its admitted known Count.");
            return result;
        }

        private static int? ValidateKnownIssueCounts(IEnumerable<ModelHealthIssue> issues, int? admittedCount)
        {
            var knownCounts = new List<int>(3);
            if (issues is ICollection<ModelHealthIssue> genericCollection)
                knownCounts.Add(genericCollection.Count);
            if (issues is IReadOnlyCollection<ModelHealthIssue> readOnlyCollection)
                knownCounts.Add(readOnlyCollection.Count);
            if (issues is System.Collections.ICollection nonGenericCollection)
                knownCounts.Add(nonGenericCollection.Count);

            int? observedCount = null;
            for (var i = 0; i < knownCounts.Count; i++)
            {
                var count = knownCounts[i];
                if (count < 0)
                    throw new InvalidOperationException("Diagnostic summary issue source reports an invalid negative count.");
                if (count > MaxIssueCount)
                    throw new InvalidOperationException("Diagnostic summary supports at most " + MaxIssueCount.ToString(CultureInfo.InvariantCulture) + " health issues.");
                if (observedCount.HasValue && count != observedCount.Value)
                    throw new InvalidOperationException("Diagnostic summary issue source reports conflicting known counts.");
                observedCount = count;
            }

            if (admittedCount.HasValue)
            {
                if (!observedCount.HasValue || observedCount.Value != admittedCount.Value)
                    throw new InvalidOperationException("Diagnostic summary issue source Count changed during traversal.");
            }
            else if (observedCount.HasValue && admittedCount != observedCount)
            {
                return observedCount;
            }

            return observedCount;
        }

        public static void Export(string path, ProjectState project, IEnumerable<ModelHealthIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Diagnostic summary path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            var content = Build(project, issues);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, StrictUtf8))
                {
                    writer.Write(content);
                    writer.Flush();
                    stream.Flush(true);
                }
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static string CanonicalCode(string value)
        {
            var code = (value ?? string.Empty).Trim().ToUpperInvariant();
            return code.Length == 0 ? "UNKNOWN" : code;
        }

        private static void JsonString(StringBuilder sb, int indent, string name, string value, bool comma)
        {
            sb.Append(new string(' ', indent * 2)).Append('"').Append(Escape(name)).Append("\":\"").Append(Escape(value)).Append('"');
            sb.Append(comma ? ",\n" : "\n");
        }

        private static void JsonNumber(StringBuilder sb, int indent, string name, int value, bool comma)
        {
            sb.Append(new string(' ', indent * 2)).Append('"').Append(Escape(name)).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
            sb.Append(comma ? ",\n" : "\n");
        }

        private static string Escape(string value)
        {
            var input = value ?? string.Empty;
            StrictUtf8.GetByteCount(input);
            var sb = new StringBuilder(input.Length + 8);
            foreach (var ch in input)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        private sealed class CategoryCount
        {
            public CategoryCount(string category, int count) { Category = category; Count = count; }
            public string Category { get; }
            public int Count { get; }
        }

        private sealed class HealthCount
        {
            public HealthCount(HealthSeverity severity, string code, int count) { Severity = severity; Code = code; Count = count; }
            public HealthSeverity Severity { get; }
            public string Code { get; }
            public int Count { get; }
        }
    }
}
