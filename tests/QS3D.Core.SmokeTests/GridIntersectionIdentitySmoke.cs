using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionIdentitySmoke
    {
        public static void Run()
        {
            PairOrderAndInputOrderAreStable();
            CaseAndWhitespaceCanonicalize();
            NearDuplicatePointFailsClosed();
            MoreThanTwoPointsPerPairFailsClosed();
            SameGridPairFailsClosed();
            OwnerTokenIsCompact();
        }

        private static void PairOrderAndInputOrderAreStable()
        {
            var first = new[]
            {
                new GridIntersection("grid-b", "grid-a", new Point2(10d, 0d)),
                new GridIntersection("GRID-A", "GRID-B", new Point2(0d, 0d)),
                new GridIntersection("grid-c", "grid-a", new Point2(5d, 5d))
            };
            var second = new[]
            {
                new GridIntersection("GRID-A", "GRID-C", new Point2(5d, 5d)),
                new GridIntersection("grid-b", "grid-a", new Point2(0d, 0d)),
                new GridIntersection("grid-a", "grid-b", new Point2(10d, 0d))
            };

            var a = GridIntersectionIdentityPlanner.Assign(first);
            var b = GridIntersectionIdentityPlanner.Assign(second);
            Equal(3, a.Count);
            Equal(3, b.Count);
            EqualSequence(a.Select(x => x.OwnerToken), b.Select(x => x.OwnerToken));
            EqualSequence(a.Select(x => x.PairKey), b.Select(x => x.PairKey));
            Equal("GRID-A", a[0].FirstElementId);
            Equal("GRID-B", a[0].SecondElementId);
            Equal(0, a[0].OccurrenceIndex);
            Equal(1, a[1].OccurrenceIndex);
            Equal(0d, a[0].Point.X);
            Equal(10d, a[1].Point.X);
        }

        private static void CaseAndWhitespaceCanonicalize()
        {
            var first = GridIntersectionIdentityPlanner.BuildPairKey(" grid-a ", "GRID-B");
            var second = GridIntersectionIdentityPlanner.BuildPairKey("grid-b", "Grid-A");
            Equal(first, second);
            Equal(
                GridIntersectionIdentityPlanner.BuildPairToken("grid-a", "grid-b"),
                GridIntersectionIdentityPlanner.BuildPairToken("GRID-B", "GRID-A"));
        }

        private static void NearDuplicatePointFailsClosed()
        {
            Throws(() => GridIntersectionIdentityPlanner.Assign(new[]
            {
                new GridIntersection("A", "B", new Point2(1d, 2d)),
                new GridIntersection("B", "A", new Point2(1d + 1e-10d, 2d))
            }, 1e-8d));
        }

        private static void MoreThanTwoPointsPerPairFailsClosed()
        {
            Throws(() => GridIntersectionIdentityPlanner.Assign(new[]
            {
                new GridIntersection("A", "B", new Point2(0d, 0d)),
                new GridIntersection("A", "B", new Point2(1d, 0d)),
                new GridIntersection("A", "B", new Point2(2d, 0d))
            }));
        }

        private static void SameGridPairFailsClosed()
        {
            Throws(() => GridIntersectionIdentityPlanner.Assign(new[]
            {
                new GridIntersection("A", "a", new Point2(0d, 0d))
            }));
        }

        private static void OwnerTokenIsCompact()
        {
            var ids = GridIntersectionIdentityPlanner.Assign(new[]
            {
                new GridIntersection(new string('A', 128), new string('B', 128), new Point2(0d, 0d))
            });
            True(ids[0].PairToken.StartsWith("GIP1:", StringComparison.Ordinal));
            True(ids[0].OwnerToken.StartsWith("GIX1:", StringComparison.Ordinal));
            True(ids[0].PairToken.Length < 100);
            True(ids[0].OwnerToken.Length < 100);
        }

        private static void Throws(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                return;
            }
            throw new Exception("Expected operation to throw.");
        }

        private static void EqualSequence(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            var left = expected.ToArray();
            var right = actual.ToArray();
            Equal(left.Length, right.Length);
            for (var i = 0; i < left.Length; i++) Equal(left[i], right[i]);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }
    }
}
