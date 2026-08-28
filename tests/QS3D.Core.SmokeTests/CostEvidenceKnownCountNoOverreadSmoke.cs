using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class CostEvidenceKnownCountNoOverreadSmoke
    {
        private static readonly DateTime EffectiveUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AsOfUtc = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBookOverrunRejectsBeforeSecondCurrent();
            RateBookStreamingCeilingRejectsBeforeOverflowCurrent();
            FrozenProjectionOverrunRejectsBeforeSecondCurrent();
            FrozenProjectionStreamingCeilingRejectsBeforeOverflowCurrent();
            HonestCountedEvidenceRemainsAccepted();
        }

        private static void RateBookOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<RateItem>(1, Item("R-1"), Item("R-2"));
            var error = Capture<InvalidOperationException>(() => new RateBook("book", source));
            Contains("traversal count", error.Message, "rate-book overrun diagnostic");
            Equal(2, source.MoveNextCalls, "rate-book overrun MoveNext");
            Equal(1, source.CurrentReads, "rate-book overrun must reject before second Current");
        }

        private static void RateBookStreamingCeilingRejectsBeforeOverflowCurrent()
        {
            var source = new StreamingProbe<RateItem>(10001, i => Item("R-" + i.ToString("D5")));
            var error = Capture<InvalidOperationException>(() => new RateBook("book", source));
            Contains("at most 10000", error.Message, "rate-book streaming ceiling diagnostic");
            Equal(10001, source.MoveNextCalls, "rate-book streaming overflow MoveNext");
            Equal(10000, source.CurrentReads, "rate-book streaming overflow Current");
        }

        private static void FrozenProjectionOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<EstimateLine>(1, Line("L-1"), Line("L-2"));
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));
            Contains("Count does not match", error.Message, "projection overrun diagnostic");
            Equal(2, source.MoveNextCalls, "projection overrun MoveNext");
            Equal(1, source.CurrentReads, "projection overrun must reject before second Current");
        }

        private static void FrozenProjectionStreamingCeilingRejectsBeforeOverflowCurrent()
        {
            var source = new StreamingProbe<EstimateLine>(10001, i => Line("L-" + i.ToString("D5")));
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));
            Contains("at most 10000", error.Message, "projection streaming ceiling diagnostic");
            Equal(10001, source.MoveNextCalls, "projection streaming overflow MoveNext");
            Equal(10000, source.CurrentReads, "projection streaming overflow Current");
        }

        private static void HonestCountedEvidenceRemainsAccepted()
        {
            var rates = new CountProbeCollection<RateItem>(1, Item("R-1"));
            var book = new RateBook("book", rates);
            Equal(1, book.Items.Count, "honest rate-book count");
            Equal(2, rates.CountReads, "rate-book Count must be rebound");

            var lines = new CountProbeCollection<EstimateLine>(1, Line("L-1"));
            var projection = FrozenEstimateProjection.Create(lines);
            Equal(1, projection.Rows.Count, "honest projection count");
            Equal(2, lines.CountReads, "projection Count must be rebound");
        }

        private static RateItem Item(string id) => new RateItem(id, new CostCode("COST-1"), "m3", "USD", 1m, EffectiveUtc, "v1");

        private static EstimateLine Line(string id)
        {
            var trace = new MeasurementTrace("cost-evidence", "element-1", "net-volume", Array.Empty<MeasurementTraceFact>(), 1d, Array.Empty<MeasurementTraceAdjustment>(), 1d, "m3", "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var code = new CostCode("COST-1");
            var book = new RateBook("projection-book", new[] { Item("projection-rate") });
            return EstimateLine.Create(id, snapshot, "cost-evidence", "element-1", "net-volume", book, code, "USD", AsOfUtc);
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException error) { return error; }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string label)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(label + ": actual='" + actual + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountProbeCollection<T> : ICollection<T>
        {
            private readonly int _count;
            private readonly T[] _items;
            internal CountProbeCollection(int count, params T[] items) { _count = count; _items = items; }
            public int Count { get { CountReads++; return _count; } }
            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly CountProbeCollection<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(CountProbeCollection<T> owner) { _owner = owner; }
                public T Current { get { _owner.CurrentReads++; return _owner._items[_index]; } }
                object IEnumerator.Current => Current!;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._items.Length; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class StreamingProbe<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;
            internal StreamingProbe(int count, Func<int, T> factory) { _count = count; _factory = factory; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly StreamingProbe<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(StreamingProbe<T> owner) { _owner = owner; }
                public T Current { get { _owner.CurrentReads++; return _owner._factory(_index); } }
                object IEnumerator.Current => Current!;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._count; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
