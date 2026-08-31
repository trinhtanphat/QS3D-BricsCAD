using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonSourceLoopRegionAssemblerSmoke
    {
        public static void Run()
        {
            DisconnectedOutersProduceStableRegions();
            HoleIsAssignedToContainingOuter();
            SelectionOrderDoesNotChangeRegionIdentity();
            DuplicateSourceIdsFailClosed();
            DeeperNestingFailsClosed();
            TouchingLoopsFailClosed();
            CrossingLoopsFailClosed();
            KnownCountOverrunStopsBeforeUnexpectedCurrent();
            TransientMoveNextCountDriftFailsClosed();
            TransientCurrentCountDriftFailsClosed();
            KnownCountUnderYieldFailsClosed();
            StableCountedSourceStillAssembles();
            PureStreamingSourceStillAssembles();
        }

        private static void DisconnectedOutersProduceStableRegions()
        {
            var result = PolygonSourceLoopRegionAssembler.Assemble(new[]
            {
                Loop("B2", Square(20, 0, 30, 10)),
                Loop("A1", Square(0, 0, 10, 10))
            });
            Equal(2, result.Regions.Count);
            Equal("A1", result.Regions[0].RegionId);
            Equal("B2", result.Regions[1].RegionId);
            Equal(2, result.RegionSet.Islands.Count);
            Equal("A1", result.RegionSet.Islands[0].RegionId);
            Equal("B2", result.RegionSet.Islands[1].RegionId);
        }

        private static void HoleIsAssignedToContainingOuter()
        {
            var result = PolygonSourceLoopRegionAssembler.Assemble(new[]
            {
                Loop("OUTER-A", Square(0, 0, 10, 10)),
                Loop("HOLE-A", Square(3, 3, 7, 7))
            });
            Equal(1, result.Regions.Count);
            Equal("OUTER-A", result.Regions[0].RegionId);
            Equal("OUTER-A", result.Regions[0].OuterSourceId);
            Equal(1, result.Regions[0].HoleSourceIds.Count);
            Equal("HOLE-A", result.Regions[0].HoleSourceIds[0]);
            Equal(1, result.RegionSet.Islands[0].Region.Holes.Count);
        }

        private static void SelectionOrderDoesNotChangeRegionIdentity()
        {
            var forward = PolygonSourceLoopRegionAssembler.Assemble(new[]
            {
                Loop("aa10", Square(0, 0, 10, 10)),
                Loop("aa11", Square(2, 2, 4, 4)),
                Loop("bb20", Square(20, 0, 30, 10))
            });
            var reverse = PolygonSourceLoopRegionAssembler.Assemble(new[]
            {
                Loop("bb20", Square(20, 0, 30, 10)),
                Loop("aa11", Square(2, 2, 4, 4)),
                Loop("aa10", Square(0, 0, 10, 10))
            });
            Equal(string.Join("|", forward.Regions.Select(x => x.RegionId + ":" + string.Join(",", x.HoleSourceIds))), string.Join("|", reverse.Regions.Select(x => x.RegionId + ":" + string.Join(",", x.HoleSourceIds))));
            Equal("AA10", forward.Regions[0].RegionId);
            Equal("BB20", forward.Regions[1].RegionId);
        }

        private static void DuplicateSourceIdsFailClosed() => Throws<ArgumentException>(() => PolygonSourceLoopRegionAssembler.Assemble(new[]
        {
            Loop("ab12", Square(0, 0, 10, 10)),
            Loop(" AB12 ", Square(20, 0, 30, 10))
        }));

        private static void DeeperNestingFailsClosed() => Throws<ArgumentException>(() => PolygonSourceLoopRegionAssembler.Assemble(new[]
        {
            Loop("A", Square(0, 0, 12, 12)),
            Loop("B", Square(2, 2, 10, 10)),
            Loop("C", Square(4, 4, 8, 8))
        }));

        private static void TouchingLoopsFailClosed() => Throws<ArgumentException>(() => PolygonSourceLoopRegionAssembler.Assemble(new[]
        {
            Loop("A", Square(0, 0, 10, 10)),
            Loop("B", Square(10, 2, 14, 6))
        }));

        private static void CrossingLoopsFailClosed() => Throws<ArgumentException>(() => PolygonSourceLoopRegionAssembler.Assemble(new[]
        {
            Loop("A", Square(0, 0, 10, 10)),
            Loop("B", Square(8, 2, 14, 6))
        }));

        private static void KnownCountOverrunStopsBeforeUnexpectedCurrent()
        {
            var source = new HostileCountSequence(Loop("A", Square(0, 0, 10, 10)), 0, DriftBoundary.None, true);
            Throws<ArgumentException>(() => PolygonSourceLoopRegionAssembler.Assemble(source));
            Equal(0, source.CurrentReads);
        }

        private static void TransientMoveNextCountDriftFailsClosed()
        {
            var source = new HostileCountSequence(Loop("A", Square(0, 0, 10, 10)), 1, DriftBoundary.MoveNext, true);
            Throws<ArgumentException>(() => PolygonSourceLoopRegionAssembler.Assemble(source));
            Equal(0, source.CurrentReads);
        }

        private static void TransientCurrentCountDriftFailsClosed()
        {
            var source = new HostileCountSequence(Loop("A", Square(0, 0, 10, 10)), 1, DriftBoundary.Current, true);
            Throws<ArgumentException>(() => PolygonSourceLoopRegionAssembler.Assemble(source));
            Equal(1, source.CurrentReads);
        }

        private static void KnownCountUnderYieldFailsClosed()
        {
            var source = new HostileCountSequence(Loop("A", Square(0, 0, 10, 10)), 2, DriftBoundary.None, true);
            Throws<ArgumentException>(() => PolygonSourceLoopRegionAssembler.Assemble(source));
            Equal(1, source.CurrentReads);
        }

        private static void StableCountedSourceStillAssembles()
        {
            var source = new HostileCountSequence(Loop("A", Square(0, 0, 10, 10)), 1, DriftBoundary.None, true);
            var result = PolygonSourceLoopRegionAssembler.Assemble(source);
            Equal(1, result.Regions.Count);
            Equal("A", result.Regions[0].RegionId);
            Equal(1, source.CurrentReads);
        }

        private static void PureStreamingSourceStillAssembles()
        {
            var result = PolygonSourceLoopRegionAssembler.Assemble(Stream(Loop("A", Square(0, 0, 10, 10))));
            Equal(1, result.Regions.Count);
            Equal("A", result.Regions[0].RegionId);
        }

        private static IEnumerable<PolygonSourceLoop2> Stream(PolygonSourceLoop2 loop)
        {
            yield return loop;
        }

        private enum DriftBoundary { None, MoveNext, Current }

        private sealed class HostileCountSequence : IEnumerable<PolygonSourceLoop2>, IReadOnlyCollection<PolygonSourceLoop2>
        {
            private readonly PolygonSourceLoop2 _item;
            private readonly int _declaredCount;
            private readonly DriftBoundary _driftBoundary;
            private readonly bool _yieldItem;
            private int _driftReads;

            public HostileCountSequence(PolygonSourceLoop2 item, int declaredCount, DriftBoundary driftBoundary, bool yieldItem)
            {
                _item = item;
                _declaredCount = declaredCount;
                _driftBoundary = driftBoundary;
                _yieldItem = yieldItem;
            }

            public int CurrentReads { get; private set; }
            public int Count
            {
                get
                {
                    if (_driftReads > 0)
                    {
                        _driftReads--;
                        return _declaredCount + 1;
                    }
                    return _declaredCount;
                }
            }

            public IEnumerator<PolygonSourceLoop2> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private void DriftOnce() => _driftReads = 1;

            private sealed class Enumerator : IEnumerator<PolygonSourceLoop2>
            {
                private readonly HostileCountSequence _owner;
                private bool _advanced;
                private bool _hasCurrent;

                public Enumerator(HostileCountSequence owner) => _owner = owner;

                public PolygonSourceLoop2 Current
                {
                    get
                    {
                        if (!_hasCurrent) throw new InvalidOperationException();
                        _owner.CurrentReads++;
                        if (_owner._driftBoundary == DriftBoundary.Current) _owner.DriftOnce();
                        return _owner._item;
                    }
                }
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_advanced)
                    {
                        _hasCurrent = false;
                        return false;
                    }
                    _advanced = true;
                    _hasCurrent = _owner._yieldItem;
                    if (_hasCurrent && _owner._driftBoundary == DriftBoundary.MoveNext) _owner.DriftOnce();
                    return _hasCurrent;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static PolygonSourceLoop2 Loop(string sourceId, IReadOnlyList<Point2> vertices) => new PolygonSourceLoop2(sourceId, vertices);
        private static Point2[] Square(double minX, double minY, double maxX, double maxY) => new[]
        {
            new Point2(minX, minY), new Point2(maxX, minY), new Point2(maxX, maxY), new Point2(minX, maxY)
        };

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
