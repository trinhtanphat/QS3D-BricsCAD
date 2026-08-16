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
        private const int MaximumFindingCount = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            BoundaryIsAccepted();
            BoundaryPlusOneFailsClosedWithoutOverEnumeration();
            KnownCollectionOverflowFailsBeforeEnumeration();
            DishonestReadOnlyCollectionStillStopsAtFirstDisallowedItem();
            NullFindingValidationIsPreserved();
        }

        private static void BoundaryIsAccepted()
        {
            var finding = CreateFinding();
            var source = new TrackingEnumerable(finding, MaximumFindingCount);
            var report = MeasurementWorkItemCoverageReport.Create(source);
            Assert(report.TotalCount == MaximumFindingCount, "Coverage report rejected the supported finding-count boundary.");
            Assert(source.MoveNextCalls == MaximumFindingCount + 1, "Coverage report did not enumerate the bounded source exactly once through completion.");
        }

        private static void BoundaryPlusOneFailsClosedWithoutOverEnumeration()
        {
            var source = new TrackingEnumerable(CreateFinding(), MaximumFindingCount + 100);
            var ex = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            Assert(ex.ParamName == "findings", "Coverage report bound failure did not identify the findings parameter.");
            Assert(source.MoveNextCalls == MaximumFindingCount + 1, "Coverage report enumerated beyond the first disallowed finding.");
        }

        private static void KnownCollectionOverflowFailsBeforeEnumeration()
        {
            var source = new KnownCountCollection(CreateFinding(), MaximumFindingCount + 1);
            var ex = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            Assert(ex.ParamName == "findings", "Known-count overflow did not identify the findings parameter.");
            Assert(source.GetEnumeratorCalls == 0, "Known-count overflow enumerated input instead of failing before materialization.");
        }

        private static void DishonestReadOnlyCollectionStillStopsAtFirstDisallowedItem()
        {
            var source = new DishonestReadOnlyCollection(CreateFinding(), 1, MaximumFindingCount + 100);
            var ex = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            Assert(ex.ParamName == "findings", "Dishonest collection overflow did not identify the findings parameter.");
            Assert(source.MoveNextCalls == MaximumFindingCount + 1, "Dishonest collection bypassed the streaming limit+1 bound.");
        }

        private static void NullFindingValidationIsPreserved()
        {
            var ex = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(
                new MeasurementWorkItemCoverageFinding[] { CreateFinding(), null! }));
            Assert(ex.ParamName == "findings", "Coverage report null-finding validation changed parameter attribution.");
            Assert(ex.Message.Contains("index 1", StringComparison.Ordinal), "Coverage report null-finding validation lost its deterministic index.");
        }

        private static MeasurementWorkItemCoverageFinding CreateFinding()
        {
            var constructor = typeof(MeasurementWorkItemCoverageFinding).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(ElementCategory), typeof(string), typeof(double?), typeof(MeasurementWorkItemMapping), typeof(IEnumerable<MeasurementWorkItemCoverageIssue>) },
                modifiers: null);
            if (constructor == null)
                throw new InvalidOperationException("Could not locate the coverage-finding constructor required by the smoke regression.");
            return (MeasurementWorkItemCoverageFinding)constructor.Invoke(new object?[]
            {
                "coverage-element", default(ElementCategory), null, null, null,
                new[] { MeasurementWorkItemCoverageIssue.MissingQuantity }
            });
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + " was not thrown.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class TrackingEnumerable : IEnumerable<MeasurementWorkItemCoverageFinding>
        {
            private readonly MeasurementWorkItemCoverageFinding _finding;
            private readonly int _count;
            public TrackingEnumerable(MeasurementWorkItemCoverageFinding finding, int count) { _finding = finding; _count = count; }
            public int MoveNextCalls { get; private set; }
            public IEnumerator<MeasurementWorkItemCoverageFinding> GetEnumerator()
            {
                for (var index = 0; index < _count; index++) { MoveNextCalls++; yield return _finding; }
                MoveNextCalls++;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class KnownCountCollection : ICollection<MeasurementWorkItemCoverageFinding>
        {
            private readonly MeasurementWorkItemCoverageFinding _finding;
            public KnownCountCollection(MeasurementWorkItemCoverageFinding finding, int count) { _finding = finding; Count = count; }
            public int GetEnumeratorCalls { get; private set; }
            public int Count { get; }
            public bool IsReadOnly => true;
            public IEnumerator<MeasurementWorkItemCoverageFinding> GetEnumerator() { GetEnumeratorCalls++; for (var index = 0; index < Count; index++) yield return _finding; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(MeasurementWorkItemCoverageFinding item) => ReferenceEquals(item, _finding);
            public void CopyTo(MeasurementWorkItemCoverageFinding[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(MeasurementWorkItemCoverageFinding item) => throw new NotSupportedException();
            public bool Remove(MeasurementWorkItemCoverageFinding item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class DishonestReadOnlyCollection : IReadOnlyCollection<MeasurementWorkItemCoverageFinding>
        {
            private readonly MeasurementWorkItemCoverageFinding _finding;
            private readonly int _actualCount;
            public DishonestReadOnlyCollection(MeasurementWorkItemCoverageFinding finding, int reportedCount, int actualCount) { _finding = finding; Count = reportedCount; _actualCount = actualCount; }
            public int Count { get; }
            public int MoveNextCalls { get; private set; }
            public IEnumerator<MeasurementWorkItemCoverageFinding> GetEnumerator()
            {
                for (var index = 0; index < _actualCount; index++) { MoveNextCalls++; yield return _finding; }
                MoveNextCalls++;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
