using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public enum GridReferenceCurveKind
    {
        Line = 0,
        Arc = 1
    }

    public sealed class GridReferenceCurve
    {
        private GridReferenceCurve(
            string elementId,
            GridReferenceCurveKind kind,
            Point2 start,
            Point2 end,
            Point2 center,
            double radius,
            double startAngleRad,
            double sweepAngleRad)
        {
            ElementId = elementId;
            Kind = kind;
            Start = start;
            End = end;
            Center = center;
            Radius = radius;
            StartAngleRad = startAngleRad;
            SweepAngleRad = sweepAngleRad;
        }

        public string ElementId { get; }
        public GridReferenceCurveKind Kind { get; }
        public Point2 Start { get; }
        public Point2 End { get; }
        public Point2 Center { get; }
        public double Radius { get; }
        public double StartAngleRad { get; }
        public double SweepAngleRad { get; }

        public static GridReferenceCurve Line(string elementId, Point2 start, Point2 end)
        {
            return new GridReferenceCurve(elementId, GridReferenceCurveKind.Line, start, end, default(Point2), 0.0, 0.0, 0.0);
        }

        public static GridReferenceCurve Arc(
            string elementId,
            Point2 center,
            double radius,
            double startAngleRad,
            double sweepAngleRad)
        {
            var start = new Point2(
                center.X + radius * Math.Cos(startAngleRad),
                center.Y + radius * Math.Sin(startAngleRad));
            var endAngle = startAngleRad + sweepAngleRad;
            var end = new Point2(
                center.X + radius * Math.Cos(endAngle),
                center.Y + radius * Math.Sin(endAngle));
            return new GridReferenceCurve(elementId, GridReferenceCurveKind.Arc, start, end, center, radius, startAngleRad, sweepAngleRad);
        }
    }

    public sealed class GridIntersection
    {
        public GridIntersection(string firstElementId, string secondElementId, Point2 point)
        {
            FirstElementId = firstElementId;
            SecondElementId = secondElementId;
            Point = point;
        }

        public string FirstElementId { get; }
        public string SecondElementId { get; }
        public Point2 Point { get; }
    }

    public static class GridIntersectionPlanner
    {
        private const int MaxCurves = 2000;
        private const int MaxIntersections = 100000;
        private const double TwoPi = Math.PI * 2.0;

        public static IReadOnlyList<GridIntersection> FindIntersections(
            IEnumerable<GridReferenceCurve> curves,
            double tolerance = 1e-8)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            if (!IsFinite(tolerance) || tolerance <= 0.0) throw new ArgumentOutOfRangeException(nameof(tolerance));

            var list = curves.ToList();
            if (list.Count > MaxCurves) throw new InvalidOperationException("Grid intersection planning supports at most " + MaxCurves + " curves.");
            if (list.Count < 2) return Array.Empty<GridIntersection>();

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < list.Count; i++)
            {
                Validate(list[i], tolerance, i);
                if (!ids.Add(list[i].ElementId))
                    throw new InvalidOperationException("Grid intersection input contains duplicate element id: " + list[i].ElementId + ".");
            }

            var result = new List<GridIntersection>();
            for (var i = 0; i < list.Count - 1; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    var points = IntersectPair(list[i], list[j], tolerance);
                    foreach (var point in points)
                    {
                        result.Add(new GridIntersection(list[i].ElementId, list[j].ElementId, point));
                        if (result.Count > MaxIntersections)
                            throw new InvalidOperationException("Grid intersection plan exceeds the supported " + MaxIntersections + " intersection limit.");
                    }
                }
            }

            return result.AsReadOnly();
        }

        private static IReadOnlyList<Point2> IntersectPair(GridReferenceCurve first, GridReferenceCurve second, double tolerance)
        {
            if (first.Kind == GridReferenceCurveKind.Line && second.Kind == GridReferenceCurveKind.Line)
                return IntersectLines(first, second, tolerance);
            if (first.Kind == GridReferenceCurveKind.Line && second.Kind == GridReferenceCurveKind.Arc)
                return IntersectLineArc(first, second, tolerance);
            if (first.Kind == GridReferenceCurveKind.Arc && second.Kind == GridReferenceCurveKind.Line)
                return IntersectLineArc(second, first, tolerance);
            if (first.Kind == GridReferenceCurveKind.Arc && second.Kind == GridReferenceCurveKind.Arc)
                return IntersectArcs(first, second, tolerance);
            throw new InvalidOperationException("Unsupported Grid reference curve pair.");
        }

        private static IReadOnlyList<Point2> IntersectLines(GridReferenceCurve first, GridReferenceCurve second, double tolerance)
        {
            var ax = first.Start.X;
            var ay = first.Start.Y;
            var rx = first.End.X - ax;
            var ry = first.End.Y - ay;
            var bx = second.Start.X;
            var by = second.Start.Y;
            var sx = second.End.X - bx;
            var sy = second.End.Y - by;
            var rxs = Cross(rx, ry, sx, sy);
            var qpx = bx - ax;
            var qpy = by - ay;
            var scale = Math.Sqrt((rx * rx + ry * ry) * (sx * sx + sy * sy));
            var crossTolerance = tolerance * Math.Max(1.0, scale);

            if (Math.Abs(rxs) <= crossTolerance)
            {
                if (Math.Abs(Cross(qpx, qpy, rx, ry)) > tolerance * Math.Max(1.0, Math.Sqrt(rx * rx + ry * ry)))
                    return Array.Empty<Point2>();

                var shared = new List<Point2>(2);
                AddIfOnBoth(shared, first.Start, first, second, tolerance);
                AddIfOnBoth(shared, first.End, first, second, tolerance);
                AddIfOnBoth(shared, second.Start, first, second, tolerance);
                AddIfOnBoth(shared, second.End, first, second, tolerance);
                Deduplicate(shared, tolerance);
                if (shared.Count > 1)
                    throw Ambiguous(first, second, "collinear/overlapping LINE references do not define one unique Grid intersection");
                return shared.Count == 1 ? shared.AsReadOnly() : Array.Empty<Point2>();
            }

            var t = Cross(qpx, qpy, sx, sy) / rxs;
            var u = Cross(qpx, qpy, rx, ry) / rxs;
            var paramTolerance = tolerance / Math.Max(tolerance, Math.Min(Length(rx, ry), Length(sx, sy)));
            if (t < -paramTolerance || t > 1.0 + paramTolerance || u < -paramTolerance || u > 1.0 + paramTolerance)
                return Array.Empty<Point2>();

            t = Clamp01(t);
            return new[] { new Point2(ax + t * rx, ay + t * ry) };
        }

        private static IReadOnlyList<Point2> IntersectLineArc(GridReferenceCurve line, GridReferenceCurve arc, double tolerance)
        {
            var dx = line.End.X - line.Start.X;
            var dy = line.End.Y - line.Start.Y;
            var fx = line.Start.X - arc.Center.X;
            var fy = line.Start.Y - arc.Center.Y;
            var a = dx * dx + dy * dy;
            var b = 2.0 * (fx * dx + fy * dy);
            var c = fx * fx + fy * fy - arc.Radius * arc.Radius;
            var discriminant = b * b - 4.0 * a * c;
            var discTolerance = tolerance * Math.Max(1.0, b * b + Math.Abs(4.0 * a * c));
            if (discriminant < -discTolerance) return Array.Empty<Point2>();
            if (Math.Abs(discriminant) <= discTolerance) discriminant = 0.0;

            var roots = new List<double>(2);
            if (discriminant == 0.0)
            {
                roots.Add(-b / (2.0 * a));
            }
            else
            {
                var sqrt = Math.Sqrt(discriminant);
                roots.Add((-b - sqrt) / (2.0 * a));
                roots.Add((-b + sqrt) / (2.0 * a));
            }
            roots.Sort();

            var lineLength = Math.Sqrt(a);
            var paramTolerance = tolerance / Math.Max(tolerance, lineLength);
            var points = new List<Point2>(2);
            foreach (var root in roots)
            {
                if (root < -paramTolerance || root > 1.0 + paramTolerance) continue;
                var t = Clamp01(root);
                var point = new Point2(line.Start.X + t * dx, line.Start.Y + t * dy);
                if (!IsOnArc(point, arc, tolerance)) continue;
                points.Add(point);
            }
            Deduplicate(points, tolerance);
            return points.AsReadOnly();
        }

        private static IReadOnlyList<Point2> IntersectArcs(GridReferenceCurve first, GridReferenceCurve second, double tolerance)
        {
            var dx = second.Center.X - first.Center.X;
            var dy = second.Center.Y - first.Center.Y;
            var distance = Length(dx, dy);

            if (distance <= tolerance && Math.Abs(first.Radius - second.Radius) <= tolerance)
                throw Ambiguous(first, second, "coincident ARC support circles are intentionally rejected; split/review the Grid references explicitly");
            if (distance <= tolerance) return Array.Empty<Point2>();
            if (distance > first.Radius + second.Radius + tolerance) return Array.Empty<Point2>();
            if (distance < Math.Abs(first.Radius - second.Radius) - tolerance) return Array.Empty<Point2>();

            var a = (first.Radius * first.Radius - second.Radius * second.Radius + distance * distance) / (2.0 * distance);
            var h2 = first.Radius * first.Radius - a * a;
            if (h2 < -tolerance * Math.Max(1.0, first.Radius * first.Radius)) return Array.Empty<Point2>();
            if (h2 < 0.0) h2 = 0.0;
            var h = Math.Sqrt(h2);
            var ux = dx / distance;
            var uy = dy / distance;
            var px = first.Center.X + a * ux;
            var py = first.Center.Y + a * uy;

            var points = new List<Point2>(2);
            var p1 = new Point2(px - h * uy, py + h * ux);
            if (IsOnArc(p1, first, tolerance) && IsOnArc(p1, second, tolerance)) points.Add(p1);
            if (h > tolerance)
            {
                var p2 = new Point2(px + h * uy, py - h * ux);
                if (IsOnArc(p2, first, tolerance) && IsOnArc(p2, second, tolerance)) points.Add(p2);
            }
            Deduplicate(points, tolerance);
            points.Sort((left, right) =>
            {
                var x = left.X.CompareTo(right.X);
                return x != 0 ? x : left.Y.CompareTo(right.Y);
            });
            return points.AsReadOnly();
        }

        private static bool IsOnArc(Point2 point, GridReferenceCurve arc, double tolerance)
        {
            var dx = point.X - arc.Center.X;
            var dy = point.Y - arc.Center.Y;
            var radius = Length(dx, dy);
            if (Math.Abs(radius - arc.Radius) > tolerance) return false;
            if (arc.SweepAngleRad >= TwoPi - tolerance / Math.Max(arc.Radius, tolerance)) return true;
            var angle = NormalizeAngle(Math.Atan2(dy, dx));
            var start = NormalizeAngle(arc.StartAngleRad);
            var delta = NormalizeAngle(angle - start);
            var angularTolerance = tolerance / Math.Max(arc.Radius, tolerance);
            return delta <= arc.SweepAngleRad + angularTolerance;
        }

        private static void AddIfOnBoth(List<Point2> points, Point2 point, GridReferenceCurve first, GridReferenceCurve second, double tolerance)
        {
            if (IsOnLineSegment(point, first, tolerance) && IsOnLineSegment(point, second, tolerance)) points.Add(point);
        }

        private static bool IsOnLineSegment(Point2 point, GridReferenceCurve line, double tolerance)
        {
            var dx = line.End.X - line.Start.X;
            var dy = line.End.Y - line.Start.Y;
            var px = point.X - line.Start.X;
            var py = point.Y - line.Start.Y;
            var length = Length(dx, dy);
            if (Math.Abs(Cross(px, py, dx, dy)) > tolerance * Math.Max(1.0, length)) return false;
            var dot = px * dx + py * dy;
            var length2 = dx * dx + dy * dy;
            var paramTolerance = tolerance / Math.Max(tolerance, length);
            return dot >= -paramTolerance * length2 && dot <= (1.0 + paramTolerance) * length2;
        }

        private static void Validate(GridReferenceCurve curve, double tolerance, int index)
        {
            if (curve == null) throw new ArgumentException("Grid intersection curve cannot be null at index " + index + ".", nameof(curve));
            if (string.IsNullOrWhiteSpace(curve.ElementId)) throw new ArgumentException("Grid intersection curve requires ElementId at index " + index + ".", nameof(curve));
            if (!IsFinite(curve.Start.X) || !IsFinite(curve.Start.Y) || !IsFinite(curve.End.X) || !IsFinite(curve.End.Y))
                throw new ArgumentException("Grid intersection curve contains non-finite endpoints: " + curve.ElementId + ".", nameof(curve));

            if (curve.Kind == GridReferenceCurveKind.Line)
            {
                if (Distance(curve.Start, curve.End) <= tolerance)
                    throw new InvalidOperationException("Grid LINE reference has zero/near-zero length: " + curve.ElementId + ".");
                return;
            }

            if (curve.Kind != GridReferenceCurveKind.Arc) throw new InvalidOperationException("Unsupported Grid reference curve kind: " + curve.ElementId + ".");
            if (!IsFinite(curve.Center.X) || !IsFinite(curve.Center.Y) || !IsFinite(curve.Radius) || !IsFinite(curve.StartAngleRad) || !IsFinite(curve.SweepAngleRad))
                throw new ArgumentException("Grid ARC reference contains non-finite geometry: " + curve.ElementId + ".", nameof(curve));
            if (curve.Radius <= tolerance) throw new InvalidOperationException("Grid ARC reference radius is too small: " + curve.ElementId + ".");
            if (curve.SweepAngleRad <= 0.0 || curve.SweepAngleRad > TwoPi + 1e-10)
                throw new InvalidOperationException("Grid ARC sweep must be in (0, 2π]: " + curve.ElementId + ".");
        }

        private static InvalidOperationException Ambiguous(GridReferenceCurve first, GridReferenceCurve second, string reason)
        {
            return new InvalidOperationException("Grid intersection is ambiguous between " + first.ElementId + " and " + second.ElementId + ": " + reason + ".");
        }

        private static void Deduplicate(List<Point2> points, double tolerance)
        {
            for (var i = points.Count - 1; i >= 0; i--)
            {
                for (var j = 0; j < i; j++)
                {
                    if (Distance(points[i], points[j]) <= tolerance)
                    {
                        points.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }

        private static double NormalizeAngle(double angle)
        {
            var value = angle % TwoPi;
            return value < 0.0 ? value + TwoPi : value;
        }

        private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;
        private static double Length(double x, double y) => Math.Sqrt(x * x + y * y);
        private static double Distance(Point2 a, Point2 b) => Length(a.X - b.X, a.Y - b.Y);
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
