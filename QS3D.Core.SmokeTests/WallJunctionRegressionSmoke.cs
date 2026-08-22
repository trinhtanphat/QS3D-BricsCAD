using System;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionRegressionSmoke
    {
        public static void Run()
        {
            StraightContinuation();
            LCorner();
            TJunction();
            XJunction();
            NearEndpointSnapsByTolerance();
            HugeCoordinateCrossingUsesFallbackIndex();
            RejectsDuplicateIdsAndInvalidCoordinates();
        }

        private static void StraightContinuation()
        {
            var nodes = Plan(
                Segment("A", 0, 0, 1, 0),
                Segment("B", 1, 0, 2, 0));
            var node = SingleAt(nodes, 1, 0);
            Equal(WallJunctionKind.Straight, node.Kind);
            Equal(2, node.RayCount);
            Equal(2, node.SegmentIds.Count);
        }

        private static void LCorner()
        {
            var nodes = Plan(
                Segment("A", 0, 0, 1, 0),
                Segment("B", 1, 0, 1, 1));
            var node = SingleAt(nodes, 1, 0);
            Equal(WallJunctionKind.L, node.Kind);
            Equal(2, node.RayCount);
        }

        private static void TJunction()
        {
            var nodes = Plan(
                Segment("A", -1, 0, 1, 0),
                Segment("B", 0, 0, 0, 1));
            var node = SingleAt(nodes, 0, 0);
            Equal(WallJunctionKind.T, node.Kind);
            Equal(3, node.RayCount);
            Equal(2, node.SegmentIds.Count);
        }

        private static void XJunction()
        {
            var nodes = Plan(
                Segment("A", -1, 0, 1, 0),
                Segment("B", 0, -1, 0, 1));
            var node = SingleAt(nodes, 0, 0);
            Equal(WallJunctionKind.X, node.Kind);
            Equal(4, node.RayCount);
        }

        private static void NearEndpointSnapsByTolerance()
        {
            var nodes = new WallJunctionPlanner().Plan(new[]
            {
                Segment("A", 0, 0, 1, 0),
                Segment("B", 1.003, 0, 1.003, 1)
            }, 0.005d);
            var junction = nodes.FirstOrDefault(x => x.SegmentIds.Count == 2);
            if (junction == null) throw new Exception("Expected a tolerance-snapped junction.");
            Equal(WallJunctionKind.L, junction.Kind);
        }

        private static void HugeCoordinateCrossingUsesFallbackIndex()
        {
            var nodes = Plan(
                Segment("H", -3e200d, 0d, 3e200d, 0d),
                Segment("V", 0d, -4e200d, 0d, 4e200d));
            var node = SingleAt(nodes, 0d, 0d);
            Equal(WallJunctionKind.X, node.Kind);
            Equal(4, node.RayCount);
            Equal(2, node.SegmentIds.Count);
        }

        private static void RejectsDuplicateIdsAndInvalidCoordinates()
        {
            Throws<InvalidOperationException>(() => Plan(
                Segment("A", 0, 0, 1, 0),
                Segment("A", 1, 0, 2, 0)));
            Throws<ArgumentOutOfRangeException>(() => Plan(
                new WallAxisSegment("A", new Point2(double.NaN, 0), new Point2(1, 0))));
            Throws<InvalidOperationException>(() => Plan(
                Segment("A", 0, 0, 0, 0)));
            Throws<OverflowException>(() => Plan(
                Segment("A", -double.MaxValue, 0d, double.MaxValue, 0d)));
        }

        private static WallAxisSegment Segment(string id, double x1, double y1, double x2, double y2) =>
            new WallAxisSegment(id, new Point2(x1, y1), new Point2(x2, y2));

        private static System.Collections.Generic.IReadOnlyList<WallJunction> Plan(params WallAxisSegment[] segments) =>
            new WallJunctionPlanner().Plan(segments, 0.005d);

        private static WallJunction SingleAt(System.Collections.Generic.IReadOnlyList<WallJunction> nodes, double x, double y)
        {
            var matches = nodes.Where(n => Math.Abs(n.Point.X - x) <= 0.005d && Math.Abs(n.Point.Y - y) <= 0.005d && n.SegmentIds.Count > 1).ToList();
            if (matches.Count != 1) throw new Exception("Expected one junction at " + x + "," + y + "; got " + matches.Count + ".");
            return matches[0];
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
