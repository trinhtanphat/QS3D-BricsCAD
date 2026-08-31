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
        internal PolygonSourceRegion2(string regionId, string outerSourceId, IReadOnlyList<string> holeSourceIds, PolygonRegion2 region)
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
        internal PolygonSourceRegionAssembly2(IReadOnlyList<PolygonSourceRegion2> regions, PolygonRegionSet2 regionSet)
        {
            Regions = regions ?? throw new ArgumentNullException(nameof(regions));
            RegionSet = regionSet ?? throw new ArgumentNullException(nameof(regionSet));
        }

        public IReadOnlyList<PolygonSourceRegion2> Regions { get; }
        public PolygonRegionSet2 RegionSet { get; }
    }

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

            var knownCount = ResolveKnownCount(sourceLoops);
            var materialized = new List<PolygonSourceLoop2>(knownCount ?? 0);
            using (var enumerator = sourceLoops.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCount(sourceLoops, knownCount, "before MoveNext");
                    var moved = enumerator.MoveNext();
                    RequireStableKnownCount(sourceLoops, knownCount, "after MoveNext");
                    if (!moved) break;
                    if (knownCount.HasValue && materialized.Count >= knownCount.Value)
                        throw new ArgumentException("Polygon source-loop sequence yielded more loops than its declared Count.", nameof(sourceLoops));
                    if (materialized.Count >= MaxSourceLoops)
                        throw new ArgumentException("Polygon source-loop assembly exceeds the supported " + MaxSourceLoops + " loop limit.", nameof(sourceLoops));
                    var current = enumerator.Current;
                    RequireStableKnownCount(sourceLoops, knownCount, "after Current");
                    materialized.Add(current);
                }
            }

            if (knownCount.HasValue && materialized.Count != knownCount.Value)
                throw new ArgumentException("Polygon source-loop sequence yielded fewer loops than its declared Count.", nameof(sourceLoops));
            RequireStableKnownCount(sourceLoops, knownCount, "after traversal");
            if (materialized.Count == 0)
                throw new ArgumentException("Polygon source-loop assembly requires at least one loop.", nameof(sourceLoops));

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var loops = new List<NormalizedLoop>(materialized.Count);
            for (var i = 0; i < materialized.Count; i++)
            {
                var source = materialized[i] ?? throw new ArgumentException("Polygon source loop cannot be null at index " + i + ".", nameof(sourceLoops));
                var sourceId = NormalizeSourceId(source.SourceId, i);
                if (!ids.Add(sourceId))
                    throw new ArgumentException("Polygon source loop identity is duplicated: " + sourceId + ".", nameof(sourceLoops));
                var normalized = PolygonRegionScanlineClipper.NormalizeAndValidate(source.Vertices).Outer;
                loops.Add(new NormalizedLoop(sourceId, normalized));
            }

            loops.Sort((left, right) => StringComparer.Ordinal.Compare(left.SourceId, right.SourceId));
            var containers = new List<int>[loops.Count];
            for (var i = 0; i < loops.Count; i++) containers[i] = new List<int>();

            for (var outerIndex = 0; outerIndex < loops.Count; outerIndex++)
            {
                for (var innerIndex = 0; innerIndex < loops.Count; innerIndex++)
                {
                    if (outerIndex == innerIndex) continue;
                    if (StrictlyContains(loops[outerIndex].Vertices, loops[innerIndex].Vertices)) containers[innerIndex].Add(outerIndex);
                }
            }

            for (var i = 0; i < containers.Length; i++)
            {
                if (containers[i].Count <= 1) continue;
                throw new ArgumentException("Polygon source loop " + loops[i].SourceId + " is nested inside more than one loop. Deeper nesting and ambiguous ownership are unsupported.", nameof(sourceLoops));
            }

            var seeds = new List<PolygonRegionSeed2>();
            var sourceByRegion = new Dictionary<string, Tuple<string, IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase);
            for (var outerIndex = 0; outerIndex < loops.Count; outerIndex++)
            {
                if (containers[outerIndex].Count != 0) continue;
                var outer = loops[outerIndex];
                var holes = new List<NormalizedLoop>();
                for (var i = 0; i < loops.Count; i++)
                    if (containers[i].Count == 1 && containers[i][0] == outerIndex) holes.Add(loops[i]);
                holes.Sort((left, right) => StringComparer.Ordinal.Compare(left.SourceId, right.SourceId));
                var holeVertices = holes.Select(x => (IReadOnlyList<Point2>)x.Vertices).ToList().AsReadOnly();
                var holeIds = holes.Select(x => x.SourceId).ToList().AsReadOnly();
                seeds.Add(new PolygonRegionSeed2(outer.SourceId, outer.Vertices, holeVertices));
                sourceByRegion.Add(outer.SourceId, Tuple.Create(outer.SourceId, (IReadOnlyList<string>)holeIds));
            }

            var regionSet = PolygonRegionSetTopology.NormalizeAndValidate(seeds);
            var regions = new List<PolygonSourceRegion2>(regionSet.Islands.Count);
            foreach (var island in regionSet.Islands)
            {
                var source = sourceByRegion[island.RegionId];
                regions.Add(new PolygonSourceRegion2(island.RegionId, source.Item1, source.Item2, island.Region));
            }
            return new PolygonSourceRegionAssembly2(regions.AsReadOnly(), regionSet);
        }

        private static int? ResolveKnownCount(IEnumerable<PolygonSourceLoop2> sourceLoops)
        {
            int? known = null;
            CaptureKnownCount((sourceLoops as ICollection<PolygonSourceLoop2>)?.Count, ref known);
            CaptureKnownCount((sourceLoops as IReadOnlyCollection<PolygonSourceLoop2>)?.Count, ref known);
            CaptureKnownCount((sourceLoops as System.Collections.ICollection)?.Count, ref known);
            if (known.HasValue && known.Value > MaxSourceLoops)
                throw new ArgumentException("Polygon source-loop assembly exceeds the supported " + MaxSourceLoops + " loop limit.", nameof(sourceLoops));
            return known;
        }

        private static void RequireStableKnownCount(IEnumerable<PolygonSourceLoop2> sourceLoops, int? admittedCount, string boundary)
        {
            if (!admittedCount.HasValue) return;
            var rebound = ResolveKnownCount(sourceLoops);
            if (!rebound.HasValue || rebound.Value != admittedCount.Value)
                throw new ArgumentException("Polygon source-loop sequence known Count changed or conflicted " + boundary + ".", nameof(sourceLoops));
        }

        private static void CaptureKnownCount(int? candidate, ref int? known)
        {
            if (!candidate.HasValue) return;
            if (candidate.Value < 0)
                throw new ArgumentException("Polygon source-loop sequence reports an invalid negative Count.", "sourceLoops");
            if (known.HasValue && known.Value != candidate.Value)
                throw new ArgumentException("Polygon source-loop sequence exposes conflicting known Count values.", "sourceLoops");
            known = candidate.Value;
        }

        private static string NormalizeSourceId(string sourceId, int index)
        {
            var normalized = (sourceId ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Polygon source loop identity is required at index " + index + ".");
            if (normalized.Length > MaxSourceIdLength) throw new ArgumentException("Polygon source loop identity exceeds the supported " + MaxSourceIdLength + " character limit.");
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Polygon source loop identity contains control characters.");
            return normalized.ToUpperInvariant();
        }

        private static bool StrictlyContains(IReadOnlyList<Point2> possibleOuter, IReadOnlyList<Point2> possibleHole)
        {
            try
            {
                PolygonRegionScanlineClipper.NormalizeAndValidate(possibleOuter, new[] { possibleHole });
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
