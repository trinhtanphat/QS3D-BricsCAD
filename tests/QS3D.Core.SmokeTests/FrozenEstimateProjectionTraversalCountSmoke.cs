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
            TransientCountDriftBeforeCurrentFailsClosed();
            TransientCountDriftAfterMaterializationFailsClosed();
            HonestCountedSourceRemainsAccepted();
            PureStreamingSourceRemainsAccepted();
        }

        private static void ReportedCountGreaterThanTraversalFailsClosed()
        {
            var source = new StableReadOnlyCollection<EstimateLine>(2, CreateLine(1));
            AssertTraversalMismatch(() => FrozenEstimateProjection.Create(source));
            Assert(source.MoveNextCalls == 2, "Count 2 -> traversal 1 must reach natural end exactly once.");
        }

        private static void ReportedCountLessThanTraversalFailsBeforeUnexpectedLineValidation()
        {
            var source = new StableReadOnlyCollection<EstimateLine>(1, CreateLine(1), null!, CreateLine(3));
            AssertTraversalMismatch(() => FrozenEstimateProjection.Create(source));
            Assert(source.MoveNextCalls == 2, "Count overrun must stop on first unexpected item before Current validation.");
            Assert(source.CurrentReads == 1, "Count overrun must reject before the unexpected Current read.");
        }

        private static void CountChangesAfterExactTraversalFailsClosed()
        {
            var source = new CompletionDriftingCollection<EstimateLine>(1, 2, CreateLine(1));
            AssertCountChanged(() => FrozenEstimateProjection.Create(source));
            Assert(source.MoveNextCalls == 2, "Post-traversal drift must be observed after exact traversal completes.");
        }

        private static void NegativeCountAfterExactTraversalFailsClosed()
        {
            var source = new CompletionDriftingCollection<EstimateLine>(1, -1, CreateLine(1));
            AssertMessage(
                () => FrozenEstimateProjection.Create(source),
                "Frozen estimate projection source reports an invalid negative known count.");
            Assert(source.MoveNextCalls == 2, "Negative post-traversal Count must be observed after traversal completes.");
        }

        private static void TransientCountDriftBeforeCurrentFailsClosed()
        {
            var source = new SequencedCountCollection<EstimateLine>(
                new[] { 2, 1, 2 },
                CreateLine(1), CreateLine(2));

            AssertCountChanged(() => FrozenEstimateProjection.Create(source));
            Assert(source.CurrentReads == 0, "Transient pre-Current Count drift must fail before caller Current is consumed.");
            Assert(source.CountReads == 2, "Transient pre-Current drift must be caught by the first traversal checkpoint.");
        }

        private static void TransientCountDriftAfterMaterializationFailsClosed()
        {
            var source = new SequencedCountCollection<EstimateLine>(
                new[] { 1, 1, 2, 1 },
                CreateLine(1));

            AssertCountChanged(() => FrozenEstimateProjection.Create(source));
            Assert(source.CurrentReads == 1, "Post-materialization drift must occur after exactly one caller Current read.");
            Assert(source.CountReads == 3, "Post-materialization drift must be caught by the row checkpoint.");
        }

        private static void HonestCountedSourceRemainsAccepted()
        {
            var source = new StableReadOnlyCollection<EstimateLine>(1, CreateLine(1));
            var projection = FrozenEstimateProjection.Create(source);

            Assert(source.CountReads == 4,
                "Honest one-line counted source must bind admission, pre-Current, post-row, and final Count evidence.");
            Assert(source.GetEnumeratorCalls == 1, "Honest counted source must be traversed once.");
            Assert(source.MoveNextCalls == 2, "Honest one-line source must complete exactly one traversal.");
            Assert(source.CurrentReads == 1, "Honest one-line source must read Current exactly once.");
            Assert(projection.Rows.Count == 1, "Honest counted source lost its estimate row.");
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
                Assert(string.Equals(error.Message, expectedMessage, StringComparison.Ordinal),
                    "Frozen estimate Count integrity returned wrong diagnostic: " + error.Message);
                return;
            }

            throw new InvalidOperationException("Frozen estimate projection accepted invalid Count evidence.");
        }

        private static EstimateLine CreateLine(int index)
        {
            var code = new CostCode("COST-TRAVERSAL");
            var trace = new MeasurementTrace(
                SemanticIdentity, SourceIdentity, QuantityKey,
                Array.Empty<MeasurementTraceFact>(), 1d,
                Array.Empty<MeasurementTraceAdjustment>(), 1d, Unit, "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var item = new RateItem("rate-frozen-traversal", code, Unit, Currency, 1m, EffectiveUtc, "v1");
            var rateBook = new RateBook("book-frozen-traversal", new[] { item });
            return EstimateLine.Create(
                "frozen-traversal-line-" + index,
                snapshot, SemanticIdentity, SourceIdentity, QuantityKey,
                rateBook, code, Currency, AsOfUtc);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class StableReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly int _reportedCount;
            private readonly T[] _items;
            internal StableReadOnlyCollection(int reportedCount, params T[] items)
            {
                _reportedCount = reportedCount;
                _items = items;
            }
            public int Count { get { CountReads++; return _reportedCount; } }
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return new ProbeEnumerator<T>(_items, () => MoveNextCalls++, () => CurrentReads++);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CompletionDriftingCollection<T> : IReadOnlyCollection<T>
        {
            private readonly int _initialCount;
            private readonly int _finalCount;
            private readonly T[] _items;
            private bool _completed;
            internal CompletionDriftingCollection(int initialCount, int finalCount, params T[] items)
            {
                _initialCount = initialCount;
                _finalCount = finalCount;
                _items = items;
            }
            public int Count => _completed ? _finalCount : _initialCount;
            internal int MoveNextCalls { get; private set; }
            public IEnumerator<T> GetEnumerator() =>
                new CompletionTrackingEnumerator<T>(_items, () => MoveNextCalls++, () => _completed = true);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class SequencedCountCollection<T> : IReadOnlyCollection<T>
        {
            private readonly int[] _counts;
            private readonly T[] _items;
            private int _countIndex;
            internal SequencedCountCollection(int[] counts, params T[] items)
            {
                _counts = counts ?? throw new ArgumentNullException(nameof(counts));
                _items = items ?? throw new ArgumentNullException(nameof(items));
                if (_counts.Length == 0) throw new ArgumentException("Count sequence is required.", nameof(counts));
            }
            public int Count
            {
                get
                {
                    CountReads++;
                    var index = _countIndex < _counts.Length ? _countIndex++ : _counts.Length - 1;
                    return _counts[index];
                }
            }
            internal int CountReads { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() =>
                new ProbeEnumerator<T>(_items, () => { }, () => CurrentReads++);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ProbeEnumerator<T> : IEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            private readonly Action _onMoveNext;
            private readonly Action _onCurrent;
            internal ProbeEnumerator(IEnumerable<T> items, Action onMoveNext, Action onCurrent)
            {
                _inner = items.GetEnumerator();
                _onMoveNext = onMoveNext;
                _onCurrent = onCurrent;
            }
            public T Current { get { _onCurrent(); return _inner.Current; } }
            object IEnumerator.Current => Current!;
            public bool MoveNext() { _onMoveNext(); return _inner.MoveNext(); }
            public void Reset() => throw new NotSupportedException();
            public void Dispose() => _inner.Dispose();
        }

        private sealed class CompletionTrackingEnumerator<T> : IEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            private readonly Action _onMoveNext;
            private readonly Action _onComplete;
            private bool _completed;
            internal CompletionTrackingEnumerator(IEnumerable<T> items, Action onMoveNext, Action onComplete)
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
                if (!moved && !_completed) { _completed = true; _onComplete(); }
                return moved;
            }
            public void Reset() => throw new NotSupportedException();
            public void Dispose() => _inner.Dispose();
        }

        private sealed class StreamingEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;
            internal StreamingEnumerable(params T[] items) { _items = items; }
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