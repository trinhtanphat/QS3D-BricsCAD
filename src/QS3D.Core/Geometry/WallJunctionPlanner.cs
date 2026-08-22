using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public enum WallJunctionKind
    {
        End,
        Straight,
        L,
        T,
        X,
        Multi
    }

    public sealed class WallAxisSegment
    {
        public WallAxisSegment(string id, Point2 start, Point2 end)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Wall segment id is required.", nameof(id));
            Id = id.Trim();
            Start = start;
            End = end;
        }

        public string Id { get; }
        public Point2 Start { get; }
        public Point2 End { get; }
    }

    public sealed class WallJunction
    {
        public WallJunction(Point2 point, WallJunctionKind kind, IReadOnlyList<string> segmentIds, int rayCount)
        {
            Point = point;
            Kind = kind;
            SegmentIds = segmentIds ?? throw new ArgumentNullException(nameof(segmentIds));
            RayCount = rayCount;
        }

        public Point2 Point { get; }
        public WallJunctionKind Kind { get; }
        public IReadOnlyList<string> SegmentIds { get; }
        public int RayCount { get; }
    }

    public sealed class WallJunctionPlanner
    {
        private const int MaxSegments = 10000;
        private const double ParallelDirectionEpsilon = 1e-12d;

        private sealed class SegmentInfo
        {
            public WallAxisSegment Segment { get; set; } = null!;
            public double MinX { get; set; }
            public double MaxX { get; set; }
            public double MinY { get; set; }
            public double MaxY { get; set; }
            public double Dx { get; set; }
            public double Dy { get; set; }
            public double Ux { get; set; }
            public double Uy { get; set; }
            public double Length { get; set; }
        }

        private sealed class Candidate
        {
            public Point2 Point { get; set; }
            public HashSet<string> SegmentIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public CellKey(long x, long y) { X = x; Y = y; }
            public long X { get; }
            public long Y { get; }
            public bool Equals(CellKey other) => X == other.X && Y == other.Y;
            public override bool Equals(object? obj) => obj is CellKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked { return (X.GetHashCode() * 397) ^ Y.GetHashCode(); }
            }
        }

        private sealed class CandidateIndex
        {
            private readonly double _tolerance;
            private readonly Dictionary<CellKey, List<Candidate>> _buckets = new Dictionary<CellKey, List<Candidate>>();
            private readonly List<Candidate> _unindexed = new List<Candidate>();
            private readonly List<Candidate> _all = new List<Candidate>();

            public CandidateIndex(double tolerance) { _tolerance = tolerance; }
            public IReadOnlyList<Candidate> All => _all;

            public void Add(Point2 point, string segmentId)
            {
                Candidate? best = null;
                var bestDistance = double.PositiveInfinity;
                if (TryCell(point, out var key))
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            var neighbor = new CellKey(key.X + dx, key.Y + dy);
                            if (!_buckets.TryGetValue(neighbor, out var bucket)) continue;
                            FindBest(bucket, point, ref best, ref bestDistance);
                        }
                    }
                    FindBest(_unindexed, point, ref best, ref bestDistance);
                }
                else
                {
                    FindBest(_all, point, ref best, ref bestDistance);
                }

                if (best == null)
                {
                    best = new Candidate { Point = point };
                    _all.Add(best);
                    if (TryCell(point, out key))
                    {
                        if (!_buckets.TryGetValue(key, out var bucket))
                        {
                            bucket = new List<Candidate>();
                            _buckets[key] = bucket;
                        }
                        bucket.Add(best);
                    }
                    else
                    {
                        _unindexed.Add(best);
                    }
                }
                best.SegmentIds.Add(segmentId);
            }

            private void FindBest(IEnumerable<Candidate> candidates, Point2 point, ref Candidate? best, ref double bestDistance)
            {
                foreach (var candidate in candidates)
                {
                    var distance = candidate.Point.DistanceTo(point);
                    if (distance <= _tolerance && distance < bestDistance)
                    {
                        best = candidate;
                        bestDistance = distance;
                    }
                }
            }

            private bool TryCell(Point2 point, out CellKey key)
            {
                if (TryQuantize(point.X, _tolerance, out var x) && TryQuantize(point.Y, _tolerance, out var y))
                {
                    key = new CellKey(x, y);
                    return true;
                }
                key = default;
                return false;
            }

            private static bool TryQuantize(double value, double tolerance, out long cell)
            {
                var scaled = value / tolerance;
                if (!double.IsNaN(scaled) && !double.IsInfinity(scaled) && scaled > long.MinValue + 2d && scaled < long.MaxValue - 2d)
                {
                    cell = (long)Math.Floor(scaled);
                    return true;
                }
                cell = 0L;
                return false;
            }
        }

        public IReadOnlyList<WallJunction> Plan(IEnumerable<WallAxisSegment> source, double tolerance = 0.005d, double angularToleranceRadians = 1e-4d)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!Finite(tolerance) || tolerance <= 0d) throw new ArgumentOutOfRangeException(nameof(tolerance));
            if (!Finite(angularToleranceRadians) || angularToleranceRadians <= 0d || angularToleranceRadians >= Math.PI / 4d)
                throw new ArgumentOutOfRangeException(nameof(angularToleranceRadians));

            var raw = source.Take(MaxSegments + 1).ToList();
            if (raw.Count > MaxSegments) throw new InvalidOperationException("Wall junction planning supports at most " + MaxSegments.ToString(CultureInfo.InvariantCulture) + " segments per batch.");
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var segments = new List<SegmentInfo>(raw.Count);
            foreach (var segment in raw)
            {
                if (segment == null) throw new ArgumentException("Wall segment collection contains null.", nameof(source));
                if (!seenIds.Add(segment.Id)) throw new InvalidOperationException("Duplicate wall segment id: " + segment.Id);
                Validate(segment.Start, segment.Id + "/start");
                Validate(segment.End, segment.Id + "/end");
                var dx = SubtractFinite(segment.End.X, segment.Start.X, segment.Id + " dx");
                var dy = SubtractFinite(segment.End.Y, segment.Start.Y, segment.Id + " dy");
                var length = segment.Start.DistanceTo(segment.End);
                var minimumLength = tolerance * 1e-6d;
                if (!Finite(minimumLength) || length <= minimumLength) throw new InvalidOperationException("Degenerate wall segment: " + segment.Id);
                var ux = dx / length;
                var uy = dy / length;
                if (!Finite(ux) || !Finite(uy)) throw new OverflowException("Wall direction normalization overflowed: " + segment.Id);
                segments.Add(new SegmentInfo
                {
                    Segment = segment,
                    MinX = Math.Min(segment.Start.X, segment.End.X),
                    MaxX = Math.Max(segment.Start.X, segment.End.X),
                    MinY = Math.Min(segment.Start.Y, segment.End.Y),
                    MaxY = Math.Max(segment.Start.Y, segment.End.Y),
                    Dx = dx,
                    Dy = dy,
                    Ux = ux,
                    Uy = uy,
                    Length = length
                });
            }
            if (segments.Count == 0) return Array.Empty<WallJunction>();

            var candidates = new CandidateIndex(tolerance);
            foreach (var segment in segments)
            {
                candidates.Add(segment.Segment.Start, segment.Segment.Id);
                candidates.Add(segment.Segment.End, segment.Segment.Id);
            }

            var ordered = segments.OrderBy(x => x.MinX).ThenBy(x => x.MinY).ThenBy(x => x.Segment.Id, StringComparer.OrdinalIgnoreCase).ToList();
            var active = new List<SegmentInfo>();
            foreach (var current in ordered)
            {
                for (var index = active.Count - 1; index >= 0; index--)
                    if (active[index].MaxX < current.MinX - tolerance) active.RemoveAt(index);

                foreach (var other in active)
                {
                    if (other.MaxY < current.MinY - tolerance || current.MaxY < other.MinY - tolerance) continue;
                    foreach (var point in Intersections(other, current, tolerance))
                    {
                        candidates.Add(point, other.Segment.Id);
                        candidates.Add(point, current.Segment.Id);
                    }
                }
                active.Add(current);
            }

            var byId = segments.ToDictionary(x => x.Segment.Id, StringComparer.OrdinalIgnoreCase);
            var result = new List<WallJunction>();
            foreach (var candidate in candidates.All)
            {
                var incident = candidate.SegmentIds;
                var rays = new List<Point2>();
                foreach (var id in incident.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    AddRays(candidate.Point, byId[id], tolerance, rays);
                var uniqueRays = MergeDirections(rays, angularToleranceRadians);
                var kind = Classify(uniqueRays);
                result.Add(new WallJunction(
                    candidate.Point,
                    kind,
                    incident.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly(),
                    uniqueRays.Count));
            }

            return result
                .Where(x => x.SegmentIds.Count > 1 || x.Kind == WallJunctionKind.End)
                .OrderBy(x => x.Point.X)
                .ThenBy(x => x.Point.Y)
                .ThenBy(x => x.Kind)
                .ToList()
                .AsReadOnly();
        }

        private static IEnumerable<Point2> Intersections(SegmentInfo a, SegmentInfo b, double tolerance)
        {
            var determinant = CrossFinite(a.Ux, a.Uy, b.Ux, b.Uy, "wall direction cross");
            var qx = SubtractFinite(b.Segment.Start.X, a.Segment.Start.X, "wall intersection qx");
            var qy = SubtractFinite(b.Segment.Start.Y, a.Segment.Start.Y, "wall intersection qy");

            if (Math.Abs(determinant) <= ParallelDirectionEpsilon)
            {
                if (DistanceToInfiniteLine(b.Segment.Start, a) > tolerance || DistanceToInfiniteLine(b.Segment.End, a) > tolerance)
                    yield break;
                foreach (var point in new[] { a.Segment.Start, a.Segment.End, b.Segment.Start, b.Segment.End })
                    if (PointOnSegment(point, a, tolerance) && PointOnSegment(point, b, tolerance)) yield return point;
                yield break;
            }

            var distanceA = CrossFinite(qx, qy, b.Ux, b.Uy, "wall intersection distance A") / determinant;
            var distanceB = CrossFinite(qx, qy, a.Ux, a.Uy, "wall intersection distance B") / determinant;
            if (!Finite(distanceA) || !Finite(distanceB)) throw new OverflowException("Wall intersection parameter overflowed.");
            if (distanceA < -tolerance || distanceA > a.Length + tolerance || distanceB < -tolerance || distanceB > b.Length + tolerance) yield break;

            var clampedDistance = Math.Max(0d, Math.Min(a.Length, distanceA));
            var pointResult = CoordinateAt(a, clampedDistance);
            Validate(pointResult, "wall intersection");
            yield return pointResult;
        }

        private static bool PointOnSegment(Point2 point, SegmentInfo segment, double tolerance)
        {
            if (point.X < segment.MinX - tolerance || point.X > segment.MaxX + tolerance || point.Y < segment.MinY - tolerance || point.Y > segment.MaxY + tolerance) return false;
            return DistanceToInfiniteLine(point, segment) <= tolerance;
        }

        private static double DistanceToInfiniteLine(Point2 point, SegmentInfo segment)
        {
            var qx = SubtractFinite(point.X, segment.Segment.Start.X, "wall line-distance qx");
            var qy = SubtractFinite(point.Y, segment.Segment.Start.Y, "wall line-distance qy");
            return Math.Abs(CrossFinite(qx, qy, segment.Ux, segment.Uy, "wall line-distance cross"));
        }

        private static void AddRays(Point2 node, SegmentInfo segment, double tolerance, List<Point2> rays)
        {
            var atStart = node.DistanceTo(segment.Segment.Start) <= tolerance;
            var atEnd = node.DistanceTo(segment.Segment.End) <= tolerance;
            if (atStart && atEnd) return;
            if (atStart)
            {
                rays.Add(new Point2(segment.Ux, segment.Uy));
                return;
            }
            if (atEnd)
            {
                rays.Add(new Point2(-segment.Ux, -segment.Uy));
                return;
            }
            rays.Add(new Point2(segment.Ux, segment.Uy));
            rays.Add(new Point2(-segment.Ux, -segment.Uy));
        }

        private static List<Point2> MergeDirections(IEnumerable<Point2> rays, double angularTolerance)
        {
            var unique = new List<Point2>();
            var cosineThreshold = Math.Cos(angularTolerance);
            foreach (var ray in rays)
            {
                var exists = unique.Any(existing => existing.X * ray.X + existing.Y * ray.Y >= cosineThreshold);
                if (!exists) unique.Add(ray);
            }
            return unique;
        }

        private static WallJunctionKind Classify(IReadOnlyList<Point2> rays)
        {
            if (rays.Count <= 1) return WallJunctionKind.End;
            if (rays.Count == 2)
            {
                var dot = rays[0].X * rays[1].X + rays[0].Y * rays[1].Y;
                return dot < -0.9999d ? WallJunctionKind.Straight : WallJunctionKind.L;
            }
            if (rays.Count == 3) return WallJunctionKind.T;
            if (rays.Count == 4) return WallJunctionKind.X;
            return WallJunctionKind.Multi;
        }

        private static Point2 CoordinateAt(SegmentInfo segment, double distance)
        {
            var x = AddFinite(segment.Segment.Start.X, MultiplyFinite(segment.Ux, distance, "wall coordinate x delta"), "wall coordinate x");
            var y = AddFinite(segment.Segment.Start.Y, MultiplyFinite(segment.Uy, distance, "wall coordinate y delta"), "wall coordinate y");
            return new Point2(x, y);
        }

        private static double CrossFinite(double ax, double ay, double bx, double by, string label)
        {
            var first = MultiplyFinite(ax, by, label + " first product");
            var second = MultiplyFinite(ay, bx, label + " second product");
            return SubtractFinite(first, second, label);
        }

        private static double MultiplyFinite(double first, double second, string label)
        {
            var value = first * second;
            if (!Finite(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static double AddFinite(double first, double second, string label)
        {
            var value = first + second;
            if (!Finite(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static double SubtractFinite(double first, double second, string label)
        {
            var value = first - second;
            if (!Finite(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Validate(Point2 point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentOutOfRangeException(label, "Point coordinates must be finite.");
        }
    }
}
