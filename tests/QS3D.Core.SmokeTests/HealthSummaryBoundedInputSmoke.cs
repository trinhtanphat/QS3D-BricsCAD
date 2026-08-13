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

        private sealed class SyntheticIssueEnumerationException : Exception
        {
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
