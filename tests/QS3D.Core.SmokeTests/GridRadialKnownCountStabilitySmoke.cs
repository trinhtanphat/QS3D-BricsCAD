using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridRadialKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            RejectsOverCapBeforeTraversal();
            RejectsNegativeCountBeforeTraversal();
            RejectsConflictingCountBeforeTraversal();
            RejectsTransientGrowthBeforeCurrent();
            RejectsTransientShrinkBeforeCurrent();
            RejectsKnownCountOverrunBeforeSecondCurrent();
            RejectsKnownCountUnderYield();
            StableCountedAndStreamingOrderingRemainSupported();
        }

        private static void RejectsOverCapBeforeTraversal()
        {
            var source = new HostileCountCollection(2001, 2001, 2001, 1);
            ThrowsInvalidOperation(() => Invoke(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void RejectsNegativeCountBeforeTraversal()
        {
            var source = new HostileCountCollection(-1, -1, -1, 1);
            ThrowsInvalidOperation(() => Invoke(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void RejectsConflictingCountBeforeTraversal()
        {
            var source = new HostileCountCollection(1, 1, 2, 1);
            ThrowsInvalidOperation(() => Invoke(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void RejectsTransientGrowthBeforeCurrent()
        {
            var source = new HostileCountCollection(1, 2, 1, 1);
            ThrowsInvalidOperation(() => Invoke(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void RejectsTransientShrinkBeforeCurrent()
        {
            var source = new HostileCountCollection(2, 1, 2, 1);
            ThrowsInvalidOperation(() => Invoke(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void RejectsKnownCountOverrunBeforeSecondCurrent()
        {
            var source = new StableCountCollection(1, 2);
            ThrowsInvalidOperation(() => Invoke(source));
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void RejectsKnownCountUnderYield()
        {
            var source = new StableCountCollection(2, 1);
            ThrowsInvalidOperation(() => Invoke(source));
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void StableCountedAndStreamingOrderingRemainSupported()
        {
            var inner = GridReferenceCurve.Arc("R1", new Point2(0, 0), 5, 0, Math.PI / 2);
            var outer = GridReferenceCurve.Arc("R2", new Point2(0, 0), 10, 0, Math.PI / 2);

            var ascending = GridRadialOrderingPlanner.OrderConcentricArcs(new[] { outer, inner });
            Equal(2, ascending.Count);
            Equal("R1", ascending[0].ElementId);
            Equal("R2", ascending[1].ElementId);

            var descending = GridRadialOrderingPlanner.OrderConcentricArcs(Streaming(inner, outer), descending: true);
            Equal(2, descending.Count);
            Equal("R2", descending[0].ElementId);
            Equal("R1", descending[1].ElementId);
        }

        private static IEnumerable<GridReferenceCurve> Streaming(params GridReferenceCurve[] values)
        {
            foreach (var value in values)
                yield return value;
        }

        private static void Invoke(IEnumerable<GridReferenceCurve> curves)
        {
            GridRadialOrderingPlanner.OrderConcentricArcs(curves);
        }

        private static void ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Expected InvalidOperationException from radial known-Count contract.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private sealed class HostileCountCollection : ICollection<GridReferenceCurve>, IReadOnlyCollection<GridReferenceCurve>, ICollection
        {
            private readonly int _initialCount;
            private readonly int _afterMoveCount;
            private readonly int _nonGenericCount;
            private readonly int _yieldCount;
            private bool _afterMove;

            internal HostileCountCollection(int initialCount, int afterMoveCount, int nonGenericCount, int yieldCount)
            {
                _initialCount = initialCount;
                _afterMoveCount = afterMoveCount;
                _nonGenericCount = nonGenericCount;
                _yieldCount = yieldCount;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _afterMove ? _afterMoveCount : _initialCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<GridReferenceCurve> GetEnumerator() => new Enumerator(this, _yieldCount);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(GridReferenceCurve item) => false;
            public void CopyTo(GridReferenceCurve[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(GridReferenceCurve item) => throw new NotSupportedException();
            public bool Remove(GridReferenceCurve item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<GridReferenceCurve>
            {
                private readonly HostileCountCollection _owner;
                private readonly int _yieldCount;
                private int _index = -1;

                internal Enumerator(HostileCountCollection owner, int yieldCount)
                {
                    _owner = owner;
                    _yieldCount = yieldCount;
                }

                public GridReferenceCurve Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        throw new Exception("Radial ordering consumed caller Current before Count revalidation.");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _owner._afterMove = true;
                    _index++;
                    return _index < _yieldCount;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StableCountCollection : ICollection<GridReferenceCurve>, IReadOnlyCollection<GridReferenceCurve>
        {
            private readonly int _reportedCount;
            private readonly int _yieldCount;

            internal StableCountCollection(int reportedCount, int yieldCount)
            {
                _reportedCount = reportedCount;
                _yieldCount = yieldCount;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _reportedCount;
            public bool IsReadOnly => true;
            public IEnumerator<GridReferenceCurve> GetEnumerator() => new Enumerator(this, _yieldCount);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(GridReferenceCurve item) => false;
            public void CopyTo(GridReferenceCurve[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(GridReferenceCurve item) => throw new NotSupportedException();
            public bool Remove(GridReferenceCurve item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<GridReferenceCurve>
            {
                private readonly StableCountCollection _owner;
                private readonly int _yieldCount;
                private int _index = -1;

                internal Enumerator(StableCountCollection owner, int yieldCount)
                {
                    _owner = owner;
                    _yieldCount = yieldCount;
                }

                public GridReferenceCurve Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return GridReferenceCurve.Arc("R" + (_index + 1), new Point2(0, 0), 5 + _index * 5, 0, Math.PI / 2);
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _yieldCount;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class GridRadialKnownCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GridRadialKnownCountStabilitySmoke.Run();
    }
}
