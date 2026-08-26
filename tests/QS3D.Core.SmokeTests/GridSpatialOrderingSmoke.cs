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
            ReviewedMixedOrderIsPermutationInvariant();
            ReviewedGroupPrecedenceIsExplicit();
            ReviewedArcCenterMismatchFailsClosed();
            ReviewedArcRadiusTieFailsClosed();
            ReviewedCrossKindDuplicateIdFailsClosed();
            ReviewedInvalidArcSweepFailsClosed();
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

        private static void ReviewedMixedOrderIsPermutationInvariant()
        {
            var first = new[]
            {
                GridReferenceCurve.Arc("R20", new Point2(0, 0), 20, 0, Math.PI),
                GridReferenceCurve.Line("L5", new Point2(5, -10), new Point2(5, 10)),
                GridReferenceCurve.Arc("R10", new Point2(0, 0), 10, Math.PI / 2.0, Math.PI),
                GridReferenceCurve.Line("L-2", new Point2(-2, -10), new Point2(-2, 10))
            };
            var second = new[] { first[3], first[0], first[1], first[2] };

            var a = GridSpatialOrderingPlanner.OrderReviewedSet(
                first, new Point2(1, 0), new Point2(0, 0), GridReviewedGroupPrecedence.LinesThenArcs);
            var b = GridSpatialOrderingPlanner.OrderReviewedSet(
                second, new Point2(1, 0), new Point2(0, 0), GridReviewedGroupPrecedence.LinesThenArcs);

            Equal(4, a.Count);
            Equal(4, b.Count);
            Equal("L-2", a[0].ElementId);
            Equal("L5", a[1].ElementId);
            Equal("R10", a[2].ElementId);
            Equal("R20", a[3].ElementId);
            for (var i = 0; i < a.Count; i++)
            {
                Equal(a[i].ElementId, b[i].ElementId);
                Equal(a[i].Kind, b[i].Kind);
                Equal(a[i].GroupIndex, b[i].GroupIndex);
                Near(a[i].Coordinate, b[i].Coordinate);
            }
        }

        private static void ReviewedGroupPrecedenceIsExplicit()
        {
            var curves = new[]
            {
                GridReferenceCurve.Line("L", new Point2(2, -10), new Point2(2, 10)),
                GridReferenceCurve.Arc("R", new Point2(0, 0), 5, 0, Math.PI)
            };
            var ordered = GridSpatialOrderingPlanner.OrderReviewedSet(
                curves, new Point2(1, 0), new Point2(0, 0), GridReviewedGroupPrecedence.ArcsThenLines);
            Equal("R", ordered[0].ElementId);
            Equal(GridReferenceCurveKind.Arc, ordered[0].Kind);
            Equal(0, ordered[0].GroupIndex);
            Equal("L", ordered[1].ElementId);
            Equal(GridReferenceCurveKind.Line, ordered[1].Kind);
            Equal(1, ordered[1].GroupIndex);
        }

        private static void ReviewedArcCenterMismatchFailsClosed()
        {
            var curves = new[]
            {
                GridReferenceCurve.Arc("R1", new Point2(0, 0), 5, 0, Math.PI),
                GridReferenceCurve.Arc("R2", new Point2(0.01, 0), 10, 0, Math.PI)
            };
            Throws<InvalidOperationException>(() => GridSpatialOrderingPlanner.OrderReviewedSet(
                curves, new Point2(1, 0), new Point2(0, 0), GridReviewedGroupPrecedence.LinesThenArcs,
                coordinateTolerance: 1e-8));
        }

        private static void ReviewedArcRadiusTieFailsClosed()
        {
            var curves = new[]
            {
                GridReferenceCurve.Arc("R1", new Point2(0, 0), 5, 0, Math.PI),
                GridReferenceCurve.Arc("R2", new Point2(0, 0), 5 + 1e-10, 0, Math.PI)
            };
            Throws<InvalidOperationException>(() => GridSpatialOrderingPlanner.OrderReviewedSet(
                curves, new Point2(1, 0), new Point2(0, 0), GridReviewedGroupPrecedence.LinesThenArcs,
                coordinateTolerance: 1e-8));
        }

        private static void ReviewedCrossKindDuplicateIdFailsClosed()
        {
            var curves = new[]
            {
                GridReferenceCurve.Line("GRID-A", new Point2(2, -10), new Point2(2, 10)),
                GridReferenceCurve.Arc("grid-a", new Point2(0, 0), 5, 0, Math.PI)
            };
            Throws<InvalidOperationException>(() => GridSpatialOrderingPlanner.OrderReviewedSet(
                curves, new Point2(1, 0), new Point2(0, 0), GridReviewedGroupPrecedence.LinesThenArcs));
        }

        private static void ReviewedInvalidArcSweepFailsClosed()
        {
            var arc = GridReferenceCurve.Arc("R", new Point2(0, 0), 5, 0, 0);
            Throws<InvalidOperationException>(() => GridSpatialOrderingPlanner.OrderReviewedSet(
                new[] { arc }, new Point2(1, 0), new Point2(0, 0), GridReviewedGroupPrecedence.ArcsThenLines));
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