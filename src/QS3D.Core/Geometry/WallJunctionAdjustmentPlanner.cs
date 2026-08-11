using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public enum WallEndpointKind
    {
        Start,
        End
    }

    public sealed class WallEndpointAdjustment
    {
        public WallEndpointAdjustment(string segmentId, WallEndpointKind endpoint, Point2 from, Point2 to, double distance, WallJunctionKind junctionKind, IReadOnlyList<string> junctionSegmentIds)
        {
            SegmentId = segmentId ?? throw new ArgumentNullException(nameof(segmentId));
            Endpoint = endpoint;
            From = from;
            To = to;
            Distance = distance;
            JunctionKind = junctionKind;
            JunctionSegmentIds = junctionSegmentIds ?? throw new ArgumentNullException(nameof(junctionSegmentIds));
        }

        public string SegmentId { get; }
        public WallEndpointKind Endpoint { get; }
        public Point2 From { get; }
        public Point2 To { get; }
        public double Distance { get; }
        public WallJunctionKind JunctionKind { get; }
        public IReadOnlyList<string> JunctionSegmentIds { get; }
    }

    public sealed class WallJunctionAdjustmentPlan
    {
        public WallJunctionAdjustmentPlan(IReadOnlyList<WallJunction> junctions, IReadOnlyList<WallEndpointAdjustment> adjustments)
        {
            Junctions = junctions ?? throw new ArgumentNullException(nameof(junctions));
            Adjustments = adjustments ?? throw new ArgumentNullException(nameof(adjustments));
        }

        public IReadOnlyList<WallJunction> Junctions { get; }
        public IReadOnlyList<WallEndpointAdjustment> Adjustments { get; }
    }

    public sealed class WallJunctionAdjustmentPlanner
    {
        private const int MaxSegments = 10000;

        public WallJunctionAdjustmentPlan Plan(IEnumerable<WallAxisSegment> source, double junctionTolerance = 0.005d, double movementEpsilon = 1e-9d)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!Finite(junctionTolerance) || junctionTolerance <= 0d) throw new ArgumentOutOfRangeException(nameof(junctionTolerance));
            if (!Finite(movementEpsilon) || movementEpsilon < 0d || movementEpsilon >= junctionTolerance) throw new ArgumentOutOfRangeException(nameof(movementEpsilon));

            var segments = source.Take(MaxSegments + 1).ToList();
            if (segments.Count > MaxSegments)
                throw new InvalidOperationException("Wall junction planning supports at most " + MaxSegments + " segments per batch.");
            var junctions = new WallJunctionPlanner().Plan(segments, junctionTolerance);
            var segmentsById = segments.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var junction in junctions.Where(x => x.SegmentIds.Count > 1))
                foreach (var segmentId in junction.SegmentIds)
                {
                    var segment = segmentsById[segmentId];
                    if (segment.Start.DistanceTo(junction.Point) <= junctionTolerance && segment.End.DistanceTo(junction.Point) <= junctionTolerance)
                        throw new InvalidOperationException("Wall endpoint adjustment would collapse segment " + segment.Id + ".");
                }
            var bySegment = new Dictionary<string, List<WallJunction>>(StringComparer.OrdinalIgnoreCase);
            foreach (var junction in junctions.Where(x => x.SegmentIds.Count > 1 && x.Kind != WallJunctionKind.End))
            {
                foreach (var segmentId in junction.SegmentIds)
                {
                    if (!bySegment.TryGetValue(segmentId, out var list))
                    {
                        list = new List<WallJunction>();
                        bySegment[segmentId] = list;
                    }
                    list.Add(junction);
                }
            }

            var adjustments = new List<WallEndpointAdjustment>();
            foreach (var segment in segments.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!bySegment.TryGetValue(segment.Id, out var candidates)) continue;
                AddAdjustment(segment, WallEndpointKind.Start, segment.Start, segment.End, candidates, junctionTolerance, movementEpsilon, adjustments);
                AddAdjustment(segment, WallEndpointKind.End, segment.End, segment.Start, candidates, junctionTolerance, movementEpsilon, adjustments);
            }

            return new WallJunctionAdjustmentPlan(
                junctions,
                adjustments
                    .OrderBy(x => x.SegmentId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Endpoint)
                    .ToList()
                    .AsReadOnly());
        }

        private static void AddAdjustment(WallAxisSegment segment, WallEndpointKind endpointKind, Point2 endpoint, Point2 opposite, IReadOnlyList<WallJunction> candidates, double tolerance, double movementEpsilon, List<WallEndpointAdjustment> result)
        {
            var matches = candidates
                .Select(x => new { Junction = x, Distance = endpoint.DistanceTo(x.Point) })
                .Where(x => x.Distance <= tolerance)
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.Junction.Point.X)
                .ThenBy(x => x.Junction.Point.Y)
                .ToList();
            if (matches.Count == 0) return;

            var best = matches[0];
            if (best.Distance <= movementEpsilon) return;
            if (matches.Count > 1)
            {
                var second = matches[1];
                var separation = best.Junction.Point.DistanceTo(second.Junction.Point);
                if (separation > movementEpsilon && Math.Abs(second.Distance - best.Distance) <= movementEpsilon)
                    throw new InvalidOperationException("Wall endpoint " + segment.Id + "/" + endpointKind + " has ambiguous equally-near junction targets.");
            }

            var remainingLength = opposite.DistanceTo(best.Junction.Point);
            if (!Finite(remainingLength) || remainingLength <= movementEpsilon)
                throw new InvalidOperationException("Wall endpoint adjustment would collapse segment " + segment.Id + ".");

            result.Add(new WallEndpointAdjustment(
                segment.Id,
                endpointKind,
                endpoint,
                best.Junction.Point,
                best.Distance,
                best.Junction.Kind,
                best.Junction.SegmentIds));
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
