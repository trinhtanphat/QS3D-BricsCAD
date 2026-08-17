using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class BulgeAdjacentVertexCollapseSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AdjacentPrecisionCollapseFailsClosed();
            FinalEndpointPrecisionCollapseFailsClosed();
            RepresentableSemicircleStillTessellates();
        }

        private static void AdjacentPrecisionCollapseFailsClosed()
        {
            var start = new Point2(1e16d, 1e16d);
            var end = new Point2(start.X + 4d, start.Y);
            if (end.Equals(start)) throw new Exception("Adjacent-collapse fixture requires distinct representable endpoints.");

            var midpoint = new Point2(
                start.X + (end.X - start.X) * 0.5d,
                start.Y + (end.Y - start.Y) * 0.5d);
            if (midpoint.Equals(start) || midpoint.Equals(end))
                throw new Exception("Adjacent-collapse fixture must bypass the midpoint representability guard.");

            const double bulge = 1d;
            var chord = start.DistanceTo(end);
            var centerOffset = chord * 0.25d * (1d / bulge - bulge);
            if (centerOffset != 0d)
                throw new Exception("Adjacent-collapse fixture must bypass center-offset displacement guards.");

            try
            {
                BulgeArcTessellator.Tessellate(start, end, bulge, 0.1d);
            }
            catch (InvalidOperationException error)
            {
                if (error.Message.IndexOf("collapsed adjacent vertices", StringComparison.Ordinal) < 0)
                    throw new Exception("Expected the adjacent-vertex precision-collapse guard to reject the tessellation.", error);
                return;
            }

            throw new Exception("Expected adjacent tessellation vertex precision collapse to fail closed.");
        }

        private static void FinalEndpointPrecisionCollapseFailsClosed()
        {
            var start = new Point2(1e12d, 1e12d);
            var end = new Point2(1000000000000.0005d, 1000000000000.001d);
            if (end.Equals(start)) throw new Exception("Final-endpoint collapse fixture requires distinct representable endpoints.");

            try
            {
                BulgeArcTessellator.Tessellate(start, end, 0.5d, 0.1d);
            }
            catch (InvalidOperationException error)
            {
                if (error.Message.IndexOf("collapsed adjacent vertices", StringComparison.Ordinal) < 0)
                    throw new Exception("Expected the final-endpoint precision-collapse guard to reject the tessellation.", error);
                return;
            }

            throw new Exception("Expected final tessellation endpoint precision collapse to fail closed.");
        }

        private static void RepresentableSemicircleStillTessellates()
        {
            var start = new Point2(0d, 0d);
            var end = new Point2(4d, 0d);
            var points = BulgeArcTessellator.Tessellate(start, end, 1d, 0.1d);
            if (points.Count <= 2 || points.Count > 4097) throw new Exception("Expected representable semicircle tessellation.");
            if (!points[0].Equals(start) || !points[points.Count - 1].Equals(end)) throw new Exception("Representable semicircle endpoints changed.");

            for (var index = 1; index < points.Count; index++)
            {
                if (points[index - 1].Equals(points[index]))
                    throw new Exception("Representable semicircle must not contain adjacent duplicate vertices.");
            }
        }
    }
}
