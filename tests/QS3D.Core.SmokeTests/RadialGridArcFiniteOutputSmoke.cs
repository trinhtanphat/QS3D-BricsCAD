using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RadialGridArcFiniteOutputSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var input = new RadialGridSystemInput
            {
                CenterM = new Point2(1e308, 0d),
                InnerRadiusM = 0d,
                OuterRadiusM = 1e308,
                Rays = new[] { new GridAngularStation("RAY", Math.PI) },
                Rings = new[] { new GridRadialStation("RING", 1e308) }
            };

            try
            {
                GridSystemPlanner.PlanRadial(input);
            }
            catch (OverflowException)
            {
                return;
            }

            throw new Exception("Expected radial Grid planning to reject a ring whose computed ARC endpoint is non-finite.");
        }
    }
}
