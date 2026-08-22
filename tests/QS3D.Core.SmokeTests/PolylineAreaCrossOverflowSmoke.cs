using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineAreaCrossOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PreservesFiniteDeterminantWhenRawProductsOverflow();
            PreservesFiniteDeterminantBeforeNormalizationCancellation();
        }

        private static void PreservesFiniteDeterminantWhenRawProductsOverflow()
        {
            const double scale = 1e160;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(scale, scale),
                new Point2(scale, 1.000000000000001e160)
            };

            var signedArea = PolylineMetrics.SignedArea(points);
            if (!Finite(signedArea) || !(signedArea > 0d))
                throw new Exception("Expected a finite positive polyline area after scale-safe cross cancellation.");

            var area = PolylineMetrics.Area(points);
            if (!Finite(area) || area != signedArea)
                throw new Exception("Expected absolute polyline area to preserve the finite positive signed area.");
        }

        private static void PreservesFiniteDeterminantBeforeNormalizationCancellation()
        {
            const double ax = 1e46;
            const double ay = 2.1485982218963585e45;
            const double bx = 0.01d;
            const double by = 0.0021485982218963583d;
            var rawDeterminant = ax * by - ay * bx;
            if (!Finite(rawDeterminant) || rawDeterminant == 0d)
                throw new Exception("Polyline cancellation fixture must have a finite non-zero raw determinant.");

            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(ax, ay),
                new Point2(bx, by)
            };
            var signedArea = PolylineMetrics.SignedArea(points);
            var expected = rawDeterminant * 0.5d;
            if (!Finite(signedArea) || signedArea != expected)
                throw new Exception("Expected polyline signed area to preserve the finite raw determinant before normalization can erase it.");
            if (PolylineMetrics.Area(points) != Math.Abs(expected))
                throw new Exception("Expected absolute polyline area to preserve the finite cancellation-sensitive determinant magnitude.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
