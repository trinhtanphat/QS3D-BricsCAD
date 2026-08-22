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

        private sealed class SegmentInfo
        {
            public WallAxisSegment Segment { get; set; } = null!;
            public double MinX { get; set; }
            public double MaxX { get; set; }
            public double MinY { get; set; }
            public double MaxY { get; set; }
            public double Dx { get; set; }
            public double Dy { get; set; }
            public double Length { get; set; }
        }

        private sealed class Candidate
        {
            public Point2 Point { get; set; }
            public HashSet<string> SegmentIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<WallJunction> Plan(IEnumerable<WallAxisSegment> source, double tolerance = 0.005d, double angularToleranceRadians = 1e-4d)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!Finite(tolerance) || tolerance <= 0d) throw new ArgumentOutOfRangeException(nameof(tolerance));
            if (!Finite(angularToleranceRadians) || angularToleranceRadians <= 0d || angularToleranceRadians >= Math.PI / 4d)
                throw new ArgumentOutOfRangeException(nameof(angularToleranceRadians));

            var raw = source.ToList();
            if (raw.Count > MaxSegments) throw new InvalidOperationException("Wall junction planning supports at most " + MaxSegments.ToString(CultureInfo.InvariantCulture) + " segments per batch.");
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var segments = new List<SegmentInfo>(raw.Count);
            foreach (var segment in raw)
            {
                if (segment == null) throw new ArgumentException("Wall segment collection contains null.", nameof(source));
                if (!seenIds.Add(segment.Id)) throw new InvalidOperationException("Duplicate wall segment id: " + segment.Id);
                Validate(segment.Start, segment.Id + "/start");
                Validate(segment.End, segment.Id + "/end");
                var dx = segment.End.X - segment.Start.X;
                var dy = segment.End.Y - segment.Start.Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                if (!Finite(length) || length <= tolerance * 1e-6d) throw new InvalidOperationException("Degenerate wall segment: " + segment.Id);
                segments.Add(new SegmentInfo
                {
                    Segment = segment,
                    MinX = Math.Min(segment.Start.X, segment.End.X),
                    MaxX = Math.Max(segment.Start.X, segment.End.X),
                    MinY = Math.Min(segment.Start.Y, segment.End.Y),
                    MaxY = Math.Max(segment.Start.Y, segment.End.Y),
                    Dx = dx,
                    Dy = dy,
                    Length = length
                });
            }
            if (segments.Count == 0) return Array.Empty<WallJunction>();

            var candidates = new List<Candidate>(segments.Count * 2);
            foreach (var segment in segments)
            {
                AddCandidate(candidates, segment.Segment.Start, segment.Segment.Id, tolerance);
                AddCandidate(candidates, segment.Segment.End, segment.Segment.Id, tolerance);
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
                        AddCandidate(candidates, point, other.Segment.Id, tolerance);
                        AddCandidate(candidates, point, current.Segment.Id, tolerance);
                    }
                }
                active.Add(current);
            }

            var byId = segments.ToDictionary(x => x.Segment.Id, StringComparer.OrdinalIgnoreCase);
            var result = new List<WallJunction>();
            foreach (var candidate in candidates)
            {
                var incident = new HashSet<string>(candidate.SegmentIds, StringComparer.OrdinalIgnoreCase);
                foreach (var segment in segments)
                    if (!incident.Contains(segment.Segment.Id) && PointOnSegment(candidate.Point, segment, tolerance)) incident.Add(segment.Segment.Id);

                var rays = new List<Point2>();
                foreach (var id in incident.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    var segment = byId[id];
                    AddRays(candidate.Point, segment, tolerance, rays);
                }
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

        private static void AddCandidate(List<Candidate> candidates, Point2 point, string segmentId, double tolerance)
        {
            Candidate? best = null;
            var bestDistance = double.PositiveInfinity;
            foreach (var candidate in candidates)
            {
                var distance = candidate.Point.DistanceTo(point);
                if (distance <= tolerance && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            if (best == null)
            {
                best = new Candidate { Point = point };
                candidates.Add(best);
            }
            best.SegmentIds.Add(segmentId);
        }

        private static IEnumerable<Point2> Intersections(SegmentInfo a, SegmentInfo b, double tolerance)
        {
            var ax = a.Segment.Start.X;
            var ay = a.Segment.Start.Y;
            var bx = b.Segment.Start.X;
            var by = b.Segment.Start.Y;
            var determinant = Cross(a.Dx, a.Dy, b.Dx, b.Dy);
            var qx = bx - ax;
            var qy = by - ay;

            if (Math.Abs(determinant) <= tolerance * 1e-3d)
            {
                if (DistanceToInfiniteLine(b.Segment.Start, a) > tolerance || DistanceToInfiniteLine(b.Segment.End, a) > tolerance)
                    yield break;
                foreach (var point in new[] { a.Segment.Start, a.Segment.End, b.Segment.Start, b.Segment.End })
                    if (PointOnSegment(point, a, tolerance) && PointOnSegment(point, b, tolerance)) yield return point;
                yield break;
            }

            var ta = Cross(qx, qy, b.Dx, b.Dy) / determinant;
            var tb = Cross(qx, qy, a.Dx, a.Dy) / determinant;
            var epsilonA = tolerance / a.Length;
            var epsilonB = tolerance / b.Length;
            if (ta < -epsilonA || ta > 1d + epsilonA || tb < -epsilonB || tb > 1d + epsilonB) yield break;
            var clamped = Math.Max(0d, Math.Min(1d, ta));
            var pointResult = new Point2(ax + a.Dx * clamped, ay + a.Dy * clamped);
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
            var qx = point.X - segment.Segment.Start.X;
            var qy = point.Y - segment.Segment.Start.Y;
            var cross = Math.Abs(Cross(qx, qy, segment.Dx, segment.Dy));
            var value = cross / segment.Length;
            if (!Finite(value)) throw new OverflowException("Wall line-distance calculation overflowed.");
            return value;
        }

        private static void AddRays(Point2 node, SegmentInfo segment, double tolerance, List<Point2> rays)
        {
            var atStart = node.DistanceTo(segment.Segment.Start) <= tolerance;
            var atEnd = node.DistanceTo(segment.Segment.End) <= tolerance;
            if (atStart && atEnd) return;
            if (atStart)
            {
                rays.Add(Unit(segment.Dx, segment.Dy));
                return;
            }
            if (atEnd)
            {
                rays.Add(Unit(-segment.Dx, -segment.Dy));
                return;
            }
            rays.Add(Unit(segment.Dx, segment.Dy));
            rays.Add(Unit(-segment.Dx, -segment.Dy));
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

        private static Point2 Unit(double x, double y)
        {
            var length = Math.Sqrt(x * x + y * y);
            if (!Finite(length) || length <= 0d) throw new InvalidOperationException("Wall direction is degenerate.");
            return new Point2(x / length, y / length);
        }

        private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Validate(Point2 point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentOutOfRangeException(label, "Point coordinates must be finite.");
        }
    }
}
