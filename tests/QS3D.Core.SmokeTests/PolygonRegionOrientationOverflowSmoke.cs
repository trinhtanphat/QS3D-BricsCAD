using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonRegionOrientationOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const double scale = 1e160;
            const double halfWidth = 1e144;
            const double intercept = 1e145;
            const double drift = 2e145;

            var first = new[]
            {
                new Point2(0d, -halfWidth),
                new Point2(scale, scale - halfWidth),
                new Point2(scale, scale + halfWidth),
                new Point2(0d, halfWidth)
            };
            var secondEndCenter = scale + intercept - drift;
            var second = new[]
            {
                new Point2(0d, intercept - halfWidth),
                new Point2(scale, secondEndCenter - halfWidth),
                new Point2(scale, secondEndCenter + halfWidth),
                new Point2(0d, intercept + halfWidth)
            };

            try
            {
                PolygonRegionSetTopology.NormalizeAndValidate(new[]
                {
                    new PolygonRegionSeed2("A", first),
                    new PolygonRegionSeed2("B", second)
                });
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("intersect or touch", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception("Expected the crossing large-coordinate islands to fail the explicit intersect/touch policy, got: " + ex.Message);
            }

            throw new Exception("Expected crossing large-coordinate polygon islands to be rejected.");
        }
    }
}
