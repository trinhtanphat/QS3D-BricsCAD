using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSpatialOrderingSmoke
    {
        public static void Run()
        {
            ParallelLinesOrderByExplicitAxis();
            DescendingOrderIsDeterministic();
            NonParallelLineFailsClosed();
            ArcOrderingRequiresSeparatePolicy();
            DuplicateIdsFailClosed();
            AmbiguousProjectedCoordinateFailsClosed();
            InvalidAxisFailsClosed();
        }

        private static void ParallelLinesOrderByExplicitAxis()
        {
            var lines = new[]
            {
                GridReferenceCurve.Line("G-10", new Point2(10, -5), new Point2(10, 5)),
                GridReferenceCurve.Line("G--5", new Point2(-5, -20), new Point2(-5, 20)),
                GridReferenceCurve.Line("G-2", new Point2(2, -2), new Point2(2, 7))
            };

            var ordered = GridSpatialOrderingPlanner.OrderParallelLines(lines, new Point2(1, 0));
            Equal(3, ordered.Count);
            Equal("G--5", ordered[0].ElementId);
            Equal("G-2", ordered[1].ElementId);
            Equal("G-10", ordered[2].ElementId);
            Near(-5.0, ordered[0].Coordinate);
            Near(2.0, ordered[1].Coordinate);
            Near(10.0, ordered[2].Coordinate);
        }

        private static void DescendingOrderIsDeterministic()
        {
            var lines = new[]
            {
                GridReferenceCurve.Line("A", new Point2(-3, 0), new Point2(-3, 10)),
                GridReferenceCurve.Line("B", new Point2(4, -10), new Point2(4, 10))
            };

            var ordered = GridSpatialOrderingPlanner.OrderParallelLines(lines, new Point2(2, 0), descending: true);
            Equal("B", ordered[0].ElementId);
            Equal("A", ordered[1].ElementId);
        }

        private static void NonParallelLineFailsClosed()
        {
            var lines = new[]
            {
                GridReferenceCurve.Line("A", new Point2(0, 0), new Point2(0, 10)),
                GridReferenceCurve.Line("B", new Point2(2, 0), new Point2(3, 10))
            };

            Throws<InvalidOperationException>(() =>
                GridSpatialOrderingPlanner.OrderParallelLines(lines, new Point2(1, 0), alignmentTolerance: 1e-3));
        }

        private static void ArcOrderingRequiresSeparatePolicy()
        {
            var curves = new[]
            {
                GridReferenceCurve.Arc("A", new Point2(0, 0), 5, 0, Math.PI),
                GridReferenceCurve.Line("B", new Point2(2, -5), new Point2(2, 5))
            };

            Throws<InvalidOperationException>(() =>
                GridSpatialOrderingPlanner.OrderParallelLines(curves, new Point2(1, 0)));
        }

        private static void DuplicateIdsFailClosed()
        {
            var lines = new[]
            {
                GridReferenceCurve.Line("GRID-A", new Point2(0, 0), new Point2(0, 10)),
                GridReferenceCurve.Line("grid-a", new Point2(3, 0), new Point2(3, 10))
            };

            Throws<InvalidOperationException>(() =>
                GridSpatialOrderingPlanner.OrderParallelLines(lines, new Point2(1, 0)));
        }

        private static void AmbiguousProjectedCoordinateFailsClosed()
        {
            var lines = new[]
            {
                GridReferenceCurve.Line("A", new Point2(1.0, 0), new Point2(1.0, 10)),
                GridReferenceCurve.Line("B", new Point2(1.0 + 1e-10, -10), new Point2(1.0 + 1e-10, 20))
            };

            Throws<InvalidOperationException>(() =>
                GridSpatialOrderingPlanner.OrderParallelLines(lines, new Point2(1, 0), coordinateTolerance: 1e-8));
        }

        private static void InvalidAxisFailsClosed()
        {
            var line = GridReferenceCurve.Line("A", new Point2(0, 0), new Point2(0, 10));
            Throws<ArgumentException>(() =>
                GridSpatialOrderingPlanner.OrderParallelLines(new[] { line }, new Point2(0, 0)));
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
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
