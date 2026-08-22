using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonRegionHoleOrientationOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const double scale = 1e160;
            const double offset = 1e145;
            var firstStart = new Point2(0d, 0d);
            var firstEnd = new Point2(scale, scale);
            var secondStart = new Point2(0d, offset);
            var secondEnd = new Point2(scale, scale - offset);

            var method = typeof(PolygonRegionScanlineClipper).GetMethod(
                "SegmentsIntersect",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new Exception("Expected PolygonRegionScanlineClipper.SegmentsIntersect regression target.");

            object? value;
            try
            {
                value = method.Invoke(null, new object[] { firstStart, firstEnd, secondStart, secondEnd });
            }
            catch (TargetInvocationException ex)
            {
                throw new Exception("Large finite hole-boundary intersection must not fail through determinant overflow.", ex.InnerException ?? ex);
            }

            if (!(value is bool intersects) || !intersects)
                throw new Exception("Expected the long finite near-parallel hole-boundary segments to intersect.");
        }
    }
}
