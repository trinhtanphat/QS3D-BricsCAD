using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class HealthSummaryBoundedInputSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactCapIsAcceptedInOnePass();
            FirstIssueBeyondCapIsRejectedInOnePass();
            ThrowingInputPropagatesWithoutAResult();
            OversizedKnownCountIsRejectedBeforeEnumeration();
            NegativeKnownCountIsRejectedBeforeEnumeration();
            ConflictingKnownCountsAreRejectedBeforeEnumeration();
            KnownCountUnderEnumerationIsRejected();
            KnownCountOverEnumerationIsRejected();
            HonestKnownCountIsAccepted();
            StreamingInputWithoutKnownCountRemainsAccepted();
        }

        private static void ExactCapIsAcceptedInOnePass()
        {
            var issue = new ModelHealthIssue("BOUNDED_INFO", HealthSeverity.Info, "Bounded issue");
            var source = new SingleUseIssueSequence(HealthSummary.MaxIssueCount, issue);

            var summary = new HealthSummary(source);

            Equal(HealthSummary.MaxIssueCount, summary.Issues.Count);
            Equal(1, source.EnumerationCount);
            Equal(HealthSummary.MaxIssueCount, source.YieldedCount);
            True(summary.IsReleaseReady);
        }

        private static void FirstIssueBeyondCapIsRejectedInOnePass()
        {
            var issue = new ModelHealthIssue("EXCESS_WARNING", HealthSeverity.Warning, "Excess issue");
            var source = new SingleUseIssueSequence(HealthSummary.MaxIssueCount + 1, issue);

            try
            {
                _ = new HealthSummary(source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Health summary supports at most " + HealthSummary.MaxIssueCount + " diagnostic issues.", ex.Message);
                Equal(1, source.EnumerationCount);
                Equal(HealthSummary.MaxIssueCount + 1, source.YieldedCount);
                return;
            }

            throw new InvalidOperationException("HealthSummary must reject the first issue beyond its public cap.");
        }

        private static void ThrowingInputPropagatesWithoutAResult()
        {
            var failure = new SyntheticIssueEnumerationException();
            var source = new ThrowingIssueSequence(failure);
            var completed = false;
            try
            {
                _ = new HealthSummary(source);
                completed = true;
            }
            catch (SyntheticIssueEnumerationException ex)
            {
                True(ReferenceEquals(failure, ex));
                Equal(1, source.EnumerationCount);
                Equal(1, source.YieldedCount);
                True(!completed);
                return;
            }

            throw new InvalidOperationException("HealthSummary must propagate the source enumeration failure.");
        }

        private static void OversizedKnownCountIsRejectedBeforeEnumeration()
        {
            var source = new AdversarialKnownCountCollection(
                HealthSummary.MaxIssueCount + 1,
                HealthSummary.MaxIssueCount + 1,
                HealthSummary.MaxIssueCount + 1);

            Throws<InvalidOperationException>(
                () => _ = new HealthSummary(source),
                "Health summary supports at most " + HealthSummary.MaxIssueCount + " diagnostic issues.");
            Equal(0, source.EnumerationCount);
        }

        private static void NegativeKnownCountIsRejectedBeforeEnumeration()
        {
            var source = new AdversarialKnownCountCollection(-1, -1, -1);

            Throws<InvalidOperationException>(
                () => _ = new HealthSummary(source),
                "Health summary received an invalid negative known issue count.");
            Equal(0, source.EnumerationCount);
        }

        private static void ConflictingKnownCountsAreRejectedBeforeEnumeration()
        {
            var source = new AdversarialKnownCountCollection(1, 2, 1);

            Throws<InvalidOperationException>(
                () => _ = new HealthSummary(source),
                "Health summary received conflicting known issue counts.");
            Equal(0, source.EnumerationCount);
        }

        private static void KnownCountUnderEnumerationIsRejected()
        {
            var issue = new ModelHealthIssue("UNDER_COUNT", HealthSeverity.Info, "Under-enumerated issue");
            var source = new KnownCountTraversalCollection(2, 1, issue);

            Throws<InvalidOperationException>(
                () => _ = new HealthSummary(source),
                "Health summary known issue count does not match enumerated issue count.");
            Equal(1, source.EnumerationCount);
            Equal(1, source.YieldedCount);
        }

        private static void KnownCountOverEnumerationIsRejected()
        {
            var issue = new ModelHealthIssue("OVER_COUNT", HealthSeverity.Info, "Over-enumerated issue");
            var source = new KnownCountTraversalCollection(1, 2, issue);

            Throws<InvalidOperationException>(
                () => _ = new HealthSummary(source),
                "Health summary traversal produced more diagnostic issues than its known count of 1.");
            Equal(1, source.EnumerationCount);
            Equal(2, source.YieldedCount);
        }

        private static void HonestKnownCountIsAccepted()
        {
            var issue = new ModelHealthIssue("HONEST_COUNT", HealthSeverity.Info, "Honest counted issue");
            var source = new KnownCountTraversalCollection(2, 2, issue);

            var summary = new HealthSummary(source);

            Equal(2, summary.Issues.Count);
            Equal(1, source.EnumerationCount);
            Equal(2, source.YieldedCount);
            True(summary.IsReleaseReady);
        }

        private static void StreamingInputWithoutKnownCountRemainsAccepted()
        {
            var issue = new ModelHealthIssue("STREAMING_INFO", HealthSeverity.Info, "Streaming issue");
            var source = new SingleUseIssueSequence(2, issue);

            var summary = new HealthSummary(source);

            Equal(2, summary.Issues.Count);
            Equal(1, source.EnumerationCount);
            Equal(2, source.YieldedCount);
        }

        private sealed class SingleUseIssueSequence : IEnumerable<ModelHealthIssue>
        {
            private readonly int _count;
            private readonly ModelHealthIssue _issue;

            public SingleUseIssueSequence(int count, ModelHealthIssue issue)
            {
                _count = count;
                _issue = issue;
            }

            public int EnumerationCount { get; private set; }
            public int YieldedCount { get; private set; }

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Health issue input was enumerated more than once.");
                return Enumerate().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<ModelHealthIssue> Enumerate()
            {
                for (var index = 0; index < _count; index++)
                {
                    YieldedCount++;
                    yield return _issue;
                }
            }
        }

        private sealed class ThrowingIssueSequence : IEnumerable<ModelHealthIssue>
        {
            private readonly SyntheticIssueEnumerationException _failure;

            public ThrowingIssueSequence(SyntheticIssueEnumerationException failure)
            {
                _failure = failure;
            }

            public int EnumerationCount { get; private set; }
            public int YieldedCount { get; private set; }

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                EnumerationCount++;
                return Enumerate().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<ModelHealthIssue> Enumerate()
            {
                YieldedCount++;
                yield return new ModelHealthIssue("FIRST", HealthSeverity.Info, "First issue");
                throw _failure;
            }
        }

        private sealed class AdversarialKnownCountCollection :
            ICollection<ModelHealthIssue>,
            IReadOnlyCollection<ModelHealthIssue>,
            ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            public AdversarialKnownCountCollection(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            public int EnumerationCount { get; private set; }

            int ICollection<ModelHealthIssue>.Count => _genericCount;
            int IReadOnlyCollection<ModelHealthIssue>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<ModelHealthIssue>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                EnumerationCount++;
                throw new InvalidOperationException("Invalid known-count input must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<ModelHealthIssue>.Add(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection<ModelHealthIssue>.Clear() => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Contains(ModelHealthIssue item) => false;
            void ICollection<ModelHealthIssue>.CopyTo(ModelHealthIssue[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Remove(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class KnownCountTraversalCollection :
            ICollection<ModelHealthIssue>,
            IReadOnlyCollection<ModelHealthIssue>,
            ICollection
        {
            private readonly int _knownCount;
            private readonly int _traversalCount;
            private readonly ModelHealthIssue _issue;

            public KnownCountTraversalCollection(int knownCount, int traversalCount, ModelHealthIssue issue)
            {
                _knownCount = knownCount;
                _traversalCount = traversalCount;
                _issue = issue;
            }

            public int EnumerationCount { get; private set; }
            public int YieldedCount { get; private set; }

            int ICollection<ModelHealthIssue>.Count => _knownCount;
            int IReadOnlyCollection<ModelHealthIssue>.Count => _knownCount;
            int ICollection.Count => _knownCount;
            bool ICollection<ModelHealthIssue>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Known-count issue input was enumerated more than once.");
                return Enumerate().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<ModelHealthIssue> Enumerate()
            {
                for (var index = 0; index < _traversalCount; index++)
                {
                    YieldedCount++;
                    yield return _issue;
                }
            }

            void ICollection<ModelHealthIssue>.Add(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection<ModelHealthIssue>.Clear() => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Contains(ModelHealthIssue item) => false;
            void ICollection<ModelHealthIssue>.CopyTo(ModelHealthIssue[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Remove(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class SyntheticIssueEnumerationException : Exception
        {
        }

        private static void Throws<T>(Action action, string expectedMessage) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                Equal(expectedMessage, ex.Message);
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("Expected condition to be true.");
        }
    }
}
