using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallFootprintIntersectionOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const double scale = 1e160;
            const double offset = 1e145;
            var centerline = new[]
            {
                new Point2(0d, 0d),
                new Point2(scale, scale),
                new Point2(0d, offset),
                new Point2(scale, scale - offset)
            };

            try
            {
                new WallFootprintEngine().Build(centerline, thickness: 1d);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("centerline self-intersects", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception("Expected large finite crossing to fail at centerline self-intersection validation, got: " + ex.Message);
            }

            throw new Exception("Expected large finite self-crossing wall centerline to be rejected.");
        }
    }
}
