using System;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionPlannerSmoke
    {
        public static void Run()
        {
            CrossingLinesProduceOneDeterministicPoint();
            EndpointTouchIsAcceptedButOverlapFailsClosed();
            LineArcRespectsArcSweep();
            ArcArcProducesTwoPointsWhenBothSweepsContainThem();
            LargeFiniteArcArcProducesFinitePoints();
            LargeFiniteArcArcAllowsRadialRoundoff();
            CoincidentArcSupportFailsClosed();
            DuplicateElementIdsFailClosed();
            ElementIdsAreCanonicalizedBeforeDuplicateCheck();
            OverflowingDerivedGeometryFailsClosed();
        }

        private static void CrossingLinesProduceOneDeterministicPoint()
        {
            var result = GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line("G-X", new Point2(-5, 0), new Point2(5, 0)),
                GridReferenceCurve.Line("G-Y", new Point2(2, -5), new Point2(2, 5))
            });

            Equal(1, result.Count);
            Equal("G-X", result[0].FirstElementId);
            Equal("G-Y", result[0].SecondElementId);
            Near(2.0, result[0].Point.X);
            Near(0.0, result[0].Point.Y);
        }

        private static void EndpointTouchIsAcceptedButOverlapFailsClosed()
        {
            var touching = GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line("G-1", new Point2(0, 0), new Point2(5, 0)),
                GridReferenceCurve.Line("G-2", new Point2(5, 0), new Point2(10, 0))
            });
            Equal(1, touching.Count);
            Near(5.0, touching[0].Point.X);

            Throws<InvalidOperationException>(() => GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line("G-1", new Point2(0, 0), new Point2(8, 0)),
                GridReferenceCurve.Line("G-2", new Point2(5, 0), new Point2(10, 0))
            }));
        }

        private static void LineArcRespectsArcSweep()
        {
            var upperHalf = GridReferenceCurve.Arc("G-A", new Point2(0, 0), 5.0, 0.0, Math.PI);
            var horizontal = GridReferenceCurve.Line("G-L", new Point2(-10, 0), new Point2(10, 0));
            var result = GridIntersectionPlanner.FindIntersections(new[] { horizontal, upperHalf });

            Equal(2, result.Count);
            Near(-5.0, result[0].Point.X);
            Near(5.0, result[1].Point.X);
        }

        private static void ArcArcProducesTwoPointsWhenBothSweepsContainThem()
        {
            var first = GridReferenceCurve.Arc("G-A", new Point2(0, 0), 5.0, 0.0, Math.PI * 2.0);
            var second = GridReferenceCurve.Arc("G-B", new Point2(6, 0), 5.0, 0.0, Math.PI * 2.0);
            var result = GridIntersectionPlanner.FindIntersections(new[] { first, second });

            Equal(2, result.Count);
            Near(3.0, result[0].Point.X);
            Near(-4.0, result[0].Point.Y);
            Near(3.0, result[1].Point.X);
            Near(4.0, result[1].Point.Y);
        }

        private static void LargeFiniteArcArcProducesFinitePoints()
        {
            const double radius = 1e200;
            var first = GridReferenceCurve.Arc("G-LARGE-A", new Point2(0.0, 0.0), radius, 0.0, Math.PI * 2.0);
            var second = GridReferenceCurve.Arc("G-LARGE-B", new Point2(radius, 0.0), radius, 0.0, Math.PI * 2.0);

            var result = GridIntersectionPlanner.FindIntersections(new[] { first, second });

            Equal(2, result.Count);
            NearRelative(5e199, result[0].Point.X, 1e-14);
            NearRelative(-8.660254037844386e199, result[0].Point.Y, 1e-14);
            NearRelative(5e199, result[1].Point.X, 1e-14);
            NearRelative(8.660254037844386e199, result[1].Point.Y, 1e-14);
        }

        private static void LargeFiniteArcArcAllowsRadialRoundoff()
        {
            const double firstRadius = 1e200;
            const double secondRadius = 5e199;
            const double centerDistance = 7.5e199;
            const double centerAngle = 0.1;
            var secondCenter = new Point2(
                centerDistance * Math.Cos(centerAngle),
                centerDistance * Math.Sin(centerAngle));
            var first = GridReferenceCurve.Arc("G-ROUND-A", new Point2(0.0, 0.0), firstRadius, 0.0, Math.PI * 2.0);
            var second = GridReferenceCurve.Arc("G-ROUND-B", secondCenter, secondRadius, 0.0, Math.PI * 2.0);

            var result = GridIntersectionPlanner.FindIntersections(new[] { first, second });

            Equal(2, result.Count);
            NearRelative(8.222969996097536e199, result[0].Point.X, 1e-14);
            NearRelative(5.690585597570753e199, result[0].Point.Y, 1e-14);
            NearRelative(9.189602896267915e199, result[1].Point.X, 1e-14);
            NearRelative(-3.943500806251261e199, result[1].Point.Y, 1e-14);
        }

        private static void CoincidentArcSupportFailsClosed()
        {
            Throws<InvalidOperationException>(() => GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Arc("G-A", new Point2(0, 0), 5.0, 0.0, Math.PI),
                GridReferenceCurve.Arc("G-B", new Point2(0, 0), 5.0, Math.PI, Math.PI)
            }));
        }

        private static void DuplicateElementIdsFailClosed()
        {
            Throws<InvalidOperationException>(() => GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line("G-A", new Point2(0, 0), new Point2(5, 0)),
                GridReferenceCurve.Line("g-a", new Point2(0, 1), new Point2(5, 1))
            }));
        }

        private static void ElementIdsAreCanonicalizedBeforeDuplicateCheck()
        {
            Throws<InvalidOperationException>(() => GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line(" G-A ", new Point2(0, 0), new Point2(5, 0)),
                GridReferenceCurve.Line("g-a", new Point2(0, 1), new Point2(5, 1))
            }));

            var result = GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line(" G-X ", new Point2(-1, 0), new Point2(1, 0)),
                GridReferenceCurve.Line("G-Y", new Point2(0, -1), new Point2(0, 1))
            });
            Equal("G-X", result.Single().FirstElementId);
        }

        private static void OverflowingDerivedGeometryFailsClosed()
        {
            Throws<OverflowException>(() => GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line("G-HUGE", new Point2(double.MaxValue, 0), new Point2(-double.MaxValue, 0)),
                GridReferenceCurve.Line("G-Y", new Point2(0, -1), new Point2(0, 1))
            }));
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-7) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void NearRelative(double expected, double actual, double relativeTolerance)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual))
                throw new Exception("Expected a finite value, got " + actual + ".");
            var scale = Math.Max(1.0, Math.Abs(expected));
            if (Math.Abs(expected - actual) > scale * relativeTolerance)
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
