using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.Reporting
{
    internal static class ReportingRowProvenance
    {
        private const int MaxSourceHandleEntries = 10000;

        internal static void AppendSourceHandles(IList<string> target, IEnumerable<string> sourceHandles)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (sourceHandles == null) throw new ArgumentNullException(nameof(sourceHandles));

            RequireTargetWithinBound(target);
            var targetSnapshot = SnapshotTargetValues(target);
            var knownCount = ResolveKnownCount(sourceHandles);
            var existingIdentities = SnapshotTargetIdentities(targetSnapshot);
            var stagedIdentities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var staged = new List<string>();
            var index = 0;

            using (var enumerator = sourceHandles.GetEnumerator())
            {
                while (true)
                {
                    RequireStableTarget(target, targetSnapshot);
                    RequireStableKnownCount(sourceHandles, knownCount);
                    var moved = enumerator.MoveNext();
                    RequireStableTarget(target, targetSnapshot);
                    RequireStableKnownCount(sourceHandles, knownCount);
                    if (!moved) break;

                    if (knownCount.HasValue && index >= knownCount.Value)
                        throw new InvalidOperationException(
                            "Report provenance SourceHandles traversal produced more entries than its known Count of " + knownCount.Value + ".");
                    if (index >= MaxSourceHandleEntries)
                        throw new InvalidOperationException(
                            "Report provenance SourceHandles cannot exceed " + MaxSourceHandleEntries + " input entries.");

                    var raw = enumerator.Current;
                    RequireStableTarget(target, targetSnapshot);
                    RequireStableKnownCount(sourceHandles, knownCount);
                    var handle = raw ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(handle))
                        throw new InvalidOperationException("Report provenance contains an empty stored SourceHandles entry at index " + index + ". Repair source ownership before reporting.");
                    if (!string.Equals(handle, handle.Trim(), StringComparison.Ordinal))
                        throw new InvalidOperationException("Report provenance contains a non-canonical stored SourceHandles entry at index " + index + ". Repair source ownership before reporting.");

                    var identity = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
                    if (existingIdentities.Contains(identity) || !stagedIdentities.Add(identity))
                        throw new InvalidOperationException("Report provenance contains duplicate stored SourceHandles identity: " + handle + ". Repair source ownership before reporting.");
                    staged.Add(handle);
                    index++;
                }
            }

            RequireStableTarget(target, targetSnapshot);
            RequireStableKnownCount(sourceHandles, knownCount);
            if (knownCount.HasValue && index != knownCount.Value)
                throw new InvalidOperationException(
                    "Report provenance SourceHandles known Count reported " + knownCount.Value +
                    " entries but traversal produced " + index + ".");
            if (targetSnapshot.Length > MaxSourceHandleEntries - staged.Count)
                throw TooManyPublishedSourceHandles();

            RequireStableTarget(target, targetSnapshot);
            foreach (var handle in staged) target.Add(handle);
        }

        private static void RequireTargetWithinBound(IList<string> target)
        {
            if (target.Count > MaxSourceHandleEntries)
                throw TooManyPublishedSourceHandles();
        }

        private static InvalidOperationException TooManyPublishedSourceHandles()
        {
            return new InvalidOperationException(
                "Report provenance SourceHandles cannot exceed " + MaxSourceHandleEntries + " published entries.");
        }

        private static string[] SnapshotTargetValues(IList<string> target)
        {
            var count = target.Count;
            var snapshot = new string[count];
            for (var index = 0; index < count; index++)
                snapshot[index] = target[index];

            if (target.Count != count)
                throw new InvalidOperationException("Report provenance target changed while its SourceHandles state was being snapshotted.");
            for (var index = 0; index < count; index++)
            {
                if (!string.Equals(target[index], snapshot[index], StringComparison.Ordinal))
                    throw new InvalidOperationException("Report provenance target changed while its SourceHandles state was being snapshotted.");
            }
            return snapshot;
        }

        private static void RequireStableTarget(IList<string> target, string[] expected)
        {
            if (target.Count != expected.Length)
                throw new InvalidOperationException("Report provenance target SourceHandles changed during source traversal. Retry reporting against the current target state.");
            for (var index = 0; index < expected.Length; index++)
            {
                if (!string.Equals(target[index], expected[index], StringComparison.Ordinal))
                    throw new InvalidOperationException("Report provenance target SourceHandles changed during source traversal. Retry reporting against the current target state.");
            }
        }

        private static HashSet<string> SnapshotTargetIdentities(IEnumerable<string> target)
        {
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in target)
                identities.Add(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(value));
            return identities;
        }

        private static int? ResolveKnownCount(IEnumerable<string> values)
        {
            int? knownCount = null;
            if (values is ICollection<string> collection)
                knownCount = AcceptKnownCount(knownCount, collection.Count);
            if (values is IReadOnlyCollection<string> readOnlyCollection)
                knownCount = AcceptKnownCount(knownCount, readOnlyCollection.Count);
            if (values is ICollection nonGenericCollection)
                knownCount = AcceptKnownCount(knownCount, nonGenericCollection.Count);
            return knownCount;
        }

        private static int? AcceptKnownCount(int? knownCount, int candidate)
        {
            if (candidate < 0)
                throw new InvalidOperationException("Report provenance SourceHandles known Count cannot be negative.");
            if (candidate > MaxSourceHandleEntries)
                throw new InvalidOperationException(
                    "Report provenance SourceHandles cannot exceed " + MaxSourceHandleEntries + " input entries.");
            if (knownCount.HasValue && knownCount.Value != candidate)
                throw new InvalidOperationException(
                    "Report provenance SourceHandles exposes conflicting known Counts: " + knownCount.Value + " and " + candidate + ".");
            return candidate;
        }

        private static void RequireStableKnownCount(IEnumerable<string> values, int? expectedCount)
        {
            if (!expectedCount.HasValue) return;
            var observedCount = ResolveKnownCount(values);
            if (!observedCount.HasValue || observedCount.Value != expectedCount.Value)
                throw new InvalidOperationException(
                    "Report provenance SourceHandles known Count changed during traversal from " + expectedCount.Value + " to " +
                    (observedCount.HasValue ? observedCount.Value.ToString() : "<none>") + ".");
        }
    }
}