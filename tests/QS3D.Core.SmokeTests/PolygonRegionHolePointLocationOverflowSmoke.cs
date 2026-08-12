using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonRegionHolePointLocationOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const double scale = 1e160;
            const double outerHalfWidth = 1e147;
            const double holeHalfWidth = 1e145;
            const double holeStart = 2e159;
            const double holeEnd = 8e159;

            var outer = new[]
            {
                new Point2(0d, -outerHalfWidth),
                new Point2(scale, scale - outerHalfWidth),
                new Point2(scale, scale + outerHalfWidth),
                new Point2(0d, outerHalfWidth)
            };
            var hole = new[]
            {
                new Point2(holeStart, holeStart - holeHalfWidth),
                new Point2(holeEnd, holeEnd - holeHalfWidth),
                new Point2(holeEnd, holeEnd + holeHalfWidth),
                new Point2(holeStart, holeStart + holeHalfWidth)
            };

            var region = PolygonRegionScanlineClipper.NormalizeAndValidate(
                outer,
                new[] { (IReadOnlyList<Point2>)hole });

            if (region.Outer.Count != 4)
                throw new Exception("Expected the long diagonal outer boundary to retain four vertices.");
            if (region.Holes.Count != 1 || region.Holes[0].Count != 4)
                throw new Exception("Expected one strictly-contained long diagonal hole after stable point-location interpolation.");
            foreach (var point in region.Outer) EnsureFinite(point, "outer boundary");
            foreach (var point in region.Holes[0]) EnsureFinite(point, "hole boundary");
        }

        private static void EnsureFinite(Point2 point, string label)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                throw new Exception("Expected finite " + label + " coordinates.");
        }
    }
}
