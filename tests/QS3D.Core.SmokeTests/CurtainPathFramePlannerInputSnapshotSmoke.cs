using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainPathFramePlannerInputSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectGrowingPathCount();
            RejectShrinkingPathCount();
            RejectGrowingFrameCount();
            SnapshotEachPathPointOnce();
            PreserveStablePlanningSemantics();
        }

        private static void RejectGrowingPathCount()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(1d, 0d) };
            var source = new DriftingCountList<Point2>(points, 2, 8193);
            ExpectInvalid(() => CurtainPathFramePlanner.Length(source), "Growing path Count must fail closed before planning.");
        }

        private static void RejectShrinkingPathCount()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(1d, 0d), new Point2(2d, 0d) };
            var source = new DriftingCountList<Point2>(points, 3, 2);
            ExpectInvalid(() => CurtainPathFramePlanner.Length(source), "Shrinking path Count must fail closed before planning.");
        }

        private static void RejectGrowingFrameCount()
        {
            var path = new[] { new Point2(0d, 0d), new Point2(10d, 0d) };
            var frames = new[] { new CurtainWallRect(1d, 0d, 2d, 3d) };
            var source = new DriftingCountList<CurtainWallRect>(frames, 1, 2);
            ExpectInvalid(() => CurtainPathFramePlanner.Plan(path, source), "Growing frame Count must fail closed before geometry mapping.");
        }

        private static void SnapshotEachPathPointOnce()
        {
            var source = new SingleReadList<Point2>(new[]
            {
                new Point2(0d, 0d),
                new Point2(5d, 0d),
                new Point2(10d, 0d)
            });

            var length = CurtainPathFramePlanner.Length(source);
            AssertNear(length, 10d, 1e-12d, "Single-read path length");
        }

        private static void PreserveStablePlanningSemantics()
        {
            var path = new[]
            {
                new Point2(0d, 0d),
                new Point2(5d, 0d),
                new Point2(10d, 0d)
            };
            var frames = new[] { new CurtainWallRect(2d, 0d, 4d, 3d) };
            var plan = CurtainPathFramePlanner.Plan(path, frames);

            AssertNear(plan.PathLengthM, 10d, 1e-12d, "Stable plan length");
            if (plan.PathSegmentCount != 2) throw new InvalidOperationException("Stable plan must preserve two path segments.");
            if (plan.SourceFrameCount != 1) throw new InvalidOperationException("Stable plan must preserve one source frame.");
            if (plan.Pieces.Count != 2) throw new InvalidOperationException("Frame spanning a path vertex must split into two deterministic pieces.");
            AssertNear(plan.Pieces[0].WidthM, 3d, 1e-12d, "First split width");
            AssertNear(plan.Pieces[1].WidthM, 1d, 1e-12d, "Second split width");

            var projection = CurtainPathFramePlanner.ProjectPoint(path, new Point2(7d, 2d));
            AssertNear(projection.StationM, 7d, 1e-12d, "Stable projection station");
            AssertNear(projection.DistanceM, 2d, 1e-12d, "Stable projection distance");
        }

        private static void ExpectInvalid(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void AssertNear(double actual, double expected, double tolerance, string label)
        {
            if (Math.Abs(actual - expected) > tolerance)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + ", actual " + actual + ".");
        }

        private sealed class DriftingCountList<T> : IReadOnlyList<T>
        {
            private readonly T[] _items;
            private readonly int _firstCount;
            private readonly int _laterCount;
            private int _countReads;

            public DriftingCountList(T[] items, int firstCount, int laterCount)
            {
                _items = items;
                _firstCount = firstCount;
                _laterCount = laterCount;
            }

            public int Count => _countReads++ == 0 ? _firstCount : _laterCount;

            public T this[int index]
            {
                get
                {
                    if (index < 0 || index >= _items.Length) throw new ArgumentOutOfRangeException(nameof(index));
                    return _items[index];
                }
            }

            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class SingleReadList<T> : IReadOnlyList<T>
        {
            private readonly T[] _items;
            private readonly int[] _reads;

            public SingleReadList(T[] items)
            {
                _items = items;
                _reads = new int[items.Length];
            }

            public int Count => _items.Length;

            public T this[int index]
            {
                get
                {
                    if (_reads[index]++ != 0)
                        throw new InvalidOperationException("Planner re-read a caller-controlled path value instead of using its snapshot.");
                    return _items[index];
                }
            }

            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
