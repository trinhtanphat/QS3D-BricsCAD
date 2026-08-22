using System;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionAdjustmentSmoke
    {
        public static void Run()
        {
            NearEndpointProducesSnap();
            ExactJunctionNeedsNoMove();
            TJunctionInteriorNeedsNoEndpointMove();
            RejectsCollapsingAdjustment();
        }

        private static void NearEndpointProducesSnap()
        {
            var plan = new WallJunctionAdjustmentPlanner().Plan(new[]
            {
                Segment("A", 0, 0, 1, 0),
                Segment("B", 1.003, 0, 1.003, 1)
            }, 0.005d);
            var adjustment = plan.Adjustments.Single();
            Equal("B", adjustment.SegmentId);
            Equal(WallEndpointKind.Start, adjustment.Endpoint);
            Equal(WallJunctionKind.L, adjustment.JunctionKind);
            Near(0.003d, adjustment.Distance, 1e-9d);
            Near(1d, adjustment.To.X, 1e-12d);
            Near(0d, adjustment.To.Y, 1e-12d);
        }

        private static void ExactJunctionNeedsNoMove()
        {
            var plan = new WallJunctionAdjustmentPlanner().Plan(new[]
            {
                Segment("A", 0, 0, 1, 0),
                Segment("B", 1, 0, 1, 1)
            }, 0.005d);
            Equal(0, plan.Adjustments.Count);
        }

        private static void TJunctionInteriorNeedsNoEndpointMove()
        {
            var plan = new WallJunctionAdjustmentPlanner().Plan(new[]
            {
                Segment("A", -1, 0, 1, 0),
                Segment("B", 0, 0, 0, 1)
            }, 0.005d);
            Equal(0, plan.Adjustments.Count);
            if (!plan.Junctions.Any(x => x.Kind == WallJunctionKind.T)) throw new Exception("Expected T junction analysis to remain available.");
        }

        private static void RejectsCollapsingAdjustment()
        {
            Throws<InvalidOperationException>(() => new WallJunctionAdjustmentPlanner().Plan(new[]
            {
                Segment("A", 0, 0, 0.003, 0),
                Segment("B", 0, 0, 0, 1)
            }, 0.005d, 1e-6d));
        }

        private static WallAxisSegment Segment(string id, double x1, double y1, double x2, double y2) =>
            new WallAxisSegment(id, new Point2(x1, y1), new Point2(x2, y2));

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

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
