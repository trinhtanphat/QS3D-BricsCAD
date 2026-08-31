using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class ClashDetectionCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GrowthRejectsBeforeSecondAdvance();
            ShrinkRejectsBeforeFirstCurrentRead();
            CurrentCountDriftWinsBeforeNullValidation();
            FinalReboundRejectsAfterEnumerationEnds();
            StableKnownCountProducesExpectedClash();
            PureStreamingEnumerableRemainsSupported();
        }

        private static void GrowthRejectsBeforeSecondAdvance()
        {
            var source = HostileKnownCountEnumerable.WithCounts(2, 2, 2, 3);
            ExpectCountDrift(() => new ClashDetectionService().Detect(source), "growth");
            Equal(1, source.MoveNextCalls, "growth MoveNext calls");
            Equal(1, source.CurrentReads, "growth Current reads");
        }

        private static void ShrinkRejectsBeforeFirstCurrentRead()
        {
            var source = HostileKnownCountEnumerable.WithCounts(2, 2, 1);
            ExpectCountDrift(() => new ClashDetectionService().Detect(source), "shrink");
            Equal(1, source.MoveNextCalls, "shrink MoveNext calls");
            Equal(0, source.CurrentReads, "shrink Current reads");
        }

        private static void CurrentCountDriftWinsBeforeNullValidation()
        {
            var source = new CurrentCountDriftEnumerable();
            ExpectCountDrift(() => new ClashDetectionService().Detect(source), "Current-induced drift");
            Equal(1, source.MoveNextCalls, "Current-induced drift MoveNext calls");
            Equal(1, source.CurrentReads, "Current-induced drift Current reads");
            Equal(4, source.CountReads, "Current-induced drift Count reads");
        }

        private static void FinalReboundRejectsAfterEnumerationEnds()
        {
            var source = HostileKnownCountEnumerable.WithCounts(2, 2, 2, 2, 2, 2, 2, 2, 3);
            ExpectCountDrift(() => new ClashDetectionService().Detect(source), "final rebound");
            Equal(3, source.MoveNextCalls, "final rebound MoveNext calls");
            Equal(2, source.CurrentReads, "final rebound Current reads");
        }

        private static void StableKnownCountProducesExpectedClash()
        {
            var source = HostileKnownCountEnumerable.WithCounts(2, 2, 2, 2, 2, 2, 2, 2, 2);
            var results = new ClashDetectionService().Detect(source, includeSameDiscipline: true);
            Equal(1, results.Count, "stable result count");
            Equal(ClashKind.Hard, results[0].Kind, "stable clash kind");
            Equal("A", results[0].LeftElementId, "stable left id");
            Equal("B", results[0].RightElementId, "stable right id");
            Equal(3, source.MoveNextCalls, "stable MoveNext calls");
            Equal(2, source.CurrentReads, "stable Current reads");
            Equal(9, source.CountReads, "stable Count reads");
        }

        private static void PureStreamingEnumerableRemainsSupported()
        {
            var source = new PureStreamingEnumerable(Element("B"), Element("A"));
            var results = new ClashDetectionService().Detect(source, includeSameDiscipline: true);
            Equal(1, results.Count, "streaming result count");
            Equal("A", results[0].LeftElementId, "streaming sorted left id");
            Equal("B", results[0].RightElementId, "streaming sorted right id");
            Equal(3, source.MoveNextCalls, "streaming MoveNext calls");
            Equal(2, source.CurrentReads, "streaming Current reads");
        }

        private static CoordinationElement Element(string id)
        {
            return new CoordinationElement(
                id,
                "Structure",
                "Beam",
                "S",
                "R1",
                new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d));
        }

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

        private sealed class CurrentCountDriftEnumerable : IReadOnlyCollection<CoordinationElement>
        {
            private int _count = 1;

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _count;
                }
            }

            public IEnumerator<CoordinationElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<CoordinationElement>
            {
                private readonly CurrentCountDriftEnumerable _owner;
                private bool _moved;

                internal Enumerator(CurrentCountDriftEnumerable owner) => _owner = owner;

                public CoordinationElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._count = 2;
                        return null!;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class HostileKnownCountEnumerable : IReadOnlyCollection<CoordinationElement>
        {
            private readonly int[] _counts;
            private readonly CoordinationElement[] _elements = { Element("B"), Element("A") };
            private int _countIndex;

            private HostileKnownCountEnumerable(int[] counts) => _counts = counts;

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            internal static HostileKnownCountEnumerable WithCounts(params int[] counts) =>
                new HostileKnownCountEnumerable(counts);

            public int Count
            {
                get
                {
                    CountReads++;
                    var index = _countIndex++;
                    return index < _counts.Length ? _counts[index] : _counts[_counts.Length - 1];
                }
            }

            public IEnumerator<CoordinationElement> GetEnumerator() => new Enumerator(this, _elements);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<CoordinationElement>
            {
                private readonly HostileKnownCountEnumerable _owner;
                private readonly CoordinationElement[] _elements;
                private int _index = -1;

                internal Enumerator(HostileKnownCountEnumerable owner, CoordinationElement[] elements)
                {
                    _owner = owner;
                    _elements = elements;
                }

                public CoordinationElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _elements.Length) throw new InvalidOperationException("Current outside element range.");
                        return _elements[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _elements.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class PureStreamingEnumerable : IEnumerable<CoordinationElement>
        {
            private readonly CoordinationElement[] _elements;

            internal PureStreamingEnumerable(params CoordinationElement[] elements) => _elements = elements;

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<CoordinationElement> GetEnumerator() => new Enumerator(this, _elements);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<CoordinationElement>
            {
                private readonly PureStreamingEnumerable _owner;
                private readonly CoordinationElement[] _elements;
                private int _index = -1;

                internal Enumerator(PureStreamingEnumerable owner, CoordinationElement[] elements)
                {
                    _owner = owner;
                    _elements = elements;
                }

                public CoordinationElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _elements.Length) throw new InvalidOperationException();
                        return _elements[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _elements.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
