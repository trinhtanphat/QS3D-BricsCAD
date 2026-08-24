using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class PolygonSourceLoop2
    {
        public PolygonSourceLoop2(string sourceId, IReadOnlyList<Point2> vertices)
        {
            SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        }

        public string SourceId { get; }
        public IReadOnlyList<Point2> Vertices { get; }
    }

    public sealed class PolygonSourceRegion2
    {
        internal PolygonSourceRegion2(
            string regionId,
            string outerSourceId,
            IReadOnlyList<string> holeSourceIds,
            PolygonRegion2 region)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            OuterSourceId = outerSourceId ?? throw new ArgumentNullException(nameof(outerSourceId));
            HoleSourceIds = holeSourceIds ?? throw new ArgumentNullException(nameof(holeSourceIds));
            Region = region ?? throw new ArgumentNullException(nameof(region));
        }

        public string RegionId { get; }
        public string OuterSourceId { get; }
        public IReadOnlyList<string> HoleSourceIds { get; }
        public PolygonRegion2 Region { get; }
    }

    public sealed class PolygonSourceRegionAssembly2
    {
        internal PolygonSourceRegionAssembly2(
            IReadOnlyList<PolygonSourceRegion2> regions,
            PolygonRegionSet2 regionSet)
        {
            Regions = regions ?? throw new ArgumentNullException(nameof(regions));
            RegionSet = regionSet ?? throw new ArgumentNullException(nameof(regionSet));
        }

        public IReadOnlyList<PolygonSourceRegion2> Regions { get; }
        public PolygonRegionSet2 RegionSet { get; }
    }

    /// <summary>
    /// Deterministically turns a complete set of closed source loops into disconnected
    /// polygon regions with zero or more holes. Region identity is derived only from
    /// the canonical outer-loop source identity so selection order cannot change it.
    /// </summary>
    public static class PolygonSourceLoopRegionAssembler
    {
        private const int MaxSourceLoops = 1024;
        private const int MaxSourceIdLength = 160;

        private sealed class NormalizedLoop
        {
            public NormalizedLoop(string sourceId, IReadOnlyList<Point2> vertices)
            {
                SourceId = sourceId;
                Vertices = vertices;
            }

            public string SourceId { get; }
            public IReadOnlyList<Point2> Vertices { get; }
        }

        public static PolygonSourceRegionAssembly2 Assemble(IEnumerable<PolygonSourceLoop2> sourceLoops)
        {
            if (sourceLoops == null) throw new ArgumentNullException(nameof(sourceLoops));

            var materialized = sourceLoops.Take(MaxSourceLoops + 1).ToList();
            if (materialized.Count == 0)
                throw new ArgumentException("Polygon source-loop assembly requires at least one loop.", nameof(sourceLoops));
            if (materialized.Count > MaxSourceLoops)
                throw new ArgumentException(
                    "Polygon source-loop assembly exceeds the supported " + MaxSourceLoops + " loop limit.",
                    nameof(sourceLoops));

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var loops = new List<NormalizedLoop>(materialized.Count);
            for (var i = 0; i < materialized.Count; i++)
            {
                var source = materialized[i] ??
                    throw new ArgumentException("Polygon source loop cannot be null at index " + i + ".", nameof(sourceLoops));
                var sourceId = NormalizeSourceId(source.SourceId, i);
                if (!ids.Add(sourceId))
                    throw new ArgumentException("Polygon source loop identity is duplicated: " + sourceId + ".", nameof(sourceLoops));

                // Reuse the existing bounded polygon validator. This rejects non-finite,
                // degenerate and self-intersecting loops before pairwise classification.
                var normalized = PolygonRegionScanlineClipper.NormalizeAndValidate(source.Vertices).Outer;
                loops.Add(new NormalizedLoop(sourceId, normalized));
            }

            loops.Sort((left, right) => StringComparer.Ordinal.Compare(left.SourceId, right.SourceId));

            // A loop with no strict container is an outer. A loop with exactly one
            // strict container is that outer's hole. More than one strict container
            // is unsupported deeper nesting (outer -> hole -> island) or ambiguous
            // ownership and must fail closed.
            var containers = new List<int>[loops.Count];
            for (var i = 0; i < loops.Count; i++)
                containers[i] = new List<int>();

            for (var outerIndex = 0; outerIndex < loops.Count; outerIndex++)
            {
                for (var innerIndex = 0; innerIndex < loops.Count; innerIndex++)
                {
                    if (outerIndex == innerIndex) continue;
                    if (StrictlyContains(loops[outerIndex].Vertices, loops[innerIndex].Vertices))
                        containers[innerIndex].Add(outerIndex);
                }
            }

            for (var i = 0; i < containers.Length; i++)
            {
                if (containers[i].Count <= 1) continue;
                throw new ArgumentException(
                    "Polygon source loop " + loops[i].SourceId +
                    " is nested inside more than one loop. Deeper nesting and ambiguous ownership are unsupported.",
                    nameof(sourceLoops));
            }

            var seeds = new List<PolygonRegionSeed2>();
            var sourceByRegion = new Dictionary<string, Tuple<string, IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase);
            for (var outerIndex = 0; outerIndex < loops.Count; outerIndex++)
            {
                if (containers[outerIndex].Count != 0) continue;

                var outer = loops[outerIndex];
                var holes = new List<NormalizedLoop>();
                for (var i = 0; i < loops.Count; i++)
                {
                    if (containers[i].Count == 1 && containers[i][0] == outerIndex)
                        holes.Add(loops[i]);
                }
                holes.Sort((left, right) => StringComparer.Ordinal.Compare(left.SourceId, right.SourceId));

                var holeVertices = holes
                    .Select(x => (IReadOnlyList<Point2>)x.Vertices)
                    .ToList()
                    .AsReadOnly();
                var holeIds = holes.Select(x => x.SourceId).ToList().AsReadOnly();

                // Stable RegionId contract: canonical outer source identity.
                seeds.Add(new PolygonRegionSeed2(outer.SourceId, outer.Vertices, holeVertices));
                sourceByRegion.Add(outer.SourceId, Tuple.Create(outer.SourceId, (IReadOnlyList<string>)holeIds));
            }

            var regionSet = PolygonRegionSetTopology.NormalizeAndValidate(seeds);
            var regions = new List<PolygonSourceRegion2>(regionSet.Islands.Count);
            foreach (var island in regionSet.Islands)
            {
                var source = sourceByRegion[island.RegionId];
                regions.Add(new PolygonSourceRegion2(
                    island.RegionId,
                    source.Item1,
                    source.Item2,
                    island.Region));
            }

            return new PolygonSourceRegionAssembly2(regions.AsReadOnly(), regionSet);
        }

        private static string NormalizeSourceId(string sourceId, int index)
        {
            var normalized = (sourceId ?? string.Empty).Trim();
            if (normalized.Length == 0)
                throw new ArgumentException("Polygon source loop identity is required at index " + index + ".");
            if (normalized.Length > MaxSourceIdLength)
                throw new ArgumentException(
                    "Polygon source loop identity exceeds the supported " + MaxSourceIdLength + " character limit.");
            if (normalized.Any(char.IsControl))
                throw new ArgumentException("Polygon source loop identity contains control characters.");

            return normalized.ToUpperInvariant();
        }

        private static bool StrictlyContains(
            IReadOnlyList<Point2> possibleOuter,
            IReadOnlyList<Point2> possibleHole)
        {
            try
            {
                PolygonRegionScanlineClipper.NormalizeAndValidate(
                    possibleOuter,
                    new[] { possibleHole });
                return true;
            }
            catch (ArgumentException)
            {
                // Both loops were already validated independently. An ArgumentException
                // here therefore means the second loop is not a strict, disjoint hole of
                // the first (outside, touching, crossing, overlapping, or nested invalidly).
                return false;
            }
        }
    }
}
