using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class PolygonRegion2
    {
        internal PolygonRegion2(IReadOnlyList<Point2> outer, IReadOnlyList<IReadOnlyList<Point2>> holes)
        {
            Outer = outer ?? throw new ArgumentNullException(nameof(outer));
            Holes = holes ?? throw new ArgumentNullException(nameof(holes));
            var loops = new List<IReadOnlyList<Point2>>(1 + holes.Count) { outer };
            loops.AddRange(holes);
            BoundaryLoops = loops.AsReadOnly();
        }

        public IReadOnlyList<Point2> Outer { get; }
        public IReadOnlyList<IReadOnlyList<Point2>> Holes { get; }
        public IReadOnlyList<IReadOnlyList<Point2>> BoundaryLoops { get; }
    }

    public static class PolygonRegionScanlineClipper
    {
        private const int MaxHoles = 256;
        private const int MaxTotalVertices = 16384;
        private const int MaxSegments = 4096;
        private const double Epsilon = 1e-10d;

        public static PolygonRegion2 NormalizeAndValidate(
            IReadOnlyList<Point2> outer,
            IReadOnlyList<IReadOnlyList<Point2>>? holes = null)
        {
            var normalizedOuter = PolygonScanlineClipper.NormalizeAndValidate(outer);
            var sourceHoles = holes ?? Array.Empty<IReadOnlyList<Point2>>();
            if (sourceHoles.Count > MaxHoles)
                throw new ArgumentException("Polygon region exceeds the supported " + MaxHoles + " hole limit.", nameof(holes));

            var normalizedHoles = new List<IReadOnlyList<Point2>>(sourceHoles.Count);
            var totalVertices = normalizedOuter.Count;
            for (var i = 0; i < sourceHoles.Count; i++)
            {
                var hole = sourceHoles[i] ?? throw new ArgumentException("Polygon region hole cannot be null at index " + i + ".", nameof(holes));
                var normalized = PolygonScanlineClipper.NormalizeAndValidate(hole);
                totalVertices += normalized.Count;
                if (totalVertices > MaxTotalVertices)
                    throw new ArgumentException("Polygon region exceeds the supported " + MaxTotalVertices + " total vertex limit.", nameof(holes));

                ValidateHoleAgainstOuter(normalizedOuter, normalized, i);
                for (var j = 0; j < normalizedHoles.Count; j++)
                    ValidateHolePair(normalizedHoles[j], normalized, j, i);
                normalizedHoles.Add(normalized);
            }

            return new PolygonRegion2(normalizedOuter, normalizedHoles.AsReadOnly());
        }

        public static IReadOnlyList<PolygonScanSegment> Clip(PolygonRegion2 region, PolygonScanAxis axis, double coordinate)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            if (!Finite(coordinate)) throw new ArgumentOutOfRangeException(nameof(coordinate));

            var active = PolygonScanlineClipper.Clip(region.Outer, axis, coordinate)
                .Select(ToInterval)
                .ToList();
            if (active.Count == 0 || region.Holes.Count == 0)
                return ToSegments(active, axis, coordinate);

            foreach (var hole in region.Holes)
            {
                var cuts = PolygonScanlineClipper.Clip(hole, axis, coordinate).Select(ToInterval).ToList();
                foreach (var cut in cuts)
                {
                    var next = new List<Interval>(active.Count + 1);
                    foreach (var current in active) Subtract(next, current, cut);
                    active = next;
                    if (active.Count > MaxSegments)
                        throw new InvalidOperationException("Polygon region scanline exceeds the supported " + MaxSegments + " segment limit.");
                    if (active.Count == 0) break;
                }
                if (active.Count == 0) break;
            }

            return ToSegments(active, axis, coordinate);
        }

        private readonly struct Interval
        {
            public Interval(double start, double end)
            {
                Start = Math.Min(start, end);
                End = Math.Max(start, end);
            }
            public double Start { get; }
            public double End { get; }
        }

        private static Interval ToInterval(PolygonScanSegment segment)
        {
            var start = segment.Start.X;
            var end = segment.End.X;
            if (NearlyEqual(segment.Start.X, segment.End.X))
            {
                start = segment.Start.Y;
                end = segment.End.Y;
            }
            return new Interval(start, end);
        }

        private static IReadOnlyList<PolygonScanSegment> ToSegments(IReadOnlyList<Interval> intervals, PolygonScanAxis axis, double coordinate)
        {
            if (intervals.Count > MaxSegments)
                throw new InvalidOperationException("Polygon region scanline exceeds the supported " + MaxSegments + " segment limit.");
            var result = new List<PolygonScanSegment>(intervals.Count);
            foreach (var interval in intervals.OrderBy(x => x.Start))
            {
                if (!(interval.End - interval.Start > Epsilon)) continue;
                var start = axis == PolygonScanAxis.Horizontal ? new Point2(interval.Start, coordinate) : new Point2(coordinate, interval.Start);
                var end = axis == PolygonScanAxis.Horizontal ? new Point2(interval.End, coordinate) : new Point2(coordinate, interval.End);
                result.Add(new PolygonScanSegment(start, end));
            }
            return result.AsReadOnly();
        }

        private static void Subtract(ICollection<Interval> target, Interval source, Interval cut)
        {
            if (cut.End <= source.Start + Epsilon || cut.Start >= source.End - Epsilon)
            {
                target.Add(source);
                return;
            }
            if (cut.Start > source.Start + Epsilon) target.Add(new Interval(source.Start, Math.Min(cut.Start, source.End)));
            if (cut.End < source.End - Epsilon) target.Add(new Interval(Math.Max(cut.End, source.Start), source.End));
        }

        private static void ValidateHoleAgainstOuter(IReadOnlyList<Point2> outer, IReadOnlyList<Point2> hole, int holeIndex)
        {
            foreach (var point in hole)
            {
                var location = LocatePoint(outer, point);
                if (location != PointLocation.Inside)
                    throw new ArgumentException("Polygon region hole " + holeIndex + " must be strictly inside the outer boundary without touching it.");
            }
            if (BoundariesIntersect(outer, hole))
                throw new ArgumentException("Polygon region hole " + holeIndex + " intersects/touches the outer boundary.");
        }

        private static void ValidateHolePair(IReadOnlyList<Point2> left, IReadOnlyList<Point2> right, int leftIndex, int rightIndex)
        {
            if (BoundariesIntersect(left, right))
                throw new ArgumentException("Polygon region holes " + leftIndex + " and " + rightIndex + " intersect/touch.");
            if (LocatePoint(left, right[0]) != PointLocation.Outside || LocatePoint(right, left[0]) != PointLocation.Outside)
                throw new ArgumentException("Polygon region holes " + leftIndex + " and " + rightIndex + " overlap or are nested. Islands require an explicit multi-region topology contract.");
        }

        private enum PointLocation { Outside, Inside, Boundary }

        private static PointLocation LocatePoint(IReadOnlyList<Point2> polygon, Point2 point)
        {
            var inside = false;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                if (OnSegment(a, point, b)) return PointLocation.Boundary;
                var crosses = (a.Y > point.Y) != (b.Y > point.Y);
                if (!crosses) continue;
                var x = a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                if (!Finite(x)) throw new OverflowException("Polygon region point-in-polygon intersection is not finite.");
                if (x > point.X + Epsilon) inside = !inside;
                else if (Math.Abs(x - point.X) <= Epsilon) return PointLocation.Boundary;
            }
            return inside ? PointLocation.Inside : PointLocation.Outside;
        }

        private static bool BoundariesIntersect(IReadOnlyList<Point2> left, IReadOnlyList<Point2> right)
        {
            for (var i = 0; i < left.Count; i++)
            {
                var a = left[i];
                var b = left[(i + 1) % left.Count];
                for (var j = 0; j < right.Count; j++)
                {
                    var c = right[j];
                    var d = right[(j + 1) % right.Count];
                    if (SegmentsIntersect(a, b, c, d)) return true;
                }
            }
            return false;
        }

        private static bool SegmentsIntersect(Point2 a, Point2 b, Point2 c, Point2 d)
        {
            var o1 = Orientation(a, b, c);
            var o2 = Orientation(a, b, d);
            var o3 = Orientation(c, d, a);
            var o4 = Orientation(c, d, b);
            if (Opposite(o1, o2) && Opposite(o3, o4)) return true;
            if (Math.Abs(o1) <= Epsilon && OnSegment(a, c, b)) return true;
            if (Math.Abs(o2) <= Epsilon && OnSegment(a, d, b)) return true;
            if (Math.Abs(o3) <= Epsilon && OnSegment(c, a, d)) return true;
            if (Math.Abs(o4) <= Epsilon && OnSegment(c, b, d)) return true;
            return false;
        }

        private static double Orientation(Point2 a, Point2 b, Point2 c)
        {
            var value = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            if (!Finite(value)) throw new OverflowException("Polygon region orientation exceeds the supported numeric range.");
            return value;
        }

        private static bool OnSegment(Point2 a, Point2 p, Point2 b)
        {
            if (Math.Abs(Orientation(a, b, p)) > Epsilon) return false;
            return p.X >= Math.Min(a.X, b.X) - Epsilon && p.X <= Math.Max(a.X, b.X) + Epsilon &&
                   p.Y >= Math.Min(a.Y, b.Y) - Epsilon && p.Y <= Math.Max(a.Y, b.Y) + Epsilon;
        }

        private static bool Opposite(double left, double right) =>
            (left > Epsilon && right < -Epsilon) || (left < -Epsilon && right > Epsilon);
        private static bool NearlyEqual(double left, double right) => Math.Abs(left - right) <= Epsilon;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
