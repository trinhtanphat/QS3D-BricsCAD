using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class DuplicateDetectionCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CandidateGrowthRejectsBeforeSecondAdvance();
            CandidateShrinkRejectsBeforeCurrent();
            CandidateTerminalReboundRejects();
            ElementProjectionPreservesKnownCountBoundary();
            StableKnownCountPreservesDuplicateSemantics();
            PureStreamingOverloadsRemainSupported();
        }

        private static void CandidateGrowthRejectsBeforeSecondAdvance()
        {
            var source = HostileKnownCountEnumerable<DuplicateCandidate>.WithCounts(
                new[] { Candidate("B", "shared"), Candidate("A", "shared") }, 2, 2, 2, 2, 3);
            ExpectCountDrift(() => new DuplicateDetectionService().Detect(source), "candidate growth");
            Equal(1, source.MoveNextCalls, "candidate growth MoveNext");
            Equal(1, source.CurrentReads, "candidate growth Current");
        }

        private static void CandidateShrinkRejectsBeforeCurrent()
        {
            var source = HostileKnownCountEnumerable<DuplicateCandidate>.WithCounts(
                new[] { Candidate("B", "shared"), Candidate("A", "shared") }, 2, 2, 1);
            ExpectCountDrift(() => new DuplicateDetectionService().Detect(source), "candidate shrink");
            Equal(1, source.MoveNextCalls, "candidate shrink MoveNext");
            Equal(0, source.CurrentReads, "candidate shrink Current");
        }

        private static void CandidateTerminalReboundRejects()
        {
            var source = HostileKnownCountEnumerable<DuplicateCandidate>.WithCounts(
                new[] { Candidate("B", "shared"), Candidate("A", "shared") }, 2, 2, 2, 2, 2, 2, 2, 2, 3);
            ExpectCountDrift(() => new DuplicateDetectionService().Detect(source), "candidate terminal rebound");
            Equal(3, source.MoveNextCalls, "candidate terminal rebound MoveNext");
            Equal(2, source.CurrentReads, "candidate terminal rebound Current");
        }

        private static void ElementProjectionPreservesKnownCountBoundary()
        {
            var growth = HostileKnownCountEnumerable<CoordinationElement>.WithCounts(
                new[] { Element("B"), Element("A") }, 2, 2, 2, 2, 3);
            ExpectCountDrift(() => new DuplicateDetectionService().Detect(growth), "element growth");
            Equal(1, growth.MoveNextCalls, "element growth MoveNext");
            Equal(1, growth.CurrentReads, "element growth Current");

            var shrink = HostileKnownCountEnumerable<CoordinationElement>.WithCounts(
                new[] { Element("B"), Element("A") }, 2, 2, 1);
            ExpectCountDrift(() => new DuplicateDetectionService().Detect(shrink), "element shrink");
            Equal(0, shrink.CurrentReads, "element shrink Current");

            var rebound = HostileKnownCountEnumerable<CoordinationElement>.WithCounts(
                new[] { Element("B"), Element("A") }, 2, 2, 2, 2, 2, 2, 2, 2, 3);
            ExpectCountDrift(() => new DuplicateDetectionService().Detect(rebound), "element terminal rebound");
            Equal(3, rebound.MoveNextCalls, "element terminal rebound MoveNext");
        }

        private static void StableKnownCountPreservesDuplicateSemantics()
        {
            var source = HostileKnownCountEnumerable<DuplicateCandidate>.WithCounts(
                new[] { Candidate("B", "shared"), Candidate("A", "shared") }, 2, 2, 2, 2, 2, 2, 2, 2, 2);
            var result = new DuplicateDetectionService().Detect(source);
            Equal(1, result.Pairs.Count, "stable pair count");
            Equal("A", result.Pairs[0].LeftElementId, "stable left id");
            Equal("B", result.Pairs[0].RightElementId, "stable right id");
            Equal(true, result.Pairs[0].IsExactGeometry, "stable exact geometry");
            Equal(true, result.Pairs[0].IsSemanticIdentity, "stable semantic identity");
            Equal(9, source.CountReads, "stable Count reads");
        }

        private static void PureStreamingOverloadsRemainSupported()
        {
            var candidates = new PureStreamingEnumerable<DuplicateCandidate>(Candidate("B", "shared"), Candidate("A", "shared"));
            var candidateResult = new DuplicateDetectionService().Detect(candidates);
            Equal(1, candidateResult.Pairs.Count, "streaming candidate pair count");
            Equal(3, candidates.MoveNextCalls, "streaming candidate MoveNext");
            Equal(2, candidates.CurrentReads, "streaming candidate Current");

            var elements = new PureStreamingEnumerable<CoordinationElement>(Element("B"), Element("A"));
            var elementResult = new DuplicateDetectionService().Detect(elements);
            Equal(1, elementResult.Pairs.Count, "streaming element pair count");
            Equal("A", elementResult.Pairs[0].LeftElementId, "streaming element sorted left");
            Equal(3, elements.MoveNextCalls, "streaming element MoveNext");
            Equal(2, elements.CurrentReads, "streaming element Current");
        }

        private static DuplicateCandidate Candidate(string id, string sourceId) => new DuplicateCandidate(Element(id), sourceId);

        private static CoordinationElement Element(string id) => new CoordinationElement(
            id, "Structure", "Beam", "S", "R1", new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d));

        private static void ExpectCountDrift(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains("known element Count changed during snapshot", StringComparison.Ordinal))
                    throw new Exception(label + " wrong InvalidOperationException: " + ex.Message);
                return;
            }
            throw new Exception(label + " expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class HostileKnownCountEnumerable<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int[] _counts;
            private int _countIndex;

            private HostileKnownCountEnumerable(T[] items, int[] counts)
            {
                _items = items;
                _counts = counts;
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            internal static HostileKnownCountEnumerable<T> WithCounts(T[] items, params int[] counts) =>
                new HostileKnownCountEnumerable<T>(items, counts);

            public int Count
            {
                get
                {
                    CountReads++;
                    var index = _countIndex++;
                    return index < _counts.Length ? _counts[index] : _counts[_counts.Length - 1];
                }
            }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly HostileKnownCountEnumerable<T> _owner;
                private int _index = -1;

                internal Enumerator(HostileKnownCountEnumerable<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _owner._items.Length) throw new InvalidOperationException("Current outside range.");
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class PureStreamingEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;

            internal PureStreamingEnumerable(params T[] items) => _items = items;

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly PureStreamingEnumerable<T> _owner;
                private int _index = -1;

                internal Enumerator(PureStreamingEnumerable<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _owner._items.Length) throw new InvalidOperationException();
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
