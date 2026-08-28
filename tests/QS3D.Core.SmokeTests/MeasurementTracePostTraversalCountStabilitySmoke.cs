using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTracePostTraversalCountStabilitySmoke
    {
        internal static void Run()
        {
            FactCountDriftAfterExactTraversalFailsClosed();
            AdjustmentCountDriftAfterExactTraversalFailsClosed();
            MessageCountDriftAfterExactTraversalFailsClosed();
            NegativeFinalCountFailsClosed();
            StableCountedChildrenRemainAccepted();
            PureStreamingChildrenRemainAccepted();
        }

        private static void FactCountDriftAfterExactTraversalFailsClosed()
        {
            var facts = new PhaseReadOnlyCollection<MeasurementTraceFact>(
                new[] { new MeasurementTraceFact("length", 2d, "m", "SRC-F1") },
                initialCount: 1,
                finalCount: 0);

            var error = Capture<ArgumentException>(() => Trace(facts, Array.Empty<MeasurementTraceAdjustment>()));
            Equal(2, facts.CountReads, "Fact Count must be sampled before and after traversal.");
            Contains("count changed during enumeration", error.Message, "Fact Count drift must fail closed.");
        }

        private static void AdjustmentCountDriftAfterExactTraversalFailsClosed()
        {
            var adjustments = new PhaseReadOnlyCollection<MeasurementTraceAdjustment>(
                new[] { new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, 1d, "m3", "opening", "SRC-A1") },
                initialCount: 1,
                finalCount: 2);

            var error = Capture<ArgumentException>(() => Trace(Array.Empty<MeasurementTraceFact>(), adjustments));
            Equal(2, adjustments.CountReads, "Adjustment Count must be sampled before and after traversal.");
            Contains("count changed during enumeration", error.Message, "Adjustment Count drift must fail closed.");
        }

        private static void MessageCountDriftAfterExactTraversalFailsClosed()
        {
            var warnings = new PhaseReadOnlyCollection<string>(new[] { "review opening" }, initialCount: 1, finalCount: 0);

            var error = Capture<ArgumentException>(() => Trace(
                Array.Empty<MeasurementTraceFact>(),
                Array.Empty<MeasurementTraceAdjustment>(),
                warnings));
            Equal(2, warnings.CountReads, "Message Count must be sampled before and after traversal.");
            Contains("count changed during enumeration", error.Message, "Message Count drift must fail closed.");
        }

        private static void NegativeFinalCountFailsClosed()
        {
            var facts = new PhaseReadOnlyCollection<MeasurementTraceFact>(
                new[] { new MeasurementTraceFact("area", 3d, "m2", "SRC-F2") },
                initialCount: 1,
                finalCount: -1);

            var error = Capture<ArgumentException>(() => Trace(facts, Array.Empty<MeasurementTraceAdjustment>()));
            Equal(2, facts.CountReads, "Negative final Count must be observed by the post-traversal rebind.");
            Contains("count cannot be negative", error.Message, "Negative post-traversal Count must fail through canonical Count validation.");
        }

        private static void StableCountedChildrenRemainAccepted()
        {
            var facts = new PhaseReadOnlyCollection<MeasurementTraceFact>(
                new[] { new MeasurementTraceFact("length", 2d, "m", "SRC-F1") },
                initialCount: 1,
                finalCount: 1);
            var adjustments = new PhaseReadOnlyCollection<MeasurementTraceAdjustment>(
                new[] { new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, 1d, "m3", "opening", "SRC-A1") },
                initialCount: 1,
                finalCount: 1);
            var warnings = new PhaseReadOnlyCollection<string>(new[] { "review opening" }, initialCount: 1, finalCount: 1);

            var trace = Trace(facts, adjustments, warnings);
            Equal(1, trace.InputFacts.Count, "Stable counted facts must remain accepted.");
            Equal(1, trace.Adjustments.Count, "Stable counted adjustments must remain accepted.");
            Equal(1, trace.Warnings.Count, "Stable counted messages must remain accepted.");
            Equal(2, facts.CountReads, "Stable fact Count must be rebound.");
            Equal(2, adjustments.CountReads, "Stable adjustment Count must be rebound.");
            Equal(2, warnings.CountReads, "Stable message Count must be rebound.");
        }

        private static void PureStreamingChildrenRemainAccepted()
        {
            var trace = Trace(
                Stream(new MeasurementTraceFact("length", 2d, "m", "SRC-F1")),
                Stream(new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, 1d, "m3", "opening", "SRC-A1")),
                Stream("review opening"));

            Equal(1, trace.InputFacts.Count, "Pure-streaming facts must remain accepted.");
            Equal(1, trace.Adjustments.Count, "Pure-streaming adjustments must remain accepted.");
            Equal(1, trace.Warnings.Count, "Pure-streaming messages must remain accepted.");
        }

        private static MeasurementTrace Trace(
            IEnumerable<MeasurementTraceFact> facts,
            IEnumerable<MeasurementTraceAdjustment> adjustments,
            IEnumerable<string>? warnings = null)
        {
            return new MeasurementTrace(
                "E-1",
                "SRC-1",
                "volume",
                facts,
                10d,
                adjustments,
                9d,
                "m3",
                "nearest",
                warnings,
                Array.Empty<string>());
        }

        private static IEnumerable<T> Stream<T>(params T[] values)
        {
            for (var i = 0; i < values.Length; i++) yield return values[i];
        }

        private sealed class PhaseReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _finalCount;
            private bool _enumerated;

            internal PhaseReadOnlyCollection(T[] items, int initialCount, int finalCount)
            {
                _items = items;
                _initialCount = initialCount;
                _finalCount = finalCount;
            }

            internal int CountReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _enumerated ? _finalCount : _initialCount;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++) yield return _items[i];
                }
                finally
                {
                    _enumerated = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " was not thrown.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Message=" + actual);
        }
    }

    internal static class MeasurementTracePostTraversalCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementTracePostTraversalCountStabilitySmoke.Run();
        }
    }
}
