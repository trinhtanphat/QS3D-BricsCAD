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

        [ModuleInitializer]
        internal static void Initialize()
        {
            PositiveOrientationPreservesRepresentableLowOrderArea();
            NegativeOrientationPreservesRepresentableLowOrderArea();
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

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected Polyline metric for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
