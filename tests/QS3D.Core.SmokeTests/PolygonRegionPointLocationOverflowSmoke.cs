using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonRegionPointLocationOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const double scale = 1e160;
            const double outerHalfWidth = 1e147;
            const double innerHalfWidth = 1e145;
            const double innerStart = 2e159;
            const double innerEnd = 8e159;

            var outer = new[]
            {
                new Point2(0d, -outerHalfWidth),
                new Point2(scale, scale - outerHalfWidth),
                new Point2(scale, scale + outerHalfWidth),
                new Point2(0d, outerHalfWidth)
            };
            var nested = new[]
            {
                new Point2(innerStart, innerStart - innerHalfWidth),
                new Point2(innerEnd, innerEnd - innerHalfWidth),
                new Point2(innerEnd, innerEnd + innerHalfWidth),
                new Point2(innerStart, innerStart + innerHalfWidth)
            };

            try
            {
                PolygonRegionSetTopology.NormalizeAndValidate(new[]
                {
                    new PolygonRegionSeed2("OUTER", outer),
                    new PolygonRegionSeed2("NESTED", nested)
                });
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("overlap or are nested", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception("Expected nested large-coordinate islands to reach the explicit nesting policy, got: " + ex.Message);
            }

            throw new Exception("Expected nested polygon islands to be rejected by the explicit ownership/topology policy.");
        }
    }
}
