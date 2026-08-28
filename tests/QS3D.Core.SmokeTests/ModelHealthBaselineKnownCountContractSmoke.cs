using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthBaselineKnownCountContractSmoke
    {
        public static void Run()
        {
            NegativeKnownCountFailsBeforeEnumeration();
            OversizedKnownCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            UnderTraversalMismatchFailsClosed();
            OverTraversalMismatchFailsClosed();
            HonestCountedInputPreservesBaselineSemantics();
            PureStreamingInputIsAccepted();
            PureStreamingOverflowFailsClosed();
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var source = new CountedIssues(Array.Empty<ModelHealthIssue>(), -1, -1, -1);
            ExpectInvalid(() => Capture(source), "Negative known issue count must fail closed.");
            Require(!source.EnumerationRequested, "Negative known issue count must be rejected before GetEnumerator().");
        }

        private static void OversizedKnownCountFailsBeforeEnumeration()
        {
            var count = checked(HealthSummary.MaxIssueCount + 1);
            var source = new CountedIssues(Array.Empty<ModelHealthIssue>(), count, count, count);
            ExpectInvalid(() => Capture(source), "Oversized known issue count must fail closed.");
            Require(!source.EnumerationRequested, "Oversized known issue count must be rejected before GetEnumerator().");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new CountedIssues(Array.Empty<ModelHealthIssue>(), 0, 1, 0);
            ExpectInvalid(() => Capture(source), "Conflicting known issue counts must fail closed.");
            Require(!source.EnumerationRequested, "Conflicting known issue counts must be rejected before GetEnumerator().");
        }

        private static void UnderTraversalMismatchFailsClosed()
        {
            var source = new CountedIssues(
                new[] { Issue("A", HealthSeverity.Warning, "one", "E1") },
                2,
                2,
                2);
            ExpectInvalid(() => Capture(source), "Known Count larger than traversal must fail closed.");
            Require(source.EnumerationRequested, "Traversal mismatch smoke must exercise enumeration.");
        }

        private static void OverTraversalMismatchFailsClosed()
        {
            var source = new CountedIssues(
                new[]
                {
                    Issue("A", HealthSeverity.Warning, "one", "E1"),
                    Issue("B", HealthSeverity.Info, "two", "E2")
                },
                1,
                1,
                1);
            ExpectInvalid(() => Capture(source), "Known Count smaller than traversal must fail closed.");
            Require(source.EnumerationRequested, "Traversal mismatch smoke must exercise enumeration.");
        }

        private static void HonestCountedInputPreservesBaselineSemantics()
        {
            var error = Issue("ERR", HealthSeverity.Error, "error", "E2");
            var duplicateError = Issue("ERR", HealthSeverity.Error, "error", "E2");
            var warning = Issue("WARN", HealthSeverity.Warning, "warning", "E1");
            var source = new CountedIssues(new[] { warning, duplicateError, error }, 3, 3, 3);

            var baseline = Capture(source);

            Require(source.EnumerationRequested, "Honest counted input must be enumerated.");
            Require(baseline.ProjectId == "baseline-bound-smoke", "Baseline must preserve project affinity.");
            Require(baseline.Issues.Count == 2, "Baseline deduplication semantics changed.");
            Require(baseline.ErrorCount == 1 && baseline.WarningCount == 1 && baseline.InfoCount == 0, "Baseline severity counters changed.");
            Require(baseline.Issues[0].Severity == HealthSeverity.Error, "Baseline deterministic severity sorting changed.");
        }

        private static void PureStreamingInputIsAccepted()
        {
            var baseline = Capture(Stream(
                Issue("INFO", HealthSeverity.Info, "info", "E2"),
                Issue("WARN", HealthSeverity.Warning, "warning", "E1")));

            Require(baseline.Issues.Count == 2, "Pure streaming input must remain supported.");
            Require(baseline.Issues[0].Severity == HealthSeverity.Warning, "Streaming baseline sorting changed.");
        }

        private static void PureStreamingOverflowFailsClosed()
        {
            var repeated = Issue("REPEAT", HealthSeverity.Info, "repeat", "E1");
            ExpectInvalid(
                () => Capture(Repeat(repeated, checked(HealthSummary.MaxIssueCount + 1))),
                "Pure streaming input beyond the diagnostics issue ceiling must fail closed.");
        }

        private static ModelHealthBaseline Capture(IEnumerable<ModelHealthIssue> issues)
        {
            return new ModelHealthBaselineService().Capture(new ProjectState("baseline-bound-smoke", "Baseline Bound Smoke"), issues);
        }

        private static ModelHealthIssue Issue(string code, HealthSeverity severity, string message, string elementId)
        {
            return new ModelHealthIssue(code, severity, message, elementId);
        }

        private static IEnumerable<ModelHealthIssue> Stream(params ModelHealthIssue[] issues)
        {
            foreach (var issue in issues) yield return issue;
        }

        private static IEnumerable<ModelHealthIssue> Repeat(ModelHealthIssue issue, int count)
        {
            for (var i = 0; i < count; i++) yield return issue;
        }

        private static void ExpectInvalid(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception(message);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private sealed class CountedIssues : ICollection<ModelHealthIssue>, IReadOnlyCollection<ModelHealthIssue>, ICollection
        {
            private readonly List<ModelHealthIssue> _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            public CountedIssues(IEnumerable<ModelHealthIssue> items, int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _items = new List<ModelHealthIssue>(items);
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            public bool EnumerationRequested { get; private set; }

            int ICollection<ModelHealthIssue>.Count => _genericCount;
            int IReadOnlyCollection<ModelHealthIssue>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<ModelHealthIssue>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                EnumerationRequested = true;
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<ModelHealthIssue>.Add(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection<ModelHealthIssue>.Clear() => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Contains(ModelHealthIssue item) => _items.Contains(item);
            void ICollection<ModelHealthIssue>.CopyTo(ModelHealthIssue[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<ModelHealthIssue>.Remove(ModelHealthIssue item) => throw new NotSupportedException();

            void ICollection.CopyTo(Array array, int index)
            {
                for (var i = 0; i < _items.Count; i++) array.SetValue(_items[i], index + i);
            }
        }
    }
}
