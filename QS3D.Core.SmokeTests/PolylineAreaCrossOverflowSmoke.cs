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

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
