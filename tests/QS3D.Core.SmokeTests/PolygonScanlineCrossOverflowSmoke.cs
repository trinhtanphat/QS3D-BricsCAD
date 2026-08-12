using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonScanlineCrossOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PreservesDeterminantWhenRawProductsOverflow();
            PreservesFiniteDeterminantBeforeNormalizationCancellation();
        }

        private static void PreservesDeterminantWhenRawProductsOverflow()
        {
            const double scale = 1e160;
            var polygon = new[]
            {
                new Point2(0d, 0d),
                new Point2(scale, scale),
                new Point2(scale, 1.000000000000001e160),
                new Point2(0d, 1e145)
            };

            var normalized = PolygonScanlineClipper.NormalizeAndValidate(polygon);
            if (normalized.Count != 4)
                throw new Exception("Expected the finite large-coordinate polygon to preserve all four vertices.");
            foreach (var point in normalized)
                if (!Finite(point.X) || !Finite(point.Y))
                    throw new Exception("Expected normalized polygon coordinates to remain finite.");

            var area = PolylineMetrics.Area(normalized);
            if (!Finite(area) || !(area > 0d))
                throw new Exception("Expected a finite positive area for the determinant-cancellation polygon.");
        }

        private static void PreservesFiniteDeterminantBeforeNormalizationCancellation()
        {
            const double ax = 1e46;
            const double ay = 2.1485982218963585e45;
            const double bx = 0.01d;
            const double by = 0.0021485982218963583d;
            var rawDeterminant = ax * by - ay * bx;
            if (!Finite(rawDeterminant) || rawDeterminant == 0d)
                throw new Exception("Polygon finite-cancellation fixture must have a finite non-zero raw determinant.");

            var polygon = new[]
            {
                new Point2(0d, 0d),
                new Point2(ax, ay),
                new Point2(bx, by)
            };
            var normalized = PolygonScanlineClipper.NormalizeAndValidate(polygon);
            if (normalized.Count != 3)
                throw new Exception("Expected the finite non-degenerate triangle to preserve all three vertices.");

            var area = PolylineMetrics.Area(normalized);
            var expected = Math.Abs(rawDeterminant * 0.5d);
            if (!Finite(area) || area != expected)
                throw new Exception("Expected polygon validation to preserve the finite raw determinant before normalization can erase it.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
