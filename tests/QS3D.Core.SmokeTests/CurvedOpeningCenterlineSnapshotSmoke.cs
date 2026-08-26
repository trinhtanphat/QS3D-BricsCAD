using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurvedOpeningCenterlineSnapshotSmoke
    {
        [ModuleInitializer]
        public static void Run()
        {
            RejectsGrowingCardinalityDuringSnapshot();
            RejectsShrinkingCardinalityDuringSnapshot();
            ReadsEachPointExactlyOnce();
        }

        private static void RejectsGrowingCardinalityDuringSnapshot()
        {
            var source = new DriftingPointList(new[] { new Point2(0d, 0d), new Point2(5d, 0d), new Point2(6d, 0d) }, 2, 3);
            Throws<InvalidOperationException>(() => Plan(source));
        }

        private static void RejectsShrinkingCardinalityDuringSnapshot()
        {
            var source = new DriftingPointList(new[] { new Point2(0d, 0d), new Point2(5d, 0d), new Point2(6d, 0d) }, 3, 2);
            Throws<InvalidOperationException>(() => Plan(source));
        }

        private static void ReadsEachPointExactlyOnce()
        {
            var source = new SingleReadPointList(new[] { new Point2(0d, 0d), new Point2(2d, 0d), new Point2(4d, 0d) });
            var plan = Plan(source);
            Near(4d, plan.HostCenterlineLengthM, 1e-12d);
            for (var i = 0; i < source.ReadCounts.Length; i++)
                if (source.ReadCounts[i] != 1)
                    throw new Exception("Expected centerline point " + i + " to be read exactly once, got " + source.ReadCounts[i] + ".");
        }

        private static CurvedOpeningFootprintPlan Plan(IReadOnlyList<Point2> source)
        {
            return CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = source,
                OpeningPoint = new Point2(2d, 0.1d),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d,
                MaximumCenterlineOffsetM = 0.35d
            });
        }

        private sealed class DriftingPointList : IReadOnlyList<Point2>
        {
            private readonly Point2[] _points;
            private readonly int _initialCount;
            private readonly int _finalCount;
            private int _countReads;

            public DriftingPointList(Point2[] points, int initialCount, int finalCount)
            {
                _points = points;
                _initialCount = initialCount;
                _finalCount = finalCount;
            }

            public int Count => ++_countReads == 1 ? _initialCount : _finalCount;
            public Point2 this[int index] => _points[index];
            public IEnumerator<Point2> GetEnumerator() => ((IEnumerable<Point2>)_points).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class SingleReadPointList : IReadOnlyList<Point2>
        {
            private readonly Point2[] _points;
            public int[] ReadCounts { get; }

            public SingleReadPointList(Point2[] points)
            {
                _points = points;
                ReadCounts = new int[points.Length];
            }

            public int Count => _points.Length;
            public Point2 this[int index]
            {
                get
                {
                    ReadCounts[index]++;
                    if (ReadCounts[index] != 1) throw new InvalidOperationException("Centerline point was read more than once.");
                    return _points[index];
                }
            }
            public IEnumerator<Point2> GetEnumerator() => ((IEnumerable<Point2>)_points).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
