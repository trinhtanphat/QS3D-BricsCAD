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
            var start = new Point2(9e307d, 0d);
            var end = new Point2(9e307d + 1e292d, 0d);
            if (!double.IsInfinity(start.X + end.X)) throw new Exception("Fixture must overflow with the naive midpoint sum.");

            var points = BulgeArcTessellator.Tessellate(start, end, 1d, 1e292d);
            if (points.Count <= 2 || points.Count > 4097) throw new Exception("Expected bounded curved tessellation.");
            if (!points[0].Equals(start) || !points[points.Count - 1].Equals(end)) throw new Exception("Arc endpoints changed.");
            foreach (var point in points)
            {
                if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                    throw new Exception("Expected finite tessellated points.");
            }
        }
    }
}
