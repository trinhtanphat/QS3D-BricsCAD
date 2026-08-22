using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public sealed class PolygonalSlabMeshRegionInput
    {
        public string RegionId { get; set; } = string.Empty;
        public IReadOnlyList<Point2> FootprintM { get; set; } = Array.Empty<Point2>();
        public IReadOnlyList<IReadOnlyList<Point2>> HoleFootprintsM { get; set; } = Array.Empty<IReadOnlyList<Point2>>();
    }

    public sealed class PolygonalSlabMultiRegionMeshInput
    {
        public IReadOnlyList<PolygonalSlabMeshRegionInput> Regions { get; set; } = Array.Empty<PolygonalSlabMeshRegionInput>();
        public double ThicknessM { get; set; }
        public double CoverM { get; set; }
        public double XDiameterMm { get; set; }
        public double YDiameterMm { get; set; }
        public double? XSpacingMm { get; set; }
        public int? XCount { get; set; }
        public double? YSpacingMm { get; set; }
        public int? YCount { get; set; }
        public bool IncludeBottom { get; set; } = true;
        public bool IncludeTop { get; set; }
        public bool XClosestToFace { get; set; } = true;
    }

    public sealed class PolygonalSlabMeshRegionLayout
    {
        internal PolygonalSlabMeshRegionLayout(string regionId, PolygonalSlabMeshLayout layout)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        public string RegionId { get; }
        public PolygonalSlabMeshLayout Layout { get; }
        public int Count => Layout.Count;
    }

    public sealed class PolygonalSlabMultiRegionMeshLayout
    {
        internal PolygonalSlabMultiRegionMeshLayout(IReadOnlyList<PolygonalSlabMeshRegionLayout> regions, int totalBarCount)
        {
            Regions = regions ?? throw new ArgumentNullException(nameof(regions));
            TotalBarCount = totalBarCount;
        }

        public IReadOnlyList<PolygonalSlabMeshRegionLayout> Regions { get; }
        public int TotalBarCount { get; }
    }

    public static class PolygonalSlabMultiRegionMeshPlanner
    {
        private const int MaxRegions = 256;
        private const int MaxTotalBars = 32768;

        public static PolygonalSlabMultiRegionMeshLayout Plan(PolygonalSlabMultiRegionMeshInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Regions == null) throw new ArgumentNullException(nameof(input.Regions));
            var regionCount = input.Regions.Count;
            if (regionCount > MaxRegions)
                throw new ArgumentException("Polygonal slab multi-region mesh exceeds the supported " + MaxRegions + " region limit.", nameof(input.Regions));

            var seeds = new List<PolygonRegionSeed2>(regionCount);
            for (var index = 0; index < regionCount; index++)
            {
                var region = input.Regions[index] ?? throw new ArgumentException("Polygonal slab multi-region input cannot contain a null region at index " + index + ".", nameof(input.Regions));
                if (region.FootprintM == null) throw new ArgumentException("Polygonal slab multi-region footprint cannot be null at index " + index + ".", nameof(input.Regions));
                if (region.HoleFootprintsM == null) throw new ArgumentException("Polygonal slab multi-region holes cannot be null at index " + index + ".", nameof(input.Regions));
                seeds.Add(new PolygonRegionSeed2(region.RegionId, region.FootprintM, region.HoleFootprintsM));
            }

            var topology = PolygonRegionSetTopology.NormalizeAndValidate(seeds);
            var layouts = new List<PolygonalSlabMeshRegionLayout>(topology.Islands.Count);
            var totalBarCount = 0;

            foreach (var island in topology.Islands)
            {
                var layout = PolygonalSlabMeshPlanner.Plan(new PolygonalSlabMeshInput
                {
                    FootprintM = island.Region.Outer,
                    HoleFootprintsM = island.Region.Holes,
                    ThicknessM = input.ThicknessM,
                    CoverM = input.CoverM,
                    XDiameterMm = input.XDiameterMm,
                    YDiameterMm = input.YDiameterMm,
                    XSpacingMm = input.XSpacingMm,
                    XCount = input.XCount,
                    YSpacingMm = input.YSpacingMm,
                    YCount = input.YCount,
                    IncludeBottom = input.IncludeBottom,
                    IncludeTop = input.IncludeTop,
                    XClosestToFace = input.XClosestToFace
                });

                totalBarCount = checked(totalBarCount + layout.Count);
                if (totalBarCount > MaxTotalBars)
                    throw new InvalidOperationException("Polygonal slab multi-region mesh exceeds the supported " + MaxTotalBars + " total bar limit.");
                layouts.Add(new PolygonalSlabMeshRegionLayout(island.RegionId, layout));
            }

            if (layouts.Count == 0)
                throw new InvalidOperationException("Polygonal slab multi-region mesh produced no region layouts.");
            if (layouts.Any(x => x.Layout.Count == 0))
                throw new InvalidOperationException("Polygonal slab multi-region mesh produced an empty region layout.");

            return new PolygonalSlabMultiRegionMeshLayout(layouts.AsReadOnly(), totalBarCount);
        }
    }
}
