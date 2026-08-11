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

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
