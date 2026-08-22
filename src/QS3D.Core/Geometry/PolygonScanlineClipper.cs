using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

            var signedArea = PolylineMetrics.SignedArea(vertices);
            if (Math.Abs(signedArea) <= Epsilon * 0.5d)
                throw new ArgumentException("Polygon area is zero or below tolerance.", nameof(polygon));

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
            var ax = b.X - a.X;
            var ay = b.Y - a.Y;
            var bx = c.X - a.X;
            var by = c.Y - a.Y;
            if (Finite(ax) && Finite(ay) && Finite(bx) && Finite(by))
                return CrossFinite(ax, ay, bx, by, "Polygon orientation");

            var scaleX = Math.Max(Math.Abs(a.X), Math.Max(Math.Abs(b.X), Math.Abs(c.X)));
            var scaleY = Math.Max(Math.Abs(a.Y), Math.Max(Math.Abs(b.Y), Math.Abs(c.Y)));
            if (!Finite(scaleX) || !Finite(scaleY))
                throw new OverflowException("Polygon orientation input exceeds the supported numeric range.");
            if (scaleX == 0d || scaleY == 0d) return 0d;

            ax = b.X / scaleX - a.X / scaleX;
            bx = c.X / scaleX - a.X / scaleX;
            ay = b.Y / scaleY - a.Y / scaleY;
            by = c.Y / scaleY - a.Y / scaleY;
            var normalized = ax * by - ay * bx;
            if (!Finite(normalized)) throw new OverflowException("Polygon orientation exceeds the supported numeric range.");
            return RestorePredicateCross(normalized, scaleX, scaleY);
        }

        private static double CrossFinite(double ax, double ay, double bx, double by, string label)
        {
            var firstProduct = ax * by;
            var secondProduct = ay * bx;
            var firstProductUnderflowed = firstProduct == 0d && ax != 0d && by != 0d;
            var secondProductUnderflowed = secondProduct == 0d && ay != 0d && bx != 0d;
            if (Finite(firstProduct) && Finite(secondProduct) && !firstProductUnderflowed && !secondProductUnderflowed)
            {
                var direct = firstProduct - secondProduct;
                if (Finite(direct))
                {
                    if (direct != 0d || firstProduct == 0d || secondProduct == 0d) return direct;
                    return ExactCrossFinite(ax, ay, bx, by);
                }
            }

            var scaleA = Math.Max(Math.Abs(ax), Math.Abs(ay));
            var scaleB = Math.Max(Math.Abs(bx), Math.Abs(by));
            if (!Finite(scaleA) || !Finite(scaleB)) throw new OverflowException(label + " input exceeds the supported numeric range.");
            if (scaleA == 0d || scaleB == 0d) return 0d;

            var normalized = ax / scaleA * (by / scaleB) - ay / scaleA * (bx / scaleB);
            if (!Finite(normalized)) throw new OverflowException(label + " exceeds the supported numeric range.");
            if (normalized == 0d) return ExactCrossFinite(ax, ay, bx, by);
            return RestorePredicateCross(normalized, scaleA, scaleB);
        }

        private static double ExactCrossFinite(double ax, double ay, double bx, double by)
        {
            BigInteger firstSignificand;
            BigInteger secondSignificand;
            int firstExponent;
            int secondExponent;
            ExactProduct(ax, by, out firstSignificand, out firstExponent);
            ExactProduct(ay, bx, out secondSignificand, out secondExponent);

            var commonExponent = Math.Min(firstExponent, secondExponent);
            var exact = (firstSignificand << (firstExponent - commonExponent))
                - (secondSignificand << (secondExponent - commonExponent));
            if (exact.IsZero) return 0d;
            return ScaleDyadicPredicate(exact, commonExponent);
        }

        private static void ExactProduct(double first, double second, out BigInteger significand, out int exponent)
        {
            BigInteger firstSignificand;
            BigInteger secondSignificand;
            int firstExponent;
            int secondExponent;
            DecomposeFinite(first, out firstSignificand, out firstExponent);
            DecomposeFinite(second, out secondSignificand, out secondExponent);
            significand = firstSignificand * secondSignificand;
            exponent = firstExponent + secondExponent;
        }

        private static void DecomposeFinite(double value, out BigInteger significand, out int exponent)
        {
            var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
            var exponentBits = (int)((bits >> 52) & 0x7ffUL);
            var fraction = bits & 0x000fffffffffffffUL;

            if (exponentBits == 0)
            {
                significand = new BigInteger(fraction);
                exponent = -1074;
            }
            else
            {
                significand = new BigInteger(fraction | 0x0010000000000000UL);
                exponent = exponentBits - 1023 - 52;
            }

            if ((bits & 0x8000000000000000UL) != 0UL) significand = -significand;
        }

        private static double ScaleDyadicPredicate(BigInteger significand, int exponent)
        {
            var sign = significand.Sign;
            var value = (double)significand;
            if (!Finite(value)) return sign > 0 ? double.MaxValue : -double.MaxValue;

            while (exponent > 512)
            {
                var scaled = value * Math.Pow(2d, 512d);
                if (!Finite(scaled)) return sign > 0 ? double.MaxValue : -double.MaxValue;
                value = scaled;
                exponent -= 512;
            }

            while (exponent < -512)
            {
                var scaled = value * Math.Pow(2d, -512d);
                if (scaled == 0d) return sign > 0 ? double.Epsilon : -double.Epsilon;
                value = scaled;
                exponent += 512;
            }

            if (exponent != 0)
            {
                var scaled = value * Math.Pow(2d, exponent);
                if (!Finite(scaled)) return sign > 0 ? double.MaxValue : -double.MaxValue;
                if (scaled == 0d) return sign > 0 ? double.Epsilon : -double.Epsilon;
                value = scaled;
            }

            return value;
        }

        private static double RestorePredicateCross(double normalized, double firstScale, double secondScale)
        {
            if (normalized == 0d) return 0d;

            var smaller = Math.Min(firstScale, secondScale);
            var larger = Math.Max(firstScale, secondScale);
            var scaleFirst = Math.Abs(normalized) <= 1d ? larger : smaller;
            var scaleSecond = Math.Abs(normalized) <= 1d ? smaller : larger;
            var scaled = normalized * scaleFirst;
            if (!Finite(scaled)) return normalized > 0d ? double.MaxValue : -double.MaxValue;
            var value = scaled * scaleSecond;
            if (!Finite(value)) return scaled > 0d ? double.MaxValue : -double.MaxValue;
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
