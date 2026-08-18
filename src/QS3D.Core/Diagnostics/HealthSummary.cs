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
            RequireKnownCountsWithinLimit(issues);

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

        private static void RequireKnownCountsWithinLimit(IEnumerable<ModelHealthIssue> issues)
        {
            var counts = new List<int>(3);
            if (issues is ICollection<ModelHealthIssue> collection) counts.Add(collection.Count);
            if (issues is IReadOnlyCollection<ModelHealthIssue> readOnlyCollection) counts.Add(readOnlyCollection.Count);
            if (issues is System.Collections.ICollection nonGenericCollection) counts.Add(nonGenericCollection.Count);

            if (counts.Count == 0) return;

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

            if (maximum > MaxIssueCount)
                throw new InvalidOperationException("Health summary supports at most " + MaxIssueCount + " diagnostic issues.");
            if (hasNegative)
                throw new InvalidOperationException("Health summary received an invalid negative known issue count.");
            if (hasConflict)
                throw new InvalidOperationException("Health summary received conflicting known issue counts.");
        }
    }
}
