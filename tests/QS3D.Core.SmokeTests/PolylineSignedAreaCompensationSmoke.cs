using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineSignedAreaCompensationSmoke
    {
        private const double Large = 1e16d;
        private const double SmallInverse = 1e-16d;
        private const double ExpectedArea = 5000000000000001d;
        private const double ScaledCoordinate = 1e155d;
        private const double TinyCross = 1d / 1152921504606846976d;

        [ModuleInitializer]
        internal static void Initialize()
        {
            PositiveOrientationPreservesRepresentableLowOrderArea();
            NegativeOrientationPreservesRepresentableLowOrderArea();
            ScaledFallbackPreservesRepresentableLowOrderArea();
            ScaledFallbackPreservesNegativeRepresentableLowOrderArea();
            LengthCompensationRemainsStable();
        }

        private static void PositiveOrientationPreservesRepresentableLowOrderArea()
        {
            var points = PositivePolygon();

            Exact(ExpectedArea, PolylineMetrics.SignedArea(points), "positive signed area");
            Exact(ExpectedArea, PolylineMetrics.Area(points), "positive absolute area");
        }

        private static void NegativeOrientationPreservesRepresentableLowOrderArea()
        {
            var positive = PositivePolygon();
            var points = new[]
            {
                positive[0],
                positive[4],
                positive[3],
                positive[2],
                positive[1]
            };

            Exact(-ExpectedArea, PolylineMetrics.SignedArea(points), "negative signed area");
            Exact(ExpectedArea, PolylineMetrics.Area(points), "negative absolute area");
        }

        private static void ScaledFallbackPreservesRepresentableLowOrderArea()
        {
            var points = ScaledFallbackPolygon();
            var expectedArea = ScaledFallbackExpectedArea();

            Exact(expectedArea, PolylineMetrics.SignedArea(points), "scaled-fallback signed area");
            Exact(expectedArea, PolylineMetrics.Area(points), "scaled-fallback absolute area");
        }

        private static void ScaledFallbackPreservesNegativeRepresentableLowOrderArea()
        {
            var positive = ScaledFallbackPolygon();
            var points = new[]
            {
                positive[0],
                positive[4],
                positive[3],
                positive[2],
                positive[1]
            };
            var expectedArea = ScaledFallbackExpectedArea();

            Exact(-expectedArea, PolylineMetrics.SignedArea(points), "scaled-fallback negative signed area");
            Exact(expectedArea, PolylineMetrics.Area(points), "scaled-fallback negative absolute area");
        }

        private static void LengthCompensationRemainsStable()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(Large, 0d),
                new Point2(Large + 2d, 0d)
            };

            Exact(10000000000000002d, PolylineMetrics.Length(points, closed: false), "existing length compensation");
        }

        private static Point2[] PositivePolygon()
        {
            return new[]
            {
                new Point2(0d, 0d),
                new Point2(1d, 0d),
                new Point2(0d, 1d),
                new Point2(-Large, 0d),
                new Point2(0d, -SmallInverse)
            };
        }

        private static Point2[] ScaledFallbackPolygon()
        {
            var tinyY = TinyCross * ScaledCoordinate;
            return new[]
            {
                new Point2(0d, 0d),
                new Point2(ScaledCoordinate, 0d),
                new Point2(ScaledCoordinate, tinyY),
                new Point2(0d, ScaledCoordinate),
                new Point2(ScaledCoordinate, 0d)
            };
        }

        private static double ScaledFallbackExpectedArea()
        {
            // The direct fan crosses overflow on the +1 / -1 terms, forcing the
            // scaled fallback. Its normalized sequence is TinyCross, +1, -1:
            // the former Kahan accumulator collapsed this representable residual
            // to zero, while the retained Neumaier correction preserves it.
            return ((TinyCross * 0.5d) * ScaledCoordinate) * ScaledCoordinate;
        }

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected Polyline metric for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
