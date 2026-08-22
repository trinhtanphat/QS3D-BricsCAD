using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Diagnostics
{
    public sealed class HealthSummary
    {
        public const int MaxIssueCount = 1000000;

        public HealthSummary(IEnumerable<ModelHealthIssue> issues)
        {
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            var normalized = MaterializeIssues(issues);
            if (normalized.Any(x => x == null))
                throw new InvalidOperationException("Health summary cannot contain a null diagnostic issue.");
            foreach (var issue in normalized)
            {
                if (!Enum.IsDefined(typeof(HealthSeverity), issue.Severity))
                    throw new InvalidOperationException("Health summary contains an undefined severity: " + (int)issue.Severity + ".");
            }
            Issues = normalized.AsReadOnly();
        }

        public IReadOnlyList<ModelHealthIssue> Issues { get; }
        public int Errors => Issues.Count(x => x.Severity == HealthSeverity.Error);
        public int Warnings => Issues.Count(x => x.Severity == HealthSeverity.Warning);
        public int Info => Issues.Count(x => x.Severity == HealthSeverity.Info);
        public bool IsHealthy => Errors == 0;
        public bool IsReleaseReady => Errors == 0 && Warnings == 0;

        private static List<ModelHealthIssue> MaterializeIssues(IEnumerable<ModelHealthIssue> issues)
        {
            var result = new List<ModelHealthIssue>(Math.Min(MaxIssueCount, 256));
            using (var enumerator = issues.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= MaxIssueCount)
                        throw new InvalidOperationException("Health summary supports at most " + MaxIssueCount + " diagnostic issues.");
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }
    }
}
