using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonalSlabMultiRegionInputBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OversizedRegionInputFailsClosed();
            OrdinarySingleRegionStillPlans();
        }

        private static void OversizedRegionInputFailsClosed()
        {
            var region = new PolygonalSlabMeshRegionInput { RegionId = "R" };
            var input = new PolygonalSlabMultiRegionMeshInput
            {
                Regions = Enumerable.Repeat(region, 257).ToArray()
            };

            try
            {
                PolygonalSlabMultiRegionMeshPlanner.Plan(input);
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("supported 256 region limit", StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Polygonal slab region preflight failed for the wrong reason.", ex);
            }
            throw new InvalidOperationException("Polygonal slab multi-region planner accepted more regions than topology supports.");
        }

        private static void OrdinarySingleRegionStillPlans()
        {
            var region = new PolygonalSlabMeshRegionInput
            {
                RegionId = "R1",
                FootprintM = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(2d, 0d),
                    new Point2(2d, 2d),
                    new Point2(0d, 2d)
                }
            };
            var layout = PolygonalSlabMultiRegionMeshPlanner.Plan(new PolygonalSlabMultiRegionMeshInput
            {
                Regions = new[] { region },
                ThicknessM = 0.2d,
                CoverM = 0.02d,
                XDiameterMm = 12d,
                YDiameterMm = 12d,
                XCount = 2,
                YCount = 2,
                IncludeBottom = true,
                IncludeTop = false
            });

            if (layout.Regions.Count != 1 ||
                !string.Equals(layout.Regions[0].RegionId, "R1", StringComparison.Ordinal) ||
                layout.Regions[0].Count <= 0 ||
                layout.TotalBarCount != layout.Regions[0].Count)
                throw new InvalidOperationException("Ordinary polygonal slab multi-region planning changed while adding the region preflight bound.");
        }
    }
}
