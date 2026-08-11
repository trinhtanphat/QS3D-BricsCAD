using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularGridExtentOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var input = new RectangularGridSystemInput
            {
                OriginM = new Point2(0d, 0d),
                UAxis = new Point2(1d, 0d),
                VAxis = new Point2(0d, 1d),
                UMinM = -1e308,
                UMaxM = 1e308,
                VMinM = -1d,
                VMaxM = 1d,
                UStations = new[] { new GridLinearStation("U0", 0d) },
                VStations = new[] { new GridLinearStation("V0", 0d) }
            };

            try
            {
                GridSystemPlanner.PlanRectangular(input);
            }
            catch (OverflowException)
            {
                return;
            }

            throw new Exception("Expected rectangular Grid planning to reject a non-finite extent span before emitting unsupported lines.");
        }
    }
}
