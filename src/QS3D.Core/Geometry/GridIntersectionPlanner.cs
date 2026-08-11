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
        private const int MaxElementIdLength = 128;

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
            ElementId = NormalizeElementId(elementId);
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

        private static string NormalizeElementId(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId)) throw new ArgumentException("Grid element id is required.", nameof(elementId));
            var normalized = elementId.Trim();
            if (normalized.Length > MaxElementIdLength)
                throw new ArgumentException("Grid element id exceeds " + MaxElementIdLength + " characters.", nameof(elementId));
            return normalized;
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

            var list = curves.Take(MaxCurves + 1).ToList();
            if (list.Count > MaxCurves) throw new InvalidOperationException("Grid intersection planning supports at most " + MaxCurves + " curves.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < list.Count; i++)
            {
                Validate(list[i], tolerance, i);
                if (!ids.Add(list[i].ElementId))
                    throw new InvalidOperationException("Grid intersection input contains duplicate element id: " + list[i].ElementId + ".");
            }
            if (list.Count < 2) return Array.Empty<GridIntersection>();

            var result = new List<GridIntersection>();
            for (var i = 0; i < list.Count - 1; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    var points = IntersectPair(list[i], list[j], tolerance);
                    foreach (var point in points)
                    {
                        EnsureFinitePoint(point, "Grid intersection result");
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
            EnsureFiniteDerived("Grid LINE direction", rx, ry, sx, sy);
            var rxs = Cross(rx, ry, sx, sy);
            var qpx = bx - ax;
            var qpy = by - ay;
            EnsureFiniteDerived("Grid LINE offset", qpx, qpy);
            var rLength = Length(rx, ry);
            var sLength = Length(sx, sy);
            var scale = rLength * sLength;
            EnsureFiniteDerived("Grid LINE direction scale", scale);
            var crossTolerance = tolerance * Math.Max(1.0, scale);
            EnsureFiniteDerived("Grid LINE cross tolerance", crossTolerance);

            if (Math.Abs(rxs) <= crossTolerance)
            {
                var collinearTolerance = tolerance * Math.Max(1.0, rLength);
                EnsureFiniteDerived("Grid LINE collinearity tolerance", collinearTolerance);
                if (Math.Abs(Cross(qpx, qpy, rx, ry)) > collinearTolerance)
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
            EnsureFiniteDerived("Grid LINE intersection parameters", t, u);
            var paramTolerance = tolerance / Math.Max(tolerance, Math.Min(rLength, sLength));
            EnsureFiniteDerived("Grid LINE parameter tolerance", paramTolerance);
            if (t < -paramTolerance || t > 1.0 + paramTolerance || u < -paramTolerance || u > 1.0 + paramTolerance)
                return Array.Empty<Point2>();

            t = Clamp01(t);
            var point = new Point2(ax + t * rx, ay + t * ry);
            EnsureFinitePoint(point, "Grid LINE intersection");
            return new[] { point };
        }

        private static IReadOnlyList<Point2> IntersectLineArc(GridReferenceCurve line, GridReferenceCurve arc, double tolerance)
        {
            var dx = line.End.X - line.Start.X;
            var dy = line.End.Y - line.Start.Y;
            var fx = line.Start.X - arc.Center.X;
            var fy = line.Start.Y - arc.Center.Y;
            EnsureFiniteDerived("Grid LINE/ARC delta", dx, dy, fx, fy);
            var a = dx * dx + dy * dy;
            var b = 2.0 * (fx * dx + fy * dy);
            var c = fx * fx + fy * fy - arc.Radius * arc.Radius;
            EnsureFiniteDerived("Grid LINE/ARC quadratic", a, b, c);
            var discriminant = b * b - 4.0 * a * c;
            var discTolerance = tolerance * Math.Max(1.0, b * b + Math.Abs(4.0 * a * c));
            EnsureFiniteDerived("Grid LINE/ARC discriminant", discriminant, discTolerance);
            if (discriminant < -discTolerance) return Array.Empty<Point2>();
            if (Math.Abs(discriminant) <= discTolerance) discriminant = 0.0;

            var roots = new List<double>(2);
            if (discriminant == 0.0)
            {
                var root = -b / (2.0 * a);
                EnsureFiniteDerived("Grid LINE/ARC tangent root", root);
                roots.Add(root);
            }
            else
            {
                var sqrt = Math.Sqrt(discriminant);
                var firstRoot = (-b - sqrt) / (2.0 * a);
                var secondRoot = (-b + sqrt) / (2.0 * a);
                EnsureFiniteDerived("Grid LINE/ARC roots", sqrt, firstRoot, secondRoot);
                roots.Add(firstRoot);
                roots.Add(secondRoot);
            }
            roots.Sort();

            var lineLength = Length(dx, dy);
            var paramTolerance = tolerance / Math.Max(tolerance, lineLength);
            EnsureFiniteDerived("Grid LINE/ARC parameter tolerance", paramTolerance);
            var points = new List<Point2>(2);
            foreach (var root in roots)
            {
                if (root < -paramTolerance || root > 1.0 + paramTolerance) continue;
                var t = Clamp01(root);
                var point = new Point2(line.Start.X + t * dx, line.Start.Y + t * dy);
                EnsureFinitePoint(point, "Grid LINE/ARC intersection");
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
            EnsureFiniteDerived("Grid ARC center delta", dx, dy);
            var distance = Length(dx, dy);

            if (distance <= tolerance && Math.Abs(first.Radius - second.Radius) <= tolerance)
                throw Ambiguous(first, second, "coincident ARC support circles are intentionally rejected; split/review the Grid references explicitly");
            if (distance <= tolerance) return Array.Empty<Point2>();
            var outerLimit = first.Radius + second.Radius + tolerance;
            var innerLimit = Math.Abs(first.Radius - second.Radius) - tolerance;
            EnsureFiniteDerived("Grid ARC separation limits", outerLimit, innerLimit);
            if (distance > outerLimit) return Array.Empty<Point2>();
            if (distance < innerLimit) return Array.Empty<Point2>();

            var a = (first.Radius * first.Radius - second.Radius * second.Radius + distance * distance) / (2.0 * distance);
            var h2 = first.Radius * first.Radius - a * a;
            var hTolerance = tolerance * Math.Max(1.0, first.Radius * first.Radius);
            EnsureFiniteDerived("Grid ARC intersection geometry", a, h2, hTolerance);
            if (h2 < -hTolerance) return Array.Empty<Point2>();
            if (h2 < 0.0) h2 = 0.0;
            var h = Math.Sqrt(h2);
            var ux = dx / distance;
            var uy = dy / distance;
            var px = first.Center.X + a * ux;
            var py = first.Center.Y + a * uy;
            EnsureFiniteDerived("Grid ARC intersection basis", h, ux, uy, px, py);

            var points = new List<Point2>(2);
            var p1 = new Point2(px - h * uy, py + h * ux);
            EnsureFinitePoint(p1, "Grid ARC intersection");
            if (IsOnArc(p1, first, tolerance) && IsOnArc(p1, second, tolerance)) points.Add(p1);
            if (h > tolerance)
            {
                var p2 = new Point2(px + h * uy, py - h * ux);
                EnsureFinitePoint(p2, "Grid ARC intersection");
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
            EnsureFinitePoint(point, "Grid ARC point");
            var dx = point.X - arc.Center.X;
            var dy = point.Y - arc.Center.Y;
            EnsureFiniteDerived("Grid ARC point delta", dx, dy);
            var radius = Length(dx, dy);
            if (Math.Abs(radius - arc.Radius) > tolerance) return false;
            if (arc.SweepAngleRad >= TwoPi - tolerance / Math.Max(arc.Radius, tolerance)) return true;
            var angle = NormalizeAngle(Math.Atan2(dy, dx));
            var start = NormalizeAngle(arc.StartAngleRad);
            var delta = NormalizeAngle(angle - start);
            var angularTolerance = tolerance / Math.Max(arc.Radius, tolerance);
            EnsureFiniteDerived("Grid ARC angular test", angle, start, delta, angularTolerance);
            return delta <= arc.SweepAngleRad + angularTolerance;
        }

        private static void AddIfOnBoth(List<Point2> points, Point2 point, GridReferenceCurve first, GridReferenceCurve second, double tolerance)
        {
            if (IsOnLineSegment(point, first, tolerance) && IsOnLineSegment(point, second, tolerance)) points.Add(point);
        }

        private static bool IsOnLineSegment(Point2 point, GridReferenceCurve line, double tolerance)
        {
            EnsureFinitePoint(point, "Grid LINE point");
            var dx = line.End.X - line.Start.X;
            var dy = line.End.Y - line.Start.Y;
            var px = point.X - line.Start.X;
            var py = point.Y - line.Start.Y;
            EnsureFiniteDerived("Grid LINE point delta", dx, dy, px, py);
            var length = Length(dx, dy);
            var crossTolerance = tolerance * Math.Max(1.0, length);
            EnsureFiniteDerived("Grid LINE point tolerance", crossTolerance);
            if (Math.Abs(Cross(px, py, dx, dy)) > crossTolerance) return false;
            var dot = px * dx + py * dy;
            var length2 = dx * dx + dy * dy;
            var paramTolerance = tolerance / Math.Max(tolerance, length);
            var lower = -paramTolerance * length2;
            var upper = (1.0 + paramTolerance) * length2;
            EnsureFiniteDerived("Grid LINE point projection", dot, length2, paramTolerance, lower, upper);
            return dot >= lower && dot <= upper;
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
            if (!IsFinite(value)) throw new OverflowException("Grid intersection parameter is not finite.");
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }

        private static double NormalizeAngle(double angle)
        {
            if (!IsFinite(angle)) throw new OverflowException("Grid intersection angle is not finite.");
            var value = angle % TwoPi;
            return value < 0.0 ? value + TwoPi : value;
        }

        private static double Cross(double ax, double ay, double bx, double by)
        {
            EnsureFiniteDerived("Grid intersection cross-product input", ax, ay, bx, by);
            var value = ax * by - ay * bx;
            if (!IsFinite(value)) throw new OverflowException("Grid intersection cross product exceeds the supported numeric range.");
            return value;
        }

        private static double Length(double x, double y)
        {
            EnsureFiniteDerived("Grid intersection length input", x, y);
            var ax = Math.Abs(x);
            var ay = Math.Abs(y);
            var scale = Math.Max(ax, ay);
            if (scale == 0.0) return 0.0;
            var ratio = Math.Min(ax, ay) / scale;
            var value = scale * Math.Sqrt(1.0 + ratio * ratio);
            if (!IsFinite(value)) throw new OverflowException("Grid intersection length exceeds the supported numeric range.");
            return value;
        }

        private static double Distance(Point2 a, Point2 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            EnsureFiniteDerived("Grid intersection point delta", dx, dy);
            return Length(dx, dy);
        }

        private static void EnsureFinitePoint(Point2 point, string context)
        {
            if (!IsFinite(point.X) || !IsFinite(point.Y))
                throw new OverflowException(context + " is not finite.");
        }

        private static void EnsureFiniteDerived(string context, params double[] values)
        {
            if (values.Any(value => !IsFinite(value)))
                throw new OverflowException(context + " exceeds the supported numeric range.");
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
