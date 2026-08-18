using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionTraversalCountSmoke
    {
        private const string SemanticIdentity = "frozen-estimate-traversal";
        private const string SourceIdentity = "element-traversal";
        private const string QuantityKey = "net-volume";
        private const string Unit = "m3";
        private const string Currency = "USD";
        private static readonly DateTime EffectiveUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AsOfUtc = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ReportedCountGreaterThanTraversalFailsClosed();
            ReportedCountLessThanTraversalFailsClosed();
            HonestCountedSourceRemainsAccepted();
            PureStreamingSourceRemainsAccepted();
        }

        private static void ReportedCountGreaterThanTraversalFailsClosed()
        {
            var source = new DishonestReadOnlyCollection<EstimateLine>(
                2,
                CreateLine(1));

            AssertTraversalMismatch(() => FrozenEstimateProjection.Create(source));
            Assert(source.GetEnumeratorCalls == 1,
                "Count 2 -> traversal 1 must consume exactly one traversal before failing closed.");
        }

        private static void ReportedCountLessThanTraversalFailsClosed()
        {
            var source = new DishonestReadOnlyCollection<EstimateLine>(
                1,
                CreateLine(1),
                CreateLine(2));

            AssertTraversalMismatch(() => FrozenEstimateProjection.Create(source));
            Assert(source.GetEnumeratorCalls == 1,
                "Count 1 -> traversal 2 must consume exactly one traversal before failing closed.");
        }

        private static void HonestCountedSourceRemainsAccepted()
        {
            var source = new DishonestReadOnlyCollection<EstimateLine>(1, CreateLine(1));
            var projection = FrozenEstimateProjection.Create(source);

            Assert(source.GetEnumeratorCalls == 1, "Honest counted source must be traversed once.");
            Assert(projection.Rows.Count == 1, "Honest counted source lost its estimate row.");
            Assert(string.Equals(projection.Rows[0].EstimateLineId, "frozen-traversal-line-1", StringComparison.Ordinal),
                "Honest counted source changed estimate-line identity.");
        }

        private static void PureStreamingSourceRemainsAccepted()
        {
            var source = new StreamingEnumerable<EstimateLine>(CreateLine(2), CreateLine(1));
            var projection = FrozenEstimateProjection.Create(source);

            Assert(source.GetEnumeratorCalls == 1, "Pure streaming source must be traversed once.");
            Assert(projection.Rows.Count == 2, "Pure streaming source lost estimate rows.");
            Assert(string.Equals(projection.Rows[0].EstimateLineId, "frozen-traversal-line-1", StringComparison.Ordinal),
                "Pure streaming source lost canonical row ordering.");
            Assert(string.Equals(projection.Rows[1].EstimateLineId, "frozen-traversal-line-2", StringComparison.Ordinal),
                "Pure streaming source lost canonical row ordering.");
        }

        private static void AssertTraversalMismatch(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException error)
            {
                Assert(
                    string.Equals(
                        error.Message,
                        "Frozen estimate projection source Count does not match source traversal.",
                        StringComparison.Ordinal),
                    "Frozen estimate traversal mismatch returned the wrong diagnostic: " + error.Message);
                return;
            }

            throw new InvalidOperationException("Frozen estimate projection accepted a known Count/traversal mismatch.");
        }

        private static EstimateLine CreateLine(int index)
        {
            var code = new CostCode("COST-TRAVERSAL");
            var trace = new MeasurementTrace(
                SemanticIdentity,
                SourceIdentity,
                QuantityKey,
                Array.Empty<MeasurementTraceFact>(),
                1d,
                Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                Unit,
                "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var item = new RateItem(
                "rate-frozen-traversal",
                code,
                Unit,
                Currency,
                1m,
                EffectiveUtc,
                "v1");
            var rateBook = new RateBook("book-frozen-traversal", new[] { item });

            return EstimateLine.Create(
                "frozen-traversal-line-" + index,
                snapshot,
                SemanticIdentity,
                SourceIdentity,
                QuantityKey,
                rateBook,
                code,
                Currency,
                AsOfUtc);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
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
