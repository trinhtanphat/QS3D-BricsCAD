using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSnapKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            LineRejectsOverCapBeforeCurrent();
            ArcRejectsOverCapBeforeCurrent();
            LineRejectsTransientGrowthBeforeCurrent();
            ArcRejectsTransientGrowthBeforeCurrent();
            LineRejectsNegativeCountBeforeTraversal();
            ArcRejectsConflictingCountBeforeTraversal();
            StableCountedAndStreamingInputsRemainSupported();
        }

        private static void LineRejectsOverCapBeforeCurrent()
        {
            var source = new HostileCountCollection(2001, 2001, 2001);
            ThrowsInvalidOperation(() => InvokeLine(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void ArcRejectsOverCapBeforeCurrent()
        {
            var source = new HostileCountCollection(2001, 2001, 2001);
            ThrowsInvalidOperation(() => InvokeArc(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void LineRejectsTransientGrowthBeforeCurrent()
        {
            var source = new HostileCountCollection(1, 2, 1);
            ThrowsInvalidOperation(() => InvokeLine(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void ArcRejectsTransientGrowthBeforeCurrent()
        {
            var source = new HostileCountCollection(1, 2, 1);
            ThrowsInvalidOperation(() => InvokeArc(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void LineRejectsNegativeCountBeforeTraversal()
        {
            var source = new HostileCountCollection(-1, -1, -1);
            ThrowsInvalidOperation(() => InvokeLine(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void ArcRejectsConflictingCountBeforeTraversal()
        {
            var source = new HostileCountCollection(1, 1, 2);
            ThrowsInvalidOperation(() => InvokeArc(source));
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void StableCountedAndStreamingInputsRemainSupported()
        {
            var line = GridReferenceCurve.Line("L1", new Point2(0, 0), new Point2(10, 0));
            if (!GridLineSnapPlanner.TryFindNearest(new Point2(5, 1), new[] { line }, 2, out var lineResult))
                throw new Exception("Stable counted LINE snap unexpectedly produced no result.");
            Equal("L1", lineResult!.GridElementId);

            var arc = GridReferenceCurve.Arc("A1", new Point2(0, 0), 10, 0, Math.PI / 2);
            if (!GridArcSnapPlanner.TryFindNearest(new Point2(10, 1), Streaming(arc), 2, out var arcResult))
                throw new Exception("Streaming ARC snap unexpectedly produced no result.");
            Equal("A1", arcResult!.GridElementId);
        }

        private static IEnumerable<GridReferenceCurve> Streaming(GridReferenceCurve value)
        {
            yield return value;
        }

        private static void InvokeLine(IEnumerable<GridReferenceCurve> curves)
        {
            GridLineSnapPlanner.TryFindNearest(new Point2(5, 1), curves, 2, out _);
        }

        private static void InvokeArc(IEnumerable<GridReferenceCurve> curves)
        {
            GridArcSnapPlanner.TryFindNearest(new Point2(10, 1), curves, 2, out _);
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

            throw new Exception("Expected InvalidOperationException before caller-controlled Current.");
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
            private bool _afterMove;

            public HostileCountCollection(int initialCount, int afterMoveCount, int nonGenericCount)
            {
                _initialCount = initialCount;
                _afterMoveCount = afterMoveCount;
                _nonGenericCount = nonGenericCount;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _afterMove ? _afterMoveCount : _initialCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<GridReferenceCurve> GetEnumerator() => new Enumerator(this);
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
                private bool _moved;

                public Enumerator(HostileCountCollection owner)
                {
                    _owner = owner;
                }

                public GridReferenceCurve Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        throw new Exception("Grid snap consumed caller Current before Count/cap admission.");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved) return false;
                    _moved = true;
                    _owner._afterMove = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class GridSnapKnownCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GridSnapKnownCountStabilitySmoke.Run();
    }
}
