using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonRegionHoleSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            UsesInitialHoleReferenceWhenSameCountSourceReplacesItem();
            RejectsHoleCollectionGrowthDuringSnapshot();
            RejectsHoleCollectionShrinkDuringSnapshot();
            RejectsOversizedHoleCollectionBeforeIndexing();
            PreservesStableRegionAndClipSemantics();
        }

        private static void UsesInitialHoleReferenceWhenSameCountSourceReplacesItem()
        {
            var initial = Rectangle(2d, 2d, 4d, 4d);
            var replacement = Rectangle(6d, 6d, 8d, 8d);
            var holes = new SameCountReplacingList(initial, replacement);

            var region = PolygonRegionScanlineClipper.NormalizeAndValidate(Outer(), holes);
            if (region.Holes.Count != 1)
                throw new InvalidOperationException("Polygon region snapshot must preserve the initial single-hole cardinality.");
            if (region.Holes[0][0].X != 2d || region.Holes[0][0].Y != 2d)
                throw new InvalidOperationException("Polygon region snapshot must validate the initially captured hole, not a same-count replacement observed later.");
            if (holes.IndexReads != 1)
                throw new InvalidOperationException("Polygon region snapshot must read each source hole reference exactly once.");
        }

        private static void RejectsHoleCollectionGrowthDuringSnapshot()
        {
            var holes = new MutatingHoleList(
                new[] { Rectangle(2d, 2d, 4d, 4d) },
                0,
                list => list.Add(Rectangle(6d, 6d, 8d, 8d)));

            ThrowsInvalidOperation(
                () => PolygonRegionScanlineClipper.NormalizeAndValidate(Outer(), holes),
                "growing hole collection");
        }

        private static void RejectsHoleCollectionShrinkDuringSnapshot()
        {
            var holes = new MutatingHoleList(
                new[] { Rectangle(2d, 2d, 4d, 4d), Rectangle(6d, 6d, 8d, 8d) },
                0,
                list => list.RemoveAt(list.Count - 1));

            ThrowsInvalidOperation(
                () => PolygonRegionScanlineClipper.NormalizeAndValidate(Outer(), holes),
                "shrinking hole collection");
        }

        private static void RejectsOversizedHoleCollectionBeforeIndexing()
        {
            var holes = new OversizedHoleList();
            ThrowsArgument(
                () => PolygonRegionScanlineClipper.NormalizeAndValidate(Outer(), holes),
                "oversized hole collection");
            if (holes.IndexRead)
                throw new InvalidOperationException("Polygon region snapshot must reject an oversized hole collection before reading any hole index.");
        }

        private static void PreservesStableRegionAndClipSemantics()
        {
            var region = PolygonRegionScanlineClipper.NormalizeAndValidate(
                Outer(),
                new[] { Rectangle(2d, 2d, 4d, 4d), Rectangle(6d, 6d, 8d, 8d) });

            if (region.Holes.Count != 2 || region.BoundaryLoops.Count != 3)
                throw new InvalidOperationException("Stable polygon region topology changed after hole snapshot hardening.");

            var segments = PolygonRegionScanlineClipper.Clip(region, PolygonScanAxis.Horizontal, 3d);
            if (segments.Count != 2)
                throw new InvalidOperationException("Expected the stable horizontal scanline to be split by one hole.");
            Exact(0d, segments[0].Start.X, "left segment start");
            Exact(2d, segments[0].End.X, "left segment end");
            Exact(4d, segments[1].Start.X, "right segment start");
            Exact(10d, segments[1].End.X, "right segment end");
        }

        private static IReadOnlyList<Point2> Outer() => Rectangle(0d, 0d, 10d, 10d);

        private static IReadOnlyList<Point2> Rectangle(double minX, double minY, double maxX, double maxY) =>
            new[]
            {
                new Point2(minX, minY),
                new Point2(maxX, minY),
                new Point2(maxX, maxY),
                new Point2(minX, maxY)
            };

        private static void ThrowsInvalidOperation(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Polygon region hole snapshot must fail closed for " + scenario + ".");
        }

        private static void ThrowsArgument(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Polygon region hole snapshot must reject " + scenario + ".");
        }

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected polygon region coordinate for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }

        private sealed class SameCountReplacingList : IReadOnlyList<IReadOnlyList<Point2>>
        {
            private IReadOnlyList<Point2> _current;
            private readonly IReadOnlyList<Point2> _replacement;
            private int _countReads;

            public SameCountReplacingList(IReadOnlyList<Point2> initial, IReadOnlyList<Point2> replacement)
            {
                _current = initial;
                _replacement = replacement;
            }

            public int IndexReads { get; private set; }

            public int Count
            {
                get
                {
                    _countReads++;
                    if (_countReads == 2) _current = _replacement;
                    return 1;
                }
            }

            public IReadOnlyList<Point2> this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    IndexReads++;
                    return _current;
                }
            }

            public IEnumerator<IReadOnlyList<Point2>> GetEnumerator()
            {
                yield return _current;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MutatingHoleList : IReadOnlyList<IReadOnlyList<Point2>>
        {
            private readonly List<IReadOnlyList<Point2>> _holes;
            private readonly int _triggerIndex;
            private readonly Action<List<IReadOnlyList<Point2>>> _mutation;
            private bool _mutated;

            public MutatingHoleList(
                IEnumerable<IReadOnlyList<Point2>> holes,
                int triggerIndex,
                Action<List<IReadOnlyList<Point2>>> mutation)
            {
                _holes = new List<IReadOnlyList<Point2>>(holes);
                _triggerIndex = triggerIndex;
                _mutation = mutation;
            }

            public int Count => _holes.Count;

            public IReadOnlyList<Point2> this[int index]
            {
                get
                {
                    var value = _holes[index];
                    if (!_mutated && index == _triggerIndex)
                    {
                        _mutated = true;
                        _mutation(_holes);
                    }
                    return value;
                }
            }

            public IEnumerator<IReadOnlyList<Point2>> GetEnumerator() => _holes.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class OversizedHoleList : IReadOnlyList<IReadOnlyList<Point2>>
        {
            public bool IndexRead { get; private set; }
            public int Count => 257;

            public IReadOnlyList<Point2> this[int index]
            {
                get
                {
                    IndexRead = true;
                    throw new InvalidOperationException("Oversized hole collection index must not be read.");
                }
            }

            public IEnumerator<IReadOnlyList<Point2>> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
