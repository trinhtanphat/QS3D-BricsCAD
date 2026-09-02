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
            UnderYieldRejectsOnBothSurfaces();
            CountDriftRejectsOnBothSurfaces();
            ConflictingAndNegativeCountsRejectBeforeTraversal();
            NullAndDuplicateEvidenceRemainRejected();
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
            var source = new StreamingProbe<RateItem>(10001, i => Item("R-" + i.ToString("D5"), i));
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

        private static void UnderYieldRejectsOnBothSurfaces()
        {
            var rates = new CountProbeCollection<RateItem>(2, Item("R-1"));
            var rateError = Capture<InvalidOperationException>(() => new RateBook("book", rates));
            Contains("traversal count", rateError.Message, "rate-book under-yield diagnostic");
            Equal(2, rates.MoveNextCalls, "rate-book under-yield MoveNext");
            Equal(1, rates.CurrentReads, "rate-book under-yield Current");

            var lines = new CountProbeCollection<EstimateLine>(2, Line("L-1"));
            var projectionError = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(lines));
            Contains("Count does not match", projectionError.Message, "projection under-yield diagnostic");
            Equal(2, lines.MoveNextCalls, "projection under-yield MoveNext");
            Equal(1, lines.CurrentReads, "projection under-yield Current");
        }

        private static void CountDriftRejectsOnBothSurfaces()
        {
            var rates = new SequencedCountCollection<RateItem>(new[] { 1, 2 }, Item("R-1"));
            var rateError = Capture<InvalidOperationException>(() => new RateBook("book", rates));
            Contains("changed during traversal", rateError.Message, "rate-book Count drift diagnostic");
            Equal(2, rates.CountReads, "rate-book Count drift rebind");

            var lines = new SequencedCountCollection<EstimateLine>(new[] { 1, 2 }, Line("L-1"));
            var projectionError = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(lines));
            Contains("changed during enumeration", projectionError.Message, "projection Count drift diagnostic");
            Equal(2, lines.CountReads, "projection Count drift rebind");
        }

        private static void ConflictingAndNegativeCountsRejectBeforeTraversal()
        {
            var conflictingRates = new DualCountCollection<RateItem>(1, 2, Item("R-1"));
            Contains("conflicting", Capture<InvalidOperationException>(() => new RateBook("book", conflictingRates)).Message, "rate-book conflicting Count");
            Equal(0, conflictingRates.MoveNextCalls, "rate-book conflicting Count traversal");

            var conflictingLines = new DualCountCollection<EstimateLine>(1, 2, Line("L-1"));
            Contains("conflicting", Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(conflictingLines)).Message, "projection conflicting Count");
            Equal(0, conflictingLines.MoveNextCalls, "projection conflicting Count traversal");

            var negativeRates = new CountProbeCollection<RateItem>(-1, Item("R-1"));
            Contains("negative", Capture<InvalidOperationException>(() => new RateBook("book", negativeRates)).Message, "rate-book negative Count");
            Equal(0, negativeRates.MoveNextCalls, "rate-book negative Count traversal");

            var negativeLines = new CountProbeCollection<EstimateLine>(-1, Line("L-1"));
            Contains("negative", Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(negativeLines)).Message, "projection negative Count");
            Equal(0, negativeLines.MoveNextCalls, "projection negative Count traversal");
        }

        private static void NullAndDuplicateEvidenceRemainRejected()
        {
            var nullRates = new CountProbeCollection<RateItem>(1, new RateItem[] { null! });
            Contains("null item", Capture<ArgumentException>(() => new RateBook("book", nullRates)).Message, "rate-book null evidence");

            var duplicateRates = new CountProbeCollection<RateItem>(2, Item("R-DUP"), Item("R-DUP"));
            Contains("Duplicate rate item id", Capture<ArgumentException>(() => new RateBook("book", duplicateRates)).Message, "rate-book duplicate evidence");

            var nullLines = new CountProbeCollection<EstimateLine>(1, new EstimateLine[] { null! });
            Contains("null line", Capture<ArgumentException>(() => FrozenEstimateProjection.Create(nullLines)).Message, "projection null evidence");

            var duplicateLines = new CountProbeCollection<EstimateLine>(2, Line("L-DUP"), Line("L-DUP"));
            Contains("Duplicate estimate line id", Capture<ArgumentException>(() => FrozenEstimateProjection.Create(duplicateLines)).Message, "projection duplicate evidence");
        }

        private static void HonestCountedEvidenceRemainsAccepted()
        {
            var rates = new CountProbeCollection<RateItem>(1, Item("R-1"));
            var book = new RateBook("book", rates);
            Equal(1, book.Items.Count, "honest rate-book count");
            Equal(6, rates.CountReads, "rate-book Count must be rebound at admission, before/after MoveNext, after Current, and before publication");

            var lines = new CountProbeCollection<EstimateLine>(1, Line("L-1"));
            var projection = FrozenEstimateProjection.Create(lines);
            Equal(1, projection.Rows.Count, "honest projection count");
            Equal(5, lines.CountReads, "projection Count must be rebound at admission, after GetEnumerator before traversal, before Current, after materialization, and before publication");
        }

        private static RateItem Item(string id, int effectiveOffset = 0) =>
            new RateItem(id, new CostCode("COST-1"), "m3", "USD", 1m, EffectiveUtc.AddTicks(effectiveOffset), "v1");

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

        private sealed class SequencedCountCollection<T> : ICollection<T>
        {
            private readonly int[] _counts;
            private readonly T[] _items;
            internal SequencedCountCollection(int[] counts, params T[] items) { _counts = counts; _items = items; }
            public int Count { get { var index = CountReads < _counts.Length ? CountReads : _counts.Length - 1; CountReads++; return _counts[index]; } }
            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class DualCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly T[] _items;
            internal DualCountCollection(int genericCount, int readOnlyCount, params T[] items) { _genericCount = genericCount; _readOnlyCount = readOnlyCount; _items = items; }
            public int Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly DualCountCollection<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(DualCountCollection<T> owner) { _owner = owner; }
                public T Current => _owner._items[_index];
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