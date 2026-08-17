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
            CenterOffsetAdditionCollapseFailsClosed();
            RepresentableCenterOffsetStillTessellates();
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

        private static void CenterOffsetAdditionCollapseFailsClosed()
        {
            var start = new Point2(1e16d, 1e16d);
            var end = new Point2(start.X + 4d, start.Y);
            if (end.Equals(start)) throw new Exception("Center-offset fixture requires distinct representable endpoints.");

            var midpoint = new Point2(
                start.X + (end.X - start.X) * 0.5d,
                start.Y + (end.Y - start.Y) * 0.5d);
            if (midpoint.Equals(start) || midpoint.Equals(end))
                throw new Exception("Center-offset fixture requires a representable midpoint distinct from both endpoints.");

            const double bulge = 0.75d;
            var chord = start.DistanceTo(end);
            var centerOffset = chord * 0.25d * (1d / bulge - bulge);
            if (!(centerOffset > 0d)) throw new Exception("Center-offset fixture requires a positive finite displacement.");
            if (midpoint.Y + centerOffset != midpoint.Y)
                throw new Exception("Center-offset fixture requires the finite displacement to collapse during coordinate addition.");

            try
            {
                BulgeArcTessellator.Tessellate(start, end, bulge, 0.1d);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Expected unrepresentable bulge center displacement to fail closed.");
        }

        private static void RepresentableCenterOffsetStillTessellates()
        {
            var start = new Point2(0d, 0d);
            var end = new Point2(4d, 0d);
            var points = BulgeArcTessellator.Tessellate(start, end, 0.75d, 0.1d);
            if (points.Count <= 2 || points.Count > 4097) throw new Exception("Expected ordinary non-semicircle bulge to tessellate.");
            if (!points[0].Equals(start) || !points[points.Count - 1].Equals(end)) throw new Exception("Ordinary arc endpoints changed.");
        }
    }
}