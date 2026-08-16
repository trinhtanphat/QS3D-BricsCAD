using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class BulgeMidpointOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OverflowSafeMidpointStillTessellates();
            RepresentabilityCollapseFailsClosed();
        }

        private static void OverflowSafeMidpointStillTessellates()
        {
            var start = new Point2(9e307d, 0d);
            var end = new Point2(start.X + 4e292d, 0d);
            if (!double.IsInfinity(start.X + end.X)) throw new Exception("Fixture must overflow with the naive midpoint sum.");

            var midpoint = new Point2(
                start.X + (end.X - start.X) * 0.5d,
                start.Y + (end.Y - start.Y) * 0.5d);
            if (midpoint.Equals(start) || midpoint.Equals(end))
                throw new Exception("Overflow-only fixture requires a representable midpoint distinct from both endpoints.");

            var points = BulgeArcTessellator.Tessellate(start, end, 1d, 1e292d);
            if (points.Count <= 2 || points.Count > 4097) throw new Exception("Expected bounded curved tessellation.");
            if (!points[0].Equals(start) || !points[points.Count - 1].Equals(end)) throw new Exception("Arc endpoints changed.");
            foreach (var point in points)
            {
                if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                    throw new Exception("Expected finite tessellated points.");
            }
        }

        private static void RepresentabilityCollapseFailsClosed()
        {
            var start = new Point2(1e16d, 0d);
            var end = new Point2(start.X + 2d, 0d);
            if (end.Equals(start)) throw new Exception("Collapse fixture requires distinct representable endpoints.");

            var midpoint = new Point2(
                start.X + (end.X - start.X) * 0.5d,
                start.Y + (end.Y - start.Y) * 0.5d);
            if (!midpoint.Equals(start)) throw new Exception("Collapse fixture requires the finite midpoint to alias the start endpoint.");

            try
            {
                BulgeArcTessellator.Tessellate(start, end, 1d, 0.01d);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Expected midpoint representability collapse to fail closed.");
        }
    }
}
