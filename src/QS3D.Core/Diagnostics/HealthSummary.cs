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
            var expectedKnownCount = RequireKnownCountsWithinLimit(issues, out var expectedKnownCountSources);

            var result = new List<ModelHealthIssue>(Math.Min(MaxIssueCount, 256));
            using (var enumerator = issues.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);
                    if (!enumerator.MoveNext())
                    {
                        RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);
                        break;
                    }

                    RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);
                    if (expectedKnownCount.HasValue && result.Count >= expectedKnownCount.Value)
                        throw new InvalidOperationException(
                            "Health summary traversal produced more diagnostic issues than its known count of " + expectedKnownCount.Value + ".");
                    if (result.Count >= MaxIssueCount)
                        throw new InvalidOperationException("Health summary supports at most " + MaxIssueCount + " diagnostic issues.");

                    var issue = enumerator.Current;
                    RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);
                    result.Add(issue);
                }
            }

            if (expectedKnownCount.HasValue && result.Count != expectedKnownCount.Value)
                throw new InvalidOperationException("Health summary known issue count does not match enumerated issue count.");

            RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);
            return result;
        }

        private static void RequireKnownCountStable(
            IEnumerable<ModelHealthIssue> issues,
            int? expectedKnownCount,
            int expectedKnownCountSources)
        {
            var currentKnownCount = RequireKnownCountsWithinLimit(issues, out var currentKnownCountSources);
            if (expectedKnownCountSources != currentKnownCountSources || expectedKnownCount != currentKnownCount)
                throw new InvalidOperationException(
                    "Health summary known issue count changed during traversal from " +
                    (expectedKnownCount.HasValue ? expectedKnownCount.Value.ToString() : "<none>") + " to " +
                    (currentKnownCount.HasValue ? currentKnownCount.Value.ToString() : "<none>") + ".");
        }

        private static int? RequireKnownCountsWithinLimit(
            IEnumerable<ModelHealthIssue> issues,
            out int knownCountSources)
        {
            var counts = new List<int>(3);
            knownCountSources = 0;
            if (issues is ICollection<ModelHealthIssue> collection)
            {
                knownCountSources |= 1;
                counts.Add(collection.Count);
            }
            if (issues is IReadOnlyCollection<ModelHealthIssue> readOnlyCollection)
            {
                knownCountSources |= 2;
                counts.Add(readOnlyCollection.Count);
            }
            if (issues is System.Collections.ICollection nonGenericCollection)
            {
                knownCountSources |= 4;
                counts.Add(nonGenericCollection.Count);
            }

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

            if (maximum > MaxIssueCount)
                throw new InvalidOperationException("Health summary supports at most " + MaxIssueCount + " diagnostic issues.");
            if (hasNegative)
                throw new InvalidOperationException("Health summary received an invalid negative known issue count.");
            if (hasConflict)
                throw new InvalidOperationException("Health summary received conflicting known issue counts.");

            return expected;
        }
    }
}
