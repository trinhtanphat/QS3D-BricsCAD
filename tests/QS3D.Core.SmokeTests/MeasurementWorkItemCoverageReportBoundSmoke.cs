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
            var finding = CreateFinding();
            var source = new TrackingEnumerable(finding, MaximumFindingCount + 100);

            var ex = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));

            Assert(ex.ParamName == "findings", "Coverage report bound failure did not identify the findings parameter.");
            Assert(source.MoveNextCalls == MaximumFindingCount + 1, "Coverage report enumerated beyond the first disallowed finding.");
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
                types: new[]
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
                throw new InvalidOperationException("Could not locate the coverage-finding constructor required by the smoke regression.");

            return (MeasurementWorkItemCoverageFinding)constructor.Invoke(new object?[]
            {
                "coverage-element",
                default(ElementCategory),
                null,
                null,
                null,
                new[] { MeasurementWorkItemCoverageIssue.MissingQuantity }
            });
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " was not thrown.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TrackingEnumerable : IEnumerable<MeasurementWorkItemCoverageFinding>
        {
            private readonly MeasurementWorkItemCoverageFinding _finding;
            private readonly int _count;

            public TrackingEnumerable(MeasurementWorkItemCoverageFinding finding, int count)
            {
                _finding = finding ?? throw new ArgumentNullException(nameof(finding));
                _count = count;
            }

            public int MoveNextCalls { get; private set; }

            public IEnumerator<MeasurementWorkItemCoverageFinding> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    MoveNextCalls++;
                    yield return _finding;
                }

                MoveNextCalls++;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
