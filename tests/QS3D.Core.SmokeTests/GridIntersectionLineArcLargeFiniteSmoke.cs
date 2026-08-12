using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionLineArcLargeFiniteSmoke
    {
        public static void Run()
        {
            LargeFiniteQuadraticFallsBackWithoutOverflow();
            FiniteRawQuadraticPreservesNearEndpointTangent();
        }

        private static void LargeFiniteQuadraticFallsBackWithoutOverflow()
        {
            var line = GridReferenceCurve.Line(
                "G-LARGE-L",
                new Point2(-1e200, 0.0),
                new Point2(1e200, 0.0));
            var arc = GridReferenceCurve.Arc(
                "G-LARGE-A",
                new Point2(0.0, 0.0),
                5e199,
                0.0,
                Math.PI * 2.0);

            var result = GridIntersectionPlanner.FindIntersections(new[] { line, arc });

            Equal(2, result.Count);
            Equal("G-LARGE-L", result[0].FirstElementId);
            Equal("G-LARGE-A", result[0].SecondElementId);
            NearRelative(-5e199, result[0].Point.X, 1e-14);
            NearRelative(0.0, result[0].Point.Y, 0.0);
            NearRelative(5e199, result[1].Point.X, 1e-14);
            NearRelative(0.0, result[1].Point.Y, 0.0);
        }

        private static void FiniteRawQuadraticPreservesNearEndpointTangent()
        {
            const double tolerance = 1e-8;
            var line = GridReferenceCurve.Line(
                "G-RAW-L",
                new Point2(0.0, 0.0),
                new Point2(2e-8, 0.0));
            var arc = GridReferenceCurve.Arc(
                "G-RAW-A",
                new Point2(1e154, 0.0),
                1e154,
                0.0,
                Math.PI * 2.0);

            var result = GridIntersectionPlanner.FindIntersections(new[] { line, arc }, tolerance);

            Equal(1, result.Count);
            Equal("G-RAW-L", result[0].FirstElementId);
            Equal("G-RAW-A", result[0].SecondElementId);
            NearRelative(0.0, result[0].Point.X, 0.0);
            NearRelative(0.0, result[0].Point.Y, 0.0);
        }

        private static void NearRelative(double expected, double actual, double relativeTolerance)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual))
                throw new Exception("Expected a finite value, got " + actual + ".");

            if (expected == 0.0)
            {
                if (actual != 0.0) throw new Exception("Expected 0, got " + actual + ".");
                return;
            }

            var scale = Math.Abs(expected);
            if (Math.Abs(expected - actual) > scale * relativeTolerance)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
