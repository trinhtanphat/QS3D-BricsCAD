using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public enum PolygonScanAxis
    {
        Horizontal,
        Vertical
    }

    public sealed class PolygonScanSegment
    {
        public PolygonScanSegment(Point2 start, Point2 end)
        {
            Start = start;
            End = end;
            Length = start.DistanceTo(end);
            if (!(Length > 0d)) throw new ArgumentOutOfRangeException(nameof(end), "Scan segment must have positive length.");
        }

        public Point2 Start { get; }
        public Point2 End { get; }
        public double Length { get; }
    }

    public static class PolygonScanlineClipper
    {
        private const int MaxVertices = 4096;
        private const int MaxSegments = 2048;
        private const double Epsilon = 1e-10d;

        public static IReadOnlyList<PolygonScanSegment> Clip(IReadOnlyList<Point2> polygon, PolygonScanAxis axis, double coordinate)
        {
            if (polygon == null) throw new ArgumentNullException(nameof(polygon));
            if (axis != PolygonScanAxis.Horizontal && axis != PolygonScanAxis.Vertical)
                throw new ArgumentOutOfRangeException(nameof(axis));
            if (!Finite(coordinate)) throw new ArgumentOutOfRangeException(nameof(coordinate));
            var vertices = NormalizeAndValidate(polygon);
            var intersections = new List<double>(vertices.Count);

            for (var i = 0; i < vertices.Count; i++)
            {
                var a = vertices[i];
                var b = vertices[(i + 1) % vertices.Count];
                var aAcross = axis == PolygonScanAxis.Horizontal ? a.Y : a.X;
                var bAcross = axis == PolygonScanAxis.Horizontal ? b.Y : b.X;
                var aAlong = axis == PolygonScanAxis.Horizontal ? a.X : a.Y;
                var bAlong = axis == PolygonScanAxis.Horizontal ? b.X : b.Y;

                if (NearlyEqual(aAcross, bAcross)) continue;

                // Half-open edge rule: include the lower endpoint and exclude the upper one. This
                // makes scanlines through a polygon vertex deterministic without double counting.
                var low = Math.Min(aAcross, bAcross);
                var high = Math.Max(aAcross, bAcross);
                if (coordinate < low - Epsilon || coordinate >= high - Epsilon) continue;

                var t = (coordinate - aAcross) / (bAcross - aAcross);
                if (!Finite(t) || t < -Epsilon || t > 1d + Epsilon)
                    throw new InvalidOperationException("Polygon scanline interpolation escaped the supported edge range.");
                var along = aAlong + (bAlong - aAlong) * t;
                if (!Finite(along)) throw new OverflowException("Polygon scanline intersection is not finite.");
                intersections.Add(along);
            }

            intersections.Sort();
            DeduplicateIntersections(intersections);
            if ((intersections.Count & 1) != 0)
                throw new InvalidOperationException("Simple polygon scanline produced an odd intersection count. Check polygon validity/tolerance.");
            if (intersections.Count / 2 > MaxSegments)
                throw new InvalidOperationException("Polygon scanline exceeds the supported " + MaxSegments + " segment limit.");

            var segments = new List<PolygonScanSegment>(intersections.Count / 2);
            for (var i = 0; i < intersections.Count; i += 2)
            {
                var startAlong = intersections[i];
                var endAlong = intersections[i + 1];
                if (!(endAlong - startAlong > Epsilon)) continue;
                var start = axis == PolygonScanAxis.Horizontal ? new Point2(startAlong, coordinate) : new Point2(coordinate, startAlong);
                var end = axis == PolygonScanAxis.Horizontal ? new Point2(endAlong, coordinate) : new Point2(coordinate, endAlong);
                segments.Add(new PolygonScanSegment(start, end));
            }
            return segments.AsReadOnly();
        }

        public static IReadOnlyList<Point2> NormalizeAndValidate(IReadOnlyList<Point2> polygon)
        {
            if (polygon == null) throw new ArgumentNullException(nameof(polygon));
            if (polygon.Count < 3) throw new ArgumentException("Polygon requires at least three vertices.", nameof(polygon));
            if (polygon.Count > MaxVertices) throw new ArgumentException("Polygon exceeds the supported " + MaxVertices + " vertex limit.", nameof(polygon));

            var vertices = polygon.ToList();
            foreach (var point in vertices)
                if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentException("Polygon vertices must be finite.", nameof(polygon));

            if (vertices.Count > 3 && vertices[0].DistanceTo(vertices[vertices.Count - 1]) <= Epsilon)
                vertices.RemoveAt(vertices.Count - 1);
            if (vertices.Count < 3) throw new ArgumentException("Polygon requires at least three distinct vertices.", nameof(polygon));

            for (var i = 0; i < vertices.Count; i++)
            {
                if (vertices[i].DistanceTo(vertices[(i + 1) % vertices.Count]) <= Epsilon)
                    throw new ArgumentException("Polygon contains a zero-length edge at vertex " + i + ".", nameof(polygon));
            }

            var origin = vertices[0];
            var twiceArea = 0d;
            var compensation = 0d;
            for (var i = 1; i < vertices.Count - 1; i++)
            {
                var ax = vertices[i].X - origin.X;
                var ay = vertices[i].Y - origin.Y;
                var bx = vertices[i + 1].X - origin.X;
                var by = vertices[i + 1].Y - origin.Y;
                if (!Finite(ax) || !Finite(ay) || !Finite(bx) || !Finite(by))
                    throw new OverflowException("Polygon coordinate delta exceeds the supported numeric range.");
                var cross = ax * by - ay * bx;
                if (!Finite(cross)) throw new OverflowException("Polygon signed area exceeds the supported numeric range.");
                var corrected = cross - compensation;
                var next = twiceArea + corrected;
                if (!Finite(next)) throw new OverflowException("Polygon signed area exceeds the supported numeric range.");
                compensation = (next - twiceArea) - corrected;
                twiceArea = next;
            }
            if (Math.Abs(twiceArea) <= Epsilon) throw new ArgumentException("Polygon area is zero or below tolerance.", nameof(polygon));

            ValidateSimple(vertices);
            return vertices.AsReadOnly();
        }

        private static void ValidateSimple(IReadOnlyList<Point2> vertices)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                var a1 = vertices[i];
                var a2 = vertices[(i + 1) % vertices.Count];
                for (var j = i + 1; j < vertices.Count; j++)
                {
                    if (Adjacent(i, j, vertices.Count)) continue;
                    var b1 = vertices[j];
                    var b2 = vertices[(j + 1) % vertices.Count];
                    if (SegmentsIntersect(a1, a2, b1, b2))
                        throw new ArgumentException("Polygon self-intersects between edges " + i + " and " + j + ".", nameof(vertices));
                }
            }
        }

        private static bool Adjacent(int left, int right, int count)
        {
            if (left == right) return true;
            if ((left + 1) % count == right) return true;
            if ((right + 1) % count == left) return true;
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
            if (!Finite(value)) throw new OverflowException("Polygon orientation exceeds the supported numeric range.");
            return value;
        }

        private static bool Opposite(double left, double right) => (left > Epsilon && right < -Epsilon) || (left < -Epsilon && right > Epsilon);

        private static bool OnSegment(Point2 a, Point2 p, Point2 b) =>
            p.X >= Math.Min(a.X, b.X) - Epsilon && p.X <= Math.Max(a.X, b.X) + Epsilon &&
            p.Y >= Math.Min(a.Y, b.Y) - Epsilon && p.Y <= Math.Max(a.Y, b.Y) + Epsilon;

        private static void DeduplicateIntersections(List<double> intersections)
        {
            if (intersections.Count < 2) return;
            var write = 1;
            for (var read = 1; read < intersections.Count; read++)
            {
                if (Math.Abs(intersections[read] - intersections[write - 1]) <= Epsilon) continue;
                intersections[write++] = intersections[read];
            }
            if (write < intersections.Count) intersections.RemoveRange(write, intersections.Count - write);
        }

        private static bool NearlyEqual(double left, double right) => Math.Abs(left - right) <= Epsilon;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
