using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class ModelHealthBaseline
    {
        internal ModelHealthBaseline(string projectId, IEnumerable<ModelHealthIssue> issues)
        {
            ProjectId = projectId ?? string.Empty;
            Issues = Sort(issues).AsReadOnly();
        }

        public string ProjectId { get; }
        public IReadOnlyList<ModelHealthIssue> Issues { get; }
        public int ErrorCount => Issues.Count(x => x.Severity == HealthSeverity.Error);
        public int WarningCount => Issues.Count(x => x.Severity == HealthSeverity.Warning);
        public int InfoCount => Issues.Count(x => x.Severity == HealthSeverity.Info);

        private static List<ModelHealthIssue> Sort(IEnumerable<ModelHealthIssue> issues)
        {
            var normalized = (issues ?? Enumerable.Empty<ModelHealthIssue>()).ToList();
            if (normalized.Any(x => x == null))
                throw new InvalidOperationException("Model health baseline cannot contain a null diagnostic issue.");
            foreach (var issue in normalized)
            {
                if (!Enum.IsDefined(typeof(HealthSeverity), issue.Severity))
                    throw new InvalidOperationException("Model health baseline contains an undefined severity: " + (int)issue.Severity + ".");
            }
            return normalized
                .OrderByDescending(x => x.Severity)
                .ThenBy(x => x.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ElementId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Message ?? string.Empty, StringComparer.Ordinal)
                .ToList();
        }
    }

    public sealed class ModelHealthBaselineDiff
    {
        internal ModelHealthBaselineDiff(
            string projectId,
            IEnumerable<ModelHealthIssue> newIssues,
            IEnumerable<ModelHealthIssue> resolvedIssues,
            IEnumerable<ModelHealthIssue> persistentIssues)
        {
            ProjectId = projectId ?? string.Empty;
            NewIssues = new ModelHealthBaseline(ProjectId, newIssues).Issues;
            ResolvedIssues = new ModelHealthBaseline(ProjectId, resolvedIssues).Issues;
            PersistentIssues = new ModelHealthBaseline(ProjectId, persistentIssues).Issues;
        }

        public string ProjectId { get; }
        public IReadOnlyList<ModelHealthIssue> NewIssues { get; }
        public IReadOnlyList<ModelHealthIssue> ResolvedIssues { get; }
        public IReadOnlyList<ModelHealthIssue> PersistentIssues { get; }
        public bool HasRegressions => NewIssues.Count > 0;
        public bool HasImprovements => ResolvedIssues.Count > 0;
        public int NewErrorCount => NewIssues.Count(x => x.Severity == HealthSeverity.Error);
        public int NewWarningCount => NewIssues.Count(x => x.Severity == HealthSeverity.Warning);
        public int ResolvedErrorCount => ResolvedIssues.Count(x => x.Severity == HealthSeverity.Error);
        public int ResolvedWarningCount => ResolvedIssues.Count(x => x.Severity == HealthSeverity.Warning);
    }

    public sealed class ModelHealthBaselineService
    {
        public ModelHealthBaseline CaptureSemantic(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return Capture(project, new ComprehensiveModelHealthService().Inspect(project));
        }

        public ModelHealthBaseline Capture(ProjectState project, IEnumerable<ModelHealthIssue> issues)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            return new ModelHealthBaseline(project.ProjectId, Unique(MaterializeIssues(issues)));
        }

        public ModelHealthBaselineDiff Compare(ModelHealthBaseline before, ModelHealthBaseline after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            if (!string.Equals(before.ProjectId, after.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Model health baselines belong to different projects.");

            var beforeIndex = Index(before.Issues);
            var afterIndex = Index(after.Issues);
            var added = afterIndex.Where(x => !beforeIndex.ContainsKey(x.Key)).Select(x => x.Value);
            var resolved = beforeIndex.Where(x => !afterIndex.ContainsKey(x.Key)).Select(x => x.Value);
            var persistent = afterIndex.Where(x => beforeIndex.ContainsKey(x.Key)).Select(x => x.Value);
            return new ModelHealthBaselineDiff(before.ProjectId, added, resolved, persistent);
        }

        private static IReadOnlyList<ModelHealthIssue> MaterializeIssues(IEnumerable<ModelHealthIssue> issues)
        {
            var expectedKnownCount = RequireKnownCountsWithinLimit(issues);
            var result = new List<ModelHealthIssue>(Math.Min(expectedKnownCount ?? 256, 256));

            using (var enumerator = issues.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownCountStable(issues, expectedKnownCount);
                    var moved = enumerator.MoveNext();
                    RequireKnownCountStable(issues, expectedKnownCount);
                    if (!moved) break;
                    if (expectedKnownCount.HasValue && result.Count >= expectedKnownCount.Value)
                        throw new InvalidOperationException("Model health baseline known issue count does not match enumerated issue count.");
                    if (result.Count >= HealthSummary.MaxIssueCount)
                        throw new InvalidOperationException("Model health baseline supports at most " + HealthSummary.MaxIssueCount + " diagnostic issues.");

                    var issue = enumerator.Current;
                    RequireKnownCountStable(issues, expectedKnownCount);
                    if (issue == null)
                        throw new InvalidOperationException("Model health baseline cannot contain a null diagnostic issue.");
                    if (!Enum.IsDefined(typeof(HealthSeverity), issue.Severity))
                        throw new InvalidOperationException("Model health baseline contains an undefined severity: " + (int)issue.Severity + ".");
                    result.Add(issue);
                }
            }

            RequireKnownCountStable(issues, expectedKnownCount);
            if (expectedKnownCount.HasValue && result.Count != expectedKnownCount.Value)
                throw new InvalidOperationException("Model health baseline known issue count does not match enumerated issue count.");

            return result.AsReadOnly();
        }

        private static void RequireKnownCountStable(IEnumerable<ModelHealthIssue> issues, int? expectedKnownCount)
        {
            var currentKnownCount = RequireKnownCountsWithinLimit(issues);
            if (currentKnownCount != expectedKnownCount)
                throw new InvalidOperationException("Model health baseline known issue count changed during enumeration.");
        }

        private static int? RequireKnownCountsWithinLimit(IEnumerable<ModelHealthIssue> issues)
        {
            var counts = new List<int>(3);
            if (issues is ICollection<ModelHealthIssue> collection) counts.Add(collection.Count);
            if (issues is IReadOnlyCollection<ModelHealthIssue> readOnlyCollection) counts.Add(readOnlyCollection.Count);
            if (issues is System.Collections.ICollection nonGenericCollection) counts.Add(nonGenericCollection.Count);

            if (counts.Count == 0) return null;

            var expected = counts[0];
            var maximum = expected;
            var hasNegative = expected < 0;
            var hasConflict = false;
            for (var i = 1; i < counts.Count; i++)
            {
                if (counts[i] < 0) hasNegative = true;
                if (counts[i] != expected) hasConflict = true;
                if (counts[i] > maximum) maximum = counts[i];
            }

            if (maximum > HealthSummary.MaxIssueCount)
                throw new InvalidOperationException("Model health baseline supports at most " + HealthSummary.MaxIssueCount + " diagnostic issues.");
            if (hasNegative)
                throw new InvalidOperationException("Model health baseline received an invalid negative known issue count.");
            if (hasConflict)
                throw new InvalidOperationException("Model health baseline received conflicting known issue counts.");

            return expected;
        }

        private static IReadOnlyList<ModelHealthIssue> Unique(IEnumerable<ModelHealthIssue> issues)
        {
            return Index(issues).Values.ToList().AsReadOnly();
        }

        private static Dictionary<string, ModelHealthIssue> Index(IEnumerable<ModelHealthIssue> issues)
        {
            var result = new Dictionary<string, ModelHealthIssue>(StringComparer.Ordinal);
            foreach (var issue in issues ?? Enumerable.Empty<ModelHealthIssue>())
            {
                if (issue == null)
                    throw new InvalidOperationException("Model health baseline cannot contain a null diagnostic issue.");
                if (!Enum.IsDefined(typeof(HealthSeverity), issue.Severity))
                    throw new InvalidOperationException("Model health baseline contains an undefined severity: " + (int)issue.Severity + ".");
                var key = Key(issue);
                if (!result.ContainsKey(key)) result[key] = issue;
            }
            return result;
        }

        private static string Key(ModelHealthIssue issue)
        {
            var code = issue.Code ?? string.Empty;
            var key = KeyPart(((int)issue.Severity).ToString(System.Globalization.CultureInfo.InvariantCulture)) +
                      KeyPart(code.ToUpperInvariant()) +
                      KeyPart((issue.ElementId ?? string.Empty).ToUpperInvariant());
            return code.EndsWith("_STALE", StringComparison.OrdinalIgnoreCase)
                ? key
                : key + KeyPart(issue.Message ?? string.Empty);
        }

        private static string KeyPart(string value)
        {
            var text = value ?? string.Empty;
            return text.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + text;
        }
    }
}
