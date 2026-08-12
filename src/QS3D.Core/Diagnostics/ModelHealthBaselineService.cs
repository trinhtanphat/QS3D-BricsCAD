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
            return new ModelHealthBaseline(project.ProjectId, Unique(issues));
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
