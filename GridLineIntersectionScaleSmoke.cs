using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridLineIntersectionScaleSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const double scale = 1e160;
            const double offset = 1e147;
            var curves = new[]
            {
                GridReferenceCurve.Line("A", new Point2(0d, 0d), new Point2(scale, scale)),
                GridReferenceCurve.Line("B", new Point2(0d, offset), new Point2(scale, scale - offset))
            };

            var intersections = GridIntersectionPlanner.FindIntersections(curves, tolerance: 1e-15d);
            if (intersections.Count != 1)
                throw new Exception("Expected exactly one representable large Grid LINE intersection.");

            var intersection = intersections[0];
            if (!string.Equals(intersection.FirstElementId, "A", StringComparison.Ordinal) ||
                !string.Equals(intersection.SecondElementId, "B", StringComparison.Ordinal))
                throw new Exception("Expected Grid LINE intersection ownership to preserve input pair identity.");
            if (!Finite(intersection.Point.X) || !Finite(intersection.Point.Y))
                throw new Exception("Expected finite large Grid LINE intersection coordinates.");

            var expected = scale * 0.5d;
            if (Math.Abs(intersection.Point.X / expected - 1d) > 1e-12d ||
                Math.Abs(intersection.Point.Y / expected - 1d) > 1e-12d)
                throw new Exception("Expected the large Grid LINE intersection near the shared midpoint.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
