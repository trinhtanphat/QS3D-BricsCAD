using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class TopologyScaleSmoke
    {
        public static void Run()
        {
            RoomGridProducesAllBoundedFaces();
            WallGridClassifiesAllJunctions();
        }

        private static void RoomGridProducesAllBoundedFaces()
        {
            const int cells = 20;
            var segments = new List<BoundarySegment>();
            for (var x = 0; x <= cells; x++)
                segments.Add(new BoundarySegment(new Point2(x, 0), new Point2(x, cells), "V" + x));
            for (var y = 0; y <= cells; y++)
                segments.Add(new BoundarySegment(new Point2(0, y), new Point2(cells, y), "H" + y));

            var rooms = new RoomBoundaryEngine().Discover(segments, 0.001d, 0.5d);
            Equal(cells * cells, rooms.Count);
            Near(cells * cells, rooms.Sum(x => x.Area), 1e-8d);
            if (rooms.Any(x => Math.Abs(x.Area - 1d) > 1e-10d || Math.Abs(x.Perimeter - 4d) > 1e-10d))
                throw new Exception("Grid room geometry is not deterministic unit-square topology.");
        }

        private static void WallGridClassifiesAllJunctions()
        {
            const int cells = 20;
            var segments = new List<WallAxisSegment>();
            for (var x = 0; x <= cells; x++)
                segments.Add(new WallAxisSegment("V" + x, new Point2(x, 0), new Point2(x, cells)));
            for (var y = 0; y <= cells; y++)
                segments.Add(new WallAxisSegment("H" + y, new Point2(0, y), new Point2(cells, y)));

            var plan = new WallJunctionAdjustmentPlanner().Plan(segments, 0.001d);
            Equal(0, plan.Adjustments.Count);
            Equal((cells - 1) * (cells - 1), plan.Junctions.Count(x => x.Kind == WallJunctionKind.X));
            Equal(4 * (cells - 1), plan.Junctions.Count(x => x.Kind == WallJunctionKind.T));
            Equal(4, plan.Junctions.Count(x => x.Kind == WallJunctionKind.L));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
