using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionMarkerPlannerSmoke
    {
        public static void Run()
        {
            PairIdentityIsCanonicalAndOccurrenceScoped();
            InputOrderDoesNotChangePairOwnerIdentity();
            NullIntersectionFailsClosed();
        }

        private static void PairIdentityIsCanonicalAndOccurrenceScoped()
        {
            var planned = GridIntersectionMarkerPlanner.Plan(new[]
            {
                new GridIntersection("GRID-B", "GRID-A", new Point2(1, 2)),
                new GridIntersection("GRID-A", "GRID-B", new Point2(3, 4))
            });

            Equal(2, planned.Count);
            Equal("GRID-A", planned[0].FirstElementId);
            Equal("GRID-B", planned[0].SecondElementId);
            Equal(planned[0].PairToken, planned[1].PairToken);
            Equal(0, planned[0].Occurrence);
            Equal(1, planned[1].Occurrence);
            Equal(GridIntersectionIdentityPlanner.BuildIntersectionOwner("GRID-A", "GRID-B", 0), planned[0].OwnerToken);
            Equal(GridIntersectionIdentityPlanner.BuildIntersectionOwner("GRID-A", "GRID-B", 1), planned[1].OwnerToken);
            True(!string.Equals(planned[0].OwnerToken, planned[1].OwnerToken, StringComparison.Ordinal));
        }

        private static void InputOrderDoesNotChangePairOwnerIdentity()
        {
            var first = GridIntersectionMarkerPlanner.Plan(new[]
            {
                new GridIntersection("G-2", "G-1", new Point2(0, 0))
            })[0];
            var second = GridIntersectionMarkerPlanner.Plan(new[]
            {
                new GridIntersection("G-1", "G-2", new Point2(0, 0))
            })[0];

            Equal(first.PairToken, second.PairToken);
            Equal(first.OwnerToken, second.OwnerToken);
        }

        private static void NullIntersectionFailsClosed()
        {
            Throws<InvalidOperationException>(() => GridIntersectionMarkerPlanner.Plan(new GridIntersection[] { null! }));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
