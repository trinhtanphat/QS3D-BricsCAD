using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridLinePointProjectionOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const double scale = 1e160;
            var shared = new Point2(scale, scale);
            var curves = new[]
            {
                GridReferenceCurve.Line("A", new Point2(0d, 0d), shared),
                GridReferenceCurve.Line("B", shared, new Point2(2e160, 2e160))
            };

            var intersections = GridIntersectionPlanner.FindIntersections(curves, tolerance: 1e-15d);
            if (intersections.Count != 1)
                throw new Exception("Expected exactly one shared-endpoint Grid LINE intersection.");

            var intersection = intersections[0];
            if (!Finite(intersection.Point.X) || !Finite(intersection.Point.Y))
                throw new Exception("Expected finite shared-endpoint Grid intersection coordinates.");
            if (intersection.Point.X != shared.X || intersection.Point.Y != shared.Y)
                throw new Exception("Expected the exact finite shared endpoint to be returned.");
            if (!string.Equals(intersection.FirstElementId, "A", StringComparison.Ordinal) ||
                !string.Equals(intersection.SecondElementId, "B", StringComparison.Ordinal))
                throw new Exception("Expected shared-endpoint intersection pair identity to remain deterministic.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
