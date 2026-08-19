using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceKnownCountTraversalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FactsUnderEnumerationFailsClosed();
            FactsOverEnumerationFailsClosed();
            AdjustmentsUnderEnumerationFailsClosed();
            MessagesOverEnumerationFailsClosed();
            HonestCountedSourceRemainsAccepted();
            PureStreamingSourceRemainsAccepted();
        }

        private static void FactsUnderEnumerationFailsClosed()
        {
            var source = new DishonestReadOnlyCollection<MeasurementTraceFact>(
                reportedCount: 2,
                new MeasurementTraceFact("fact-a", 1d, "m", "source-a"));

            ExpectTraversalMismatch(() => Trace(inputFacts: source), "facts under-enumeration");
            Equal(1, source.GetEnumeratorCalls, "Facts mismatch should consume exactly one enumeration attempt.");
        }

        private static void FactsOverEnumerationFailsClosed()
        {
            var source = new DishonestReadOnlyCollection<MeasurementTraceFact>(
                reportedCount: 1,
                new MeasurementTraceFact("fact-a", 1d, "m", "source-a"),
                new MeasurementTraceFact("fact-b", 2d, "m", "source-b"));

            ExpectTraversalMismatch(() => Trace(inputFacts: source), "facts over-enumeration");
            Equal(1, source.GetEnumeratorCalls, "Facts mismatch should not require a second traversal.");
        }

        private static void AdjustmentsUnderEnumerationFailsClosed()
        {
            var source = new DishonestReadOnlyCollection<MeasurementTraceAdjustment>(
                reportedCount: 2,
                new MeasurementTraceAdjustment(
                    MeasurementTraceAdjustmentKind.Addition,
                    0d,
                    "m",
                    "zero-control",
                    "adjustment-source"));

            ExpectTraversalMismatch(() => Trace(adjustments: source), "adjustments under-enumeration");
            Equal(1, source.GetEnumeratorCalls, "Adjustment mismatch should consume exactly one enumeration attempt.");
        }

        private static void MessagesOverEnumerationFailsClosed()
        {
            var source = new DishonestReadOnlyCollection<string>(
                reportedCount: 1,
                "warning-a",
                "warning-b");

            ExpectTraversalMismatch(() => Trace(warnings: source), "messages over-enumeration");
            Equal(1, source.GetEnumeratorCalls, "Message mismatch should not require a second traversal.");
        }

        private static void HonestCountedSourceRemainsAccepted()
        {
            var fact = new MeasurementTraceFact("fact-honest", 3d, "m", "source-honest");
            var source = new DishonestReadOnlyCollection<MeasurementTraceFact>(1, fact);

            var trace = Trace(inputFacts: source);

            Equal(1, source.GetEnumeratorCalls, "Honest counted source should be enumerated once.");
            Equal(1, trace.InputFacts.Count, "Honest counted source lost its fact.");
            Equal("fact-honest", trace.InputFacts[0].Name, "Honest counted source changed fact identity.");
        }

        private static void PureStreamingSourceRemainsAccepted()
        {
            var source = new StreamingEnumerable<string>("assumption-b", "assumption-a");

            var trace = Trace(assumptions: source);

            Equal(1, source.GetEnumeratorCalls, "Pure streaming source should be traversed once.");
            Equal(2, trace.Assumptions.Count, "Pure streaming source lost messages.");
            Equal("assumption-a", trace.Assumptions[0], "Pure streaming source lost canonical ordering.");
            Equal("assumption-b", trace.Assumptions[1], "Pure streaming source lost canonical ordering.");
        }

        private static MeasurementTrace Trace(
            IEnumerable<MeasurementTraceFact>? inputFacts = null,
            IEnumerable<MeasurementTraceAdjustment>? adjustments = null,
            IEnumerable<string>? warnings = null,
            IEnumerable<string>? assumptions = null)
        {
            return new MeasurementTrace(
                "SEM-TRACE-TRAVERSAL",
                "SRC-TRACE-TRAVERSAL",
                "QTY-TRACE-TRAVERSAL",
                inputFacts ?? Array.Empty<MeasurementTraceFact>(),
                1d,
                adjustments ?? Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                "m",
                "none",
                warnings,
                assumptions);
        }

        private static void ExpectTraversalMismatch(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("count does not match source traversal", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "MeasurementTraceKnownCountTraversalSmoke returned the wrong diagnostic for " + label + ": " + ex.Message,
                        ex);
                return;
            }

            throw new InvalidOperationException(
                "MeasurementTraceKnownCountTraversalSmoke did not reject " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class DishonestReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly int _reportedCount;
            private readonly T[] _items;

            internal DishonestReadOnlyCollection(int reportedCount, params T[] items)
            {
                _reportedCount = reportedCount;
                _items = items;
            }

            public int Count => _reportedCount;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;

            internal StreamingEnumerable(params T[] items)
            {
                _items = items;
            }

            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
