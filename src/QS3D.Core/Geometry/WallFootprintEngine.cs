using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class WallFootprintResult
    {
        public WallFootprintResult(IReadOnlyList<Point2> polygon, double centerlineLength, double area, double perimeter, bool usedBevelJoin)
        {
            Polygon = new List<Point2>(polygon ?? throw new ArgumentNullException(nameof(polygon))).AsReadOnly();
            CenterlineLength = centerlineLength;
            Area = area;
            Perimeter = perimeter;
            UsedBevelJoin = usedBevelJoin;
        }

        public IReadOnlyList<Point2> Polygon { get; }
        public double CenterlineLength { get; }
        public double Area { get; }
        public double Perimeter { get; }
        public bool UsedBevelJoin { get; }
    }

    public sealed class WallFootprintEngine
    {
        private readonly struct SegmentInfo
        {
            public SegmentInfo(Point2 start, Point2 end)
            {
                Start = start;
                End = end;
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                Length = start.DistanceTo(end);
                Dx = dx / Length;
                Dy = dy / Length;
                Nx = -Dy;
                Ny = Dx;
            }

            public Point2 Start { get; }
            public Point2 End { get; }
            public double Length { get; }
            public double Dx { get; }
            public double Dy { get; }
            public double Nx { get; }
            public double Ny { get; }
        }

        public WallFootprintResult Build(IReadOnlyList<Point2> centerline, double thickness, double miterLimit = 4d, double tolerance = 1e-9d)
        {
            if (centerline == null) throw new ArgumentNullException(nameof(centerline));
            if (!Finite(thickness) || thickness <= 0d) throw new ArgumentOutOfRangeException(nameof(thickness));
            if (!Finite(miterLimit) || miterLimit < 1d) throw new ArgumentOutOfRangeException(nameof(miterLimit));
            if (!Finite(tolerance) || tolerance <= 0d) throw new ArgumentOutOfRangeException(nameof(tolerance));

            var sourcePoints = Clean(centerline, tolerance);
            if (sourcePoints.Count < 2) throw new ArgumentException("Wall centerline requires at least two distinct points.", nameof(centerline));
            var worldOrigin = sourcePoints[0];
            var points = TranslateToLocal(sourcePoints, worldOrigin);
            if (HasSelfIntersection(points, tolerance)) throw new InvalidOperationException("Wall centerline self-intersects; split it into non-self-intersecting wall elements first.");

            var segments = new List<SegmentInfo>(points.Count - 1);
            var centerlineLength = 0d;
            for (var i = 1; i < points.Count; i++)
            {
                var segment = new SegmentInfo(points[i - 1], points[i]);
                if (!(segment.Length > tolerance) || !Finite(segment.Length)) throw new InvalidOperationException("Wall centerline contains a degenerate segment.");
                centerlineLength = CheckedAdd(centerlineLength, segment.Length, "wall centerline length");
                segments.Add(segment);
            }

            var half = thickness / 2d;
            var left = BuildSide(points, segments, half, +1d, miterLimit, tolerance, out var leftBevel);
            var right = BuildSide(points, segments, half, -1d, miterLimit, tolerance, out var rightBevel);
            var polygon = new List<Point2>(left.Count + right.Count);
            polygon.AddRange(left);
            for (var i = right.Count - 1; i >= 0; i--) polygon.Add(right[i]);
            polygon = RemoveAdjacentDuplicates(polygon, tolerance);
            if (polygon.Count < 4) throw new InvalidOperationException("Wall footprint collapsed to fewer than four vertices.");
            if (HasPolygonSelfIntersection(polygon, tolerance)) throw new InvalidOperationException("Wall footprint self-intersects. Reduce thickness, split the centerline, or simplify the corner geometry.");

            var area = Math.Abs(SignedAreaRelative(polygon));
            if (!Finite(area) || area <= tolerance * tolerance) throw new InvalidOperationException("Wall footprint area is degenerate.");
            var perimeter = ClosedPerimeter(polygon);
            var worldPolygon = TranslateFromLocal(polygon, worldOrigin);
            return new WallFootprintResult(worldPolygon.AsReadOnly(), centerlineLength, area, perimeter, leftBevel || rightBevel);
        }

        private static List<Point2> TranslateToLocal(IReadOnlyList<Point2> source, Point2 origin)
        {
            var result = new List<Point2>(source.Count);
            foreach (var point in source)
            {
                var local = new Point2(point.X - origin.X, point.Y - origin.Y);
                Validate(local, "local wall centerline point");
                result.Add(local);
            }
            return result;
        }

        private static List<Point2> TranslateFromLocal(IReadOnlyList<Point2> source, Point2 origin)
        {
            var result = new List<Point2>(source.Count);
            foreach (var point in source)
            {
                var world = new Point2(point.X + origin.X, point.Y + origin.Y);
                Validate(world, "world wall footprint point");
                result.Add(world);
            }
            return result;
        }

        private static List<Point2> BuildSide(IReadOnlyList<Point2> points, IReadOnlyList<SegmentInfo> segments, double half, double side, double miterLimit, double tolerance, out bool usedBevel)
        {
            usedBevel = false;
            var result = new List<Point2>();
            result.Add(Offset(points[0], segments[0], half, side));

            for (var i = 1; i < points.Count - 1; i++)
            {
                var previous = segments[i - 1];
                var next = segments[i];
                var directionDot = previous.Dx * next.Dx + previous.Dy * next.Dy;
                if (directionDot < -0.999999999d) throw new InvalidOperationException("Wall centerline contains a 180-degree reversal.");

                var previousOffset = Offset(points[i], previous, half, side);
                var nextOffset = Offset(points[i], next, half, side);
                var determinant = Cross(previous.Dx, previous.Dy, next.Dx, next.Dy);
                if (Math.Abs(determinant) <= tolerance)
                {
                    result.Add(Midpoint(previousOffset, nextOffset));
                    continue;
                }

                var qx = nextOffset.X - previousOffset.X;
                var qy = nextOffset.Y - previousOffset.Y;
                var t = Cross(qx, qy, next.Dx, next.Dy) / determinant;
                var candidate = new Point2(previousOffset.X + previous.Dx * t, previousOffset.Y + previous.Dy * t);
                Validate(candidate, "wall miter point");
                var miterDistance = candidate.DistanceTo(points[i]);
                if (Finite(miterDistance) && miterDistance <= half * miterLimit + tolerance)
                {
                    result.Add(candidate);
                }
                else
                {
                    result.Add(previousOffset);
                    result.Add(nextOffset);
                    usedBevel = true;
                }
            }

            result.Add(Offset(points[points.Count - 1], segments[segments.Count - 1], half, side));
            return RemoveAdjacentDuplicates(result, tolerance);
        }

        private static Point2 Offset(Point2 point, SegmentInfo segment, double half, double side)
        {
            var result = new Point2(point.X + segment.Nx * half * side, point.Y + segment.Ny * half * side);
            Validate(result, "wall offset point");
            return result;
        }

        private static Point2 Midpoint(Point2 a, Point2 b)
        {
            var result = new Point2(a.X + (b.X - a.X) / 2d, a.Y + (b.Y - a.Y) / 2d);
            Validate(result, "wall midpoint");
            return result;
        }

        private static List<Point2> Clean(IReadOnlyList<Point2> source, double tolerance)
        {
            var result = new List<Point2>(source.Count);
            foreach (var point in source)
            {
                Validate(point, "wall centerline point");
                if (result.Count == 0 || result[result.Count - 1].DistanceTo(point) > tolerance) result.Add(point);
            }
            return result;
        }

        private static List<Point2> RemoveAdjacentDuplicates(IReadOnlyList<Point2> source, double tolerance)
        {
            var result = new List<Point2>(source.Count);
            foreach (var point in source)
                if (result.Count == 0 || result[result.Count - 1].DistanceTo(point) > tolerance) result.Add(point);
            if (result.Count > 1 && result[0].DistanceTo(result[result.Count - 1]) <= tolerance) result.RemoveAt(result.Count - 1);
            return result;
        }

        private static bool HasSelfIntersection(IReadOnlyList<Point2> points, double tolerance)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                for (var j = i + 2; j < points.Count - 1; j++)
                {
                    if (SegmentsIntersect(points[i], points[i + 1], points[j], points[j + 1], tolerance)) return true;
                }
            }
            return false;
        }

        private static bool HasPolygonSelfIntersection(IReadOnlyList<Point2> polygon, double tolerance)
        {
            for (var i = 0; i < polygon.Count; i++)
            {
                var a1 = polygon[i];
                var a2 = polygon[(i + 1) % polygon.Count];
                for (var j = i + 1; j < polygon.Count; j++)
                {
                    if (j == i || j == i + 1) continue;
                    if (i == 0 && j == polygon.Count - 1) continue;
                    var b1 = polygon[j];
                    var b2 = polygon[(j + 1) % polygon.Count];
                    if (SegmentsIntersect(a1, a2, b1, b2, tolerance)) return true;
                }
            }
            return false;
        }

        private static bool SegmentsIntersect(Point2 a, Point2 b, Point2 c, Point2 d, double tolerance)
        {
            var abx = b.X - a.X;
            var aby = b.Y - a.Y;
            var cdx = d.X - c.X;
            var cdy = d.Y - c.Y;
            var determinant = Cross(abx, aby, cdx, cdy);
            var acx = c.X - a.X;
            var acy = c.Y - a.Y;
            if (Math.Abs(determinant) <= tolerance)
            {
                if (Math.Abs(Cross(acx, acy, abx, aby)) > tolerance) return false;
                return Overlaps(a.X, b.X, c.X, d.X, tolerance) && Overlaps(a.Y, b.Y, c.Y, d.Y, tolerance);
            }
            var t = Cross(acx, acy, cdx, cdy) / determinant;
            var u = Cross(acx, acy, abx, aby) / determinant;
            return t >= -tolerance && t <= 1d + tolerance && u >= -tolerance && u <= 1d + tolerance;
        }

        private static bool Overlaps(double a, double b, double c, double d, double tolerance)
        {
            var min1 = Math.Min(a, b); var max1 = Math.Max(a, b);
            var min2 = Math.Min(c, d); var max2 = Math.Max(c, d);
            return Math.Max(min1, min2) <= Math.Min(max1, max2) + tolerance;
        }

        private static double SignedAreaRelative(IReadOnlyList<Point2> polygon)
        {
            return PolylineMetrics.SignedArea(polygon);
        }

        private static double ClosedPerimeter(IReadOnlyList<Point2> polygon)
        {
            var sum = 0d;
            var compensation = 0d;
            for (var i = 0; i < polygon.Count; i++)
                AddCompensated(ref sum, ref compensation, polygon[i].DistanceTo(polygon[(i + 1) % polygon.Count]), "wall footprint perimeter");
            return FinalizeCompensated(sum, compensation, "wall footprint perimeter");
        }

        private static void AddCompensated(ref double sum, ref double compensation, double value, string label)
        {
            if (!Finite(sum) || !Finite(compensation) || !Finite(value))
                throw new OverflowException(label + " contains a non-finite value.");

            var next = sum + value;
            if (!Finite(next)) throw new OverflowException(label + " overflowed.");
            var correction = Math.Abs(sum) >= Math.Abs(value)
                ? (sum - next) + value
                : (value - next) + sum;
            var nextCompensation = compensation + correction;
            if (!Finite(nextCompensation)) throw new OverflowException(label + " overflowed.");

            sum = next == 0d ? 0d : next;
            compensation = nextCompensation == 0d ? 0d : nextCompensation;
        }

        private static double FinalizeCompensated(double sum, double compensation, string label)
        {
            if (!Finite(sum) || !Finite(compensation))
                throw new OverflowException(label + " contains a non-finite value.");
            var result = sum + compensation;
            if (!Finite(result)) throw new OverflowException(label + " overflowed.");
            return result == 0d ? 0d : result;
        }

        private static double CheckedAdd(double left, double right, string label)
        {
            if (!Finite(left) || !Finite(right)) throw new OverflowException(label + " contains a non-finite value.");
            var value = left + right;
            if (!Finite(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static double Cross(double ax, double ay, double bx, double by)
        {
            var scaleA = Math.Max(Math.Abs(ax), Math.Abs(ay));
            var scaleB = Math.Max(Math.Abs(bx), Math.Abs(by));
            if (!Finite(scaleA) || !Finite(scaleB)) throw new OverflowException("wall footprint determinant input exceeds the supported numeric range.");
            if (scaleA == 0d || scaleB == 0d) return 0d;

            var normalized = ax / scaleA * (by / scaleB) - ay / scaleA * (bx / scaleB);
            if (!Finite(normalized)) throw new OverflowException("wall footprint determinant exceeds the supported numeric range.");
            var smallerScale = Math.Min(scaleA, scaleB);
            var largerScale = Math.Max(scaleA, scaleB);
            var scaled = normalized * smallerScale;
            if (!Finite(scaled)) throw new OverflowException("wall footprint determinant exceeds the supported numeric range.");
            var value = scaled * largerScale;
            if (!Finite(value)) throw new OverflowException("wall footprint determinant exceeds the supported numeric range.");
            return value;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Validate(Point2 point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentOutOfRangeException(label, "Point coordinates must be finite.");
        }
    }
}
