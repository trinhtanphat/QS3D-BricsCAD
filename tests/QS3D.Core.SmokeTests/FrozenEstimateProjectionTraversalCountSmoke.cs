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
            ReportedCountLessThanTraversalFailsBeforeUnexpectedLineValidation();
            CountChangesAfterExactTraversalFailsClosed();
            NegativeCountAfterExactTraversalFailsClosed();
            HonestCountedSourceRemainsAccepted();
            PureStreamingSourceRemainsAccepted();
        }

        private static void ReportedCountGreaterThanTraversalFailsClosed()
        {
            var source = new DishonestReadOnlyCollection<EstimateLine>(
                2,
                CreateLine(1));

            AssertTraversalMismatch(() => FrozenEstimateProjection.Create(source));
            Assert(source.CountReads == 1,
                "Count 2 -> traversal 1 must use the admitted Count and fail before a stability re-read.");
            Assert(source.GetEnumeratorCalls == 1,
                "Count 2 -> traversal 1 must consume exactly one traversal before failing closed.");
            Assert(source.MoveNextCalls == 2,
                "Count 2 -> traversal 1 must reach the natural end exactly once before failing closed.");
        }

        private static void ReportedCountLessThanTraversalFailsBeforeUnexpectedLineValidation()
        {
            var source = new DishonestReadOnlyCollection<EstimateLine>(
                1,
                CreateLine(1),
                null!,
                CreateLine(3));

            AssertTraversalMismatch(() => FrozenEstimateProjection.Create(source));
            Assert(source.CountReads == 1,
                "Count 1 -> traversal 2 must fail against the admitted Count without a stability re-read.");
            Assert(source.GetEnumeratorCalls == 1,
                "Count 1 -> traversal overrun must use one traversal.");
            Assert(source.MoveNextCalls == 2,
                "Count overrun must stop on the first unexpected item before null validation or later reads.");
        }

        private static void CountChangesAfterExactTraversalFailsClosed()
        {
            var source = new DriftingReadOnlyCollection<EstimateLine>(
                1,
                2,
                CreateLine(1));

            AssertCountChanged(() => FrozenEstimateProjection.Create(source));
            Assert(source.CountReads == 2,
                "Exact traversal must re-bind deterministic Count evidence after enumeration.");
            Assert(source.MoveNextCalls == 2,
                "Exact one-line traversal must finish before post-traversal Count stability is checked.");
        }

        private static void NegativeCountAfterExactTraversalFailsClosed()
        {
            var source = new DriftingReadOnlyCollection<EstimateLine>(
                1,
                -1,
                CreateLine(1));

            AssertMessage(
                () => FrozenEstimateProjection.Create(source),
                "Frozen estimate projection source reports an invalid negative known count.");
            Assert(source.CountReads == 2,
                "Post-traversal negative Count must be observed by the stability re-bind.");
            Assert(source.MoveNextCalls == 2,
                "Negative post-traversal Count must not be queried before exact traversal completes.");
        }

        private static void HonestCountedSourceRemainsAccepted()
        {
            var source = new DishonestReadOnlyCollection<EstimateLine>(1, CreateLine(1));
            var projection = FrozenEstimateProjection.Create(source);

            Assert(source.CountReads == 2,
                "Honest counted source must bind Count before and after traversal.");
            Assert(source.GetEnumeratorCalls == 1, "Honest counted source must be traversed once.");
            Assert(source.MoveNextCalls == 2, "Honest one-line source must complete exactly one traversal.");
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

        private static void AssertTraversalMismatch(Action action) =>
            AssertMessage(action, "Frozen estimate projection source Count does not match source traversal.");

        private static void AssertCountChanged(Action action) =>
            AssertMessage(action, "Frozen estimate projection source Count changed during enumeration.");

        private static void AssertMessage(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException error)
            {
                Assert(
                    string.Equals(error.Message, expectedMessage, StringComparison.Ordinal),
                    "Frozen estimate Count integrity returned the wrong diagnostic: " + error.Message);
                return;
            }

            throw new InvalidOperationException("Frozen estimate projection accepted invalid Count evidence.");
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

            public int Count
            {
                get
                {
                    CountReads++;
                    return _reportedCount;
                }
            }

            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return new CountingEnumerator<T>(_items, () => MoveNextCalls++);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DriftingReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly int _initialCount;
            private readonly int _finalCount;
            private readonly T[] _items;
            private bool _enumerationCompleted;

            internal DriftingReadOnlyCollection(int initialCount, int finalCount, params T[] items)
            {
                _initialCount = initialCount;
                _finalCount = finalCount;
                _items = items;
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _enumerationCompleted ? _finalCount : _initialCount;
                }
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<T> GetEnumerator() =>
                new CompletionTrackingEnumerator<T>(
                    _items,
                    () => MoveNextCalls++,
                    () => _enumerationCompleted = true);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CountingEnumerator<T> : IEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            private readonly Action _onMoveNext;

            internal CountingEnumerator(IEnumerable<T> items, Action onMoveNext)
            {
                _inner = items.GetEnumerator();
                _onMoveNext = onMoveNext;
            }

            public T Current => _inner.Current;
            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                _onMoveNext();
                return _inner.MoveNext();
            }

            public void Reset() => _inner.Reset();
            public void Dispose() => _inner.Dispose();
        }

        private sealed class CompletionTrackingEnumerator<T> : IEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            private readonly Action _onMoveNext;
            private readonly Action _onComplete;
            private bool _completed;

            internal CompletionTrackingEnumerator(
                IEnumerable<T> items,
                Action onMoveNext,
                Action onComplete)
            {
                _inner = items.GetEnumerator();
                _onMoveNext = onMoveNext;
                _onComplete = onComplete;
            }

            public T Current => _inner.Current;
            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                _onMoveNext();
                var moved = _inner.MoveNext();
                if (!moved && !_completed)
                {
                    _completed = true;
                    _onComplete();
                }

                return moved;
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose() => _inner.Dispose();
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
