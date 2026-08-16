using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageReportBoundSmoke
    {
        private const int MaximumFindings = 10000;

        internal static void Run()
        {
            CountedOversizeFailsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedFinding();
            ExactBoundaryIsAccepted();
            OrderingAndIssueAggregationRemainDeterministic();
            NullFindingStillReportsItsIndex();
            NullInputStillFailsFast();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(MaximumFindings + 1);
            var error = Capture<InvalidOperationException>(() => MeasurementWorkItemCoverageReport.Create(source));
            Equal(0, source.GetEnumeratorCalls, "Known oversized coverage input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Oversize diagnostic must state the report bound.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedFinding()
        {
            var source = new StreamingFindings(MaximumFindings + 2);
            var error = Capture<InvalidOperationException>(() => MeasurementWorkItemCoverageReport.Create(source));
            Equal(MaximumFindings + 1, source.YieldedCount, "Streaming ingestion must stop after observing finding 10,001.");
            Contains("at most 10000", error.Message, "Streaming oversize diagnostic must state the report bound.");
        }

        private static void ExactBoundaryIsAccepted()
        {
            var source = new MeasurementWorkItemCoverageFinding[MaximumFindings];
            for (var i = 0; i < source.Length; i++)
                source[i] = Finding("E-" + i.ToString("D5"), null, null, MeasurementWorkItemCoverageIssue.MissingQuantity);

            var report = MeasurementWorkItemCoverageReport.Create(source);
            Equal(MaximumFindings, report.TotalCount, "Coverage report must accept exactly 10,000 findings.");
            Equal(MaximumFindings, report.MissingQuantityCount, "Boundary report issue aggregation changed.");
            Equal("E-00000", report.Rows[0].ElementId, "Boundary report first row ordering changed.");
            Equal("E-09999", report.Rows[report.Rows.Count - 1].ElementId, "Boundary report final row ordering changed.");
        }

        private static void OrderingAndIssueAggregationRemainDeterministic()
        {
            var source = new[]
            {
                Finding("b", "Length", 2d, MeasurementWorkItemCoverageIssue.StaleQuantity, MeasurementWorkItemCoverageIssue.UnmappedWorkItem),
                Finding("A", null, null, MeasurementWorkItemCoverageIssue.MissingQuantity),
                Finding("a", "Area", 1d, MeasurementWorkItemCoverageIssue.UnmappedWorkItem)
            };

            var report = MeasurementWorkItemCoverageReport.Create(source);
            Equal(3, report.TotalCount, "Ordinary report total changed.");
            Equal(0, report.ReadyCount, "Ordinary report ready count changed.");
            Equal(1, report.MissingQuantityCount, "Missing quantity aggregation changed.");
            Equal(1, report.StaleQuantityCount, "Stale quantity aggregation changed.");
            Equal(2, report.UnmappedWorkItemCount, "Unmapped aggregation changed.");
            Equal("A", report.Rows[0].ElementId, "Ordinal tie-break ordering changed.");
            Equal("a", report.Rows[1].ElementId, "Case-insensitive ordering changed.");
            Equal("b", report.Rows[2].ElementId, "Final deterministic ordering changed.");
        }

        private static void NullFindingStillReportsItsIndex()
        {
            var source = new MeasurementWorkItemCoverageFinding[]
            {
                Finding("E-1", null, null, MeasurementWorkItemCoverageIssue.MissingQuantity),
                null!
            };
            var error = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            Contains("index 1", error.Message, "Null-finding diagnostic lost the input index.");
        }

        private static void NullInputStillFailsFast()
        {
            Capture<ArgumentNullException>(() => MeasurementWorkItemCoverageReport.Create(null!));
        }

        private static MeasurementWorkItemCoverageFinding Finding(
            string elementId,
            string? quantityKey,
            double? quantityValue,
            params MeasurementWorkItemCoverageIssue[] issues)
        {
            var constructor = typeof(MeasurementWorkItemCoverageFinding).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[]
                {
                    typeof(string),
                    typeof(ElementCategory),
                    typeof(string),
                    typeof(double?),
                    typeof(MeasurementWorkItemMapping),
                    typeof(IEnumerable<MeasurementWorkItemCoverageIssue>)
                },
                modifiers: null);
            if (constructor == null)
                throw new InvalidOperationException("Coverage finding constructor contract changed.");

            return (MeasurementWorkItemCoverageFinding)constructor.Invoke(new object?[]
            {
                elementId,
                ElementCategory.Wall,
                quantityKey,
                quantityValue,
                null,
                issues
            });
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedNeverEnumerated : IReadOnlyCollection<MeasurementWorkItemCoverageFinding>
        {
            internal CountedNeverEnumerated(int count) { Count = count; }
            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<MeasurementWorkItemCoverageFinding> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingFindings : IEnumerable<MeasurementWorkItemCoverageFinding>
        {
            private readonly int _count;
            internal StreamingFindings(int count) { _count = count; }
            internal int YieldedCount { get; private set; }
            public IEnumerator<MeasurementWorkItemCoverageFinding> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Finding("S-" + i.ToString("D5"), null, null, MeasurementWorkItemCoverageIssue.MissingQuantity);
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class MeasurementWorkItemCoverageReportBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementWorkItemCoverageReportBoundSmoke.Run();
        }
    }
}
