using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSnapCurrentCountStabilitySmoke
    {
        internal static void Run()
        {
            LineRebindsKnownCountImmediatelyAfterCurrent();
            ArcRebindsKnownCountImmediatelyAfterCurrent();
        }

        private static void LineRebindsKnownCountImmediatelyAfterCurrent()
        {
            var curve = GridReferenceCurve.Line("L-CURRENT", new Point2(0, 0), new Point2(10, 0));
            var source = new ObservedReadOnlyCollection(curve);

            if (!GridLineSnapPlanner.TryFindNearest(new Point2(5, 1), source, 2, out var result))
                throw new Exception("Stable counted LINE snap unexpectedly produced no result.");

            Equal("L-CURRENT", result!.GridElementId);
            Equal(1, source.CurrentReads);
            Equal(2, source.MoveNextCalls);
            Equal(7, source.CountReads);
        }

        private static void ArcRebindsKnownCountImmediatelyAfterCurrent()
        {
            var curve = GridReferenceCurve.Arc("A-CURRENT", new Point2(0, 0), 10, 0, Math.PI / 2);
            var source = new ObservedReadOnlyCollection(curve);

            if (!GridArcSnapPlanner.TryFindNearest(new Point2(10, 1), source, 2, out var result))
                throw new Exception("Stable counted ARC snap unexpectedly produced no result.");

            Equal("A-CURRENT", result!.GridElementId);
            Equal(1, source.CurrentReads);
            Equal(2, source.MoveNextCalls);
            Equal(7, source.CountReads);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private sealed class ObservedReadOnlyCollection : IReadOnlyCollection<GridReferenceCurve>
        {
            private readonly GridReferenceCurve _curve;

            internal ObservedReadOnlyCollection(GridReferenceCurve curve)
            {
                _curve = curve;
            }

            internal int CountReads { get; private set; }
            internal int CurrentReads { get; private set; }
            internal int MoveNextCalls { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return 1;
                }
            }

            public IEnumerator<GridReferenceCurve> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<GridReferenceCurve>
            {
                private readonly ObservedReadOnlyCollection _owner;
                private bool _moved;

                internal Enumerator(ObservedReadOnlyCollection owner)
                {
                    _owner = owner;
                }

                public GridReferenceCurve Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._curve;
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
    }

    internal static class GridSnapCurrentCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GridSnapCurrentCountStabilitySmoke.Run();
    }
}
