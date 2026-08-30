using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialEstimatingKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PortfolioOverrunRejectsBeforeSecondCurrent();
            PortfolioUnderYieldFailsClosed();
            PortfolioPostTraversalCountDriftFailsClosed();
            PortfolioConflictingCountsFailBeforeTraversal();
            SelectedLineOverrunRejectsBeforeSecondCurrent();
            UnitRateOverrunRejectsBeforeSecondCurrent();
            SelectedLineStreamingCeilingRejectsBeforeOverflowCurrent();
            UnitRateStreamingCeilingRejectsBeforeOverflowCurrent();
            HonestCommercialCollectionsRemainAccepted();
        }

        private static void PortfolioOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<EstimatingLine>(
                1, 1, Line("L-1"), Line("L-2"));
            var error = Capture<InvalidOperationException>(() => new EstimatingPortfolio(source));
            Contains("line count changed during enumeration", error.Message, "portfolio overrun diagnostic");
            Equal(2, source.MoveNextCalls, "portfolio overrun boundary MoveNext");
            Equal(1, source.CurrentReads, "portfolio overrun must reject before second Current");
        }

        private static void PortfolioUnderYieldFailsClosed()
        {
            var source = new CountProbeCollection<EstimatingLine>(2, 2, Line("L-1"));
            var error = Capture<InvalidOperationException>(() => new EstimatingPortfolio(source));
            Contains("line count changed during enumeration", error.Message, "portfolio under-yield diagnostic");
            Equal(1, source.CurrentReads, "portfolio under-yield Current reads");
        }

        private static void PortfolioPostTraversalCountDriftFailsClosed()
        {
            var source = new CountProbeCollection<EstimatingLine>(1, 2, Line("L-1"));
            var error = Capture<InvalidOperationException>(() => new EstimatingPortfolio(source));
            Contains("known line count changed during enumeration", error.Message, "portfolio Count drift diagnostic");
            Equal(6, source.CountReads, "portfolio Count evidence must be rebound around traversal and after Current");
            Equal(1, source.CurrentReads, "portfolio Count rebind must not reread Current");
        }

        private static void PortfolioConflictingCountsFailBeforeTraversal()
        {
            var source = new ConflictingCountCollection<EstimatingLine>(1, 2, Line("L-1"));
            var error = Capture<InvalidOperationException>(() => new EstimatingPortfolio(source));
            Contains("conflicting known line counts", error.Message, "portfolio conflicting Count diagnostic");
            Equal(0, source.GetEnumeratorCalls, "portfolio conflicting Count must fail before traversal");
        }

        private static void SelectedLineOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<string>(1, 1, "L-1", "L-2");
            var error = Capture<InvalidOperationException>(() => Request(source, new[] { new UnitRateAssignment("m3", 10m) }));
            Contains("selected-line count changed during enumeration", error.Message, "selected-line overrun diagnostic");
            Equal(2, source.MoveNextCalls, "selected-line overrun boundary MoveNext");
            Equal(1, source.CurrentReads, "selected-line overrun must reject before second Current");
        }

        private static void UnitRateOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<UnitRateAssignment>(
                1, 1,
                new UnitRateAssignment("m3", 10m),
                new UnitRateAssignment("m2", 20m));
            var error = Capture<InvalidOperationException>(() => Request(new[] { "L-1" }, source));
            Contains("unit-rate count changed during enumeration", error.Message, "unit-rate overrun diagnostic");
            Equal(2, source.MoveNextCalls, "unit-rate overrun boundary MoveNext");
            Equal(1, source.CurrentReads, "unit-rate overrun must reject before second Current");
        }

        private static void SelectedLineStreamingCeilingRejectsBeforeOverflowCurrent()
        {
            var source = new StreamingProbe<string>(10001, index => "L-" + index.ToString("D5"));
            var error = Capture<InvalidOperationException>(() => Request(source, new[] { new UnitRateAssignment("m3", 10m) }));
            Contains("at most 10000 selected lines", error.Message, "selected-line streaming ceiling diagnostic");
            Equal(10001, source.MoveNextCalls, "selected-line streaming overflow MoveNext");
            Equal(10000, source.CurrentReads, "selected-line streaming must reject before overflow Current");
        }

        private static void UnitRateStreamingCeilingRejectsBeforeOverflowCurrent()
        {
            var source = new StreamingProbe<UnitRateAssignment>(257,
                index => new UnitRateAssignment("u" + index.ToString("D3"), index));
            var error = Capture<InvalidOperationException>(() => Request(new[] { "L-1" }, source));
            Contains("at most 256 unit rates", error.Message, "unit-rate streaming ceiling diagnostic");
            Equal(257, source.MoveNextCalls, "unit-rate streaming overflow MoveNext");
            Equal(256, source.CurrentReads, "unit-rate streaming must reject before overflow Current");
        }

        private static void HonestCommercialCollectionsRemainAccepted()
        {
            var lines = new CountProbeCollection<EstimatingLine>(2, 2, Line("L-2"), Line("L-1"));
            var portfolio = new EstimatingPortfolio(lines);
            Equal(2, portfolio.Lines.Count, "honest portfolio count");
            Equal("L-1", portfolio.Lines[0].LineId, "honest portfolio deterministic order");
            Equal(9, lines.CountReads, "honest portfolio traversal-wide Count evidence including post-Current rebinds");

            var ids = new CountProbeCollection<string>(1, 1, "L-1");
            var rates = new CountProbeCollection<UnitRateAssignment>(1, 1, new UnitRateAssignment("m3", 10m));
            var request = Request(ids, rates);
            Equal(1, request.LineIds.Count, "honest selected-line count");
            Equal(1, request.UnitRates.Count, "honest unit-rate count");
            Equal(6, ids.CountReads, "honest selected-line traversal-wide Count evidence including post-Current rebound");
            Equal(6, rates.CountReads, "honest unit-rate traversal-wide Count evidence including post-Current rebound");
        }

        private static EstimatingLine Line(string id)
        {
            return new EstimatingLine(id, "q-src", "q-rev", 1m, "m3");
        }

        private static BulkRateAssignmentRequest Request(
            IEnumerable<string> ids,
            IEnumerable<UnitRateAssignment> rates)
        {
            return new BulkRateAssignmentRequest(ids, "CC-1", "rate-src", "rate-rev", rates);
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string label)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(label + ": expected diagnostic containing '" + expected + "', actual='" + actual + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountProbeCollection<T> : ICollection<T>
        {
            private readonly int _initialCount;
            private readonly int _postTraversalCount;
            private readonly T[] _items;
            private bool _completed;

            internal CountProbeCollection(int initialCount, int postTraversalCount, params T[] items)
            {
                _initialCount = initialCount;
                _postTraversalCount = postTraversalCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _completed ? _postTraversalCount : _initialCount;
                }
            }

            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return new ProbeEnumerator(this);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly CountProbeCollection<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(CountProbeCollection<T> owner) { _owner = owner; }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index < _owner._items.Length) return true;
                    _owner._completed = true;
                    return false;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;
            private readonly T[] _items;

            internal ConflictingCountCollection(int collectionCount, int readOnlyCount, params T[] items)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
                _items = items;
            }

            int ICollection<T>.Count => _collectionCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            bool ICollection<T>.IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return ((IEnumerable<T>)_items).GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        }

        private sealed class StreamingProbe<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;
            internal StreamingProbe(int count, Func<int, T> factory)
            {
                _count = count;
                _factory = factory;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly StreamingProbe<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(StreamingProbe<T> owner) { _owner = owner; }
                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._factory(_index);
                    }
                }
                object IEnumerator.Current => Current!;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._count;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
