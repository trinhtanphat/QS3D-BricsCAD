using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallFootprintAreaOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var result = new WallFootprintEngine().Build(
                new[]
                {
                    new Point2(0d, 0d),
                    new Point2(1e160, 1e160)
                },
                thickness: 1e145);

            if (result.Polygon.Count != 4)
                throw new Exception("Expected a four-vertex footprint for the long straight wall centerline.");
            if (!Finite(result.CenterlineLength) || !(result.CenterlineLength > 0d))
                throw new Exception("Expected a finite positive long wall centerline length.");
            if (!Finite(result.Area) || !(result.Area > 0d))
                throw new Exception("Expected a finite positive wall footprint area after scale-safe determinant cancellation.");
            if (!Finite(result.Perimeter) || !(result.Perimeter > 0d))
                throw new Exception("Expected a finite positive wall footprint perimeter.");
            if (result.UsedBevelJoin)
                throw new Exception("A straight two-point wall footprint must not require a bevel join.");

            foreach (var point in result.Polygon)
                if (!Finite(point.X) || !Finite(point.Y))
                    throw new Exception("Expected finite wall footprint coordinates.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
