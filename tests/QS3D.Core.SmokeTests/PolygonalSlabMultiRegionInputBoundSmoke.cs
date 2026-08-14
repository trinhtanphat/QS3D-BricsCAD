using System;
using System.Collections;
using System.Collections.Generic;
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
            ChangingCountInputUsesOneSnapshot();
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

        private static void ChangingCountInputUsesOneSnapshot()
        {
            var regions = new ChangingCountRegionList(Region("R1"));
            var layout = PolygonalSlabMultiRegionMeshPlanner.Plan(Input(regions));

            if (regions.CountReadCount != 1)
                throw new InvalidOperationException("Polygonal slab multi-region planner must snapshot Regions.Count exactly once.");
            if (regions.IndexerReadCount != 1)
                throw new InvalidOperationException("Polygonal slab multi-region planner must copy only the snapshotted region count.");
            if (layout.Regions.Count != 1 ||
                !string.Equals(layout.Regions[0].RegionId, "R1", StringComparison.Ordinal) ||
                layout.Regions[0].Count <= 0)
                throw new InvalidOperationException("Changing Count input did not preserve the initial one-region snapshot.");
        }

        private static void OrdinarySingleRegionStillPlans()
        {
            var layout = PolygonalSlabMultiRegionMeshPlanner.Plan(Input(new[] { Region("R1") }));

            if (layout.Regions.Count != 1 ||
                !string.Equals(layout.Regions[0].RegionId, "R1", StringComparison.Ordinal) ||
                layout.Regions[0].Count <= 0 ||
                layout.TotalBarCount != layout.Regions[0].Count)
                throw new InvalidOperationException("Ordinary polygonal slab multi-region planning changed while adding the region preflight bound.");
        }

        private static PolygonalSlabMultiRegionMeshInput Input(IReadOnlyList<PolygonalSlabMeshRegionInput> regions) =>
            new PolygonalSlabMultiRegionMeshInput
            {
                Regions = regions,
                ThicknessM = 0.2d,
                CoverM = 0.02d,
                XDiameterMm = 12d,
                YDiameterMm = 12d,
                XCount = 2,
                YCount = 2,
                IncludeBottom = true,
                IncludeTop = false
            };

        private static PolygonalSlabMeshRegionInput Region(string id) => new PolygonalSlabMeshRegionInput
        {
            RegionId = id,
            FootprintM = new[]
            {
                new Point2(0d, 0d),
                new Point2(2d, 0d),
                new Point2(2d, 2d),
                new Point2(0d, 2d)
            }
        };

        private sealed class ChangingCountRegionList : IReadOnlyList<PolygonalSlabMeshRegionInput>
        {
            private readonly PolygonalSlabMeshRegionInput _region;

            public ChangingCountRegionList(PolygonalSlabMeshRegionInput region)
            {
                _region = region;
            }

            public int Count
            {
                get
                {
                    CountReadCount++;
                    return CountReadCount == 1 ? 1 : 257;
                }
            }

            public int CountReadCount { get; private set; }
            public int IndexerReadCount { get; private set; }

            public PolygonalSlabMeshRegionInput this[int index]
            {
                get
                {
                    IndexerReadCount++;
                    return _region;
                }
            }

            public IEnumerator<PolygonalSlabMeshRegionInput> GetEnumerator()
            {
                yield return _region;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
