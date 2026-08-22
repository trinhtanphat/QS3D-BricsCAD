using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class WallPierPathProfileInput
    {
        public IReadOnlyList<Point2> Centerline { get; set; } = Array.Empty<Point2>();
        public double ThicknessM { get; set; }
        public double HeightM { get; set; }
        public WallPierProfileMode Mode { get; set; } = WallPierProfileMode.Rectangular;
        public double ChamferM { get; set; }
        public double MiterLimit { get; set; } = 4d;
        public double Tolerance { get; set; } = 1e-9d;
    }

    public sealed class WallPierPathProfile
    {
        internal WallPierPathProfile(
            IReadOnlyList<Point2> polygon,
            WallPierProfileMode mode,
            double centerlineLengthM,
            double thicknessM,
            double heightM,
            double chamferM,
            double footprintAreaM2,
            double footprintPerimeterM,
            double volumeM3,
            double lateralAreaM2,
            bool usedBevelJoin)
        {
            Polygon = polygon ?? throw new ArgumentNullException(nameof(polygon));
            Mode = mode;
            CenterlineLengthM = centerlineLengthM;
            ThicknessM = thicknessM;
            HeightM = heightM;
            ChamferM = chamferM;
            FootprintAreaM2 = footprintAreaM2;
            FootprintPerimeterM = footprintPerimeterM;
            VolumeM3 = volumeM3;
            LateralAreaM2 = lateralAreaM2;
            UsedBevelJoin = usedBevelJoin;
        }

        public IReadOnlyList<Point2> Polygon { get; }
        public WallPierProfileMode Mode { get; }
        public double CenterlineLengthM { get; }
        public double ThicknessM { get; }
        public double HeightM { get; }
        public double ChamferM { get; }
        public double FootprintAreaM2 { get; }
        public double FootprintPerimeterM { get; }
        public double VolumeM3 { get; }
        public double LateralAreaM2 { get; }
        public bool UsedBevelJoin { get; }
    }

    public static class WallPierPathProfilePlanner
    {
        private const double MachineEpsilon = 2.2204460492503131e-16d;

        public static WallPierPathProfile Plan(WallPierPathProfileInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var thickness = Positive(input.ThicknessM, nameof(input.ThicknessM));
            var height = Positive(input.HeightM, nameof(input.HeightM));
            var miterLimit = Positive(input.MiterLimit, nameof(input.MiterLimit));
            if (miterLimit < 1d) throw new ArgumentOutOfRangeException(nameof(input.MiterLimit), "Miter limit must be at least one.");
            var tolerance = Positive(input.Tolerance, nameof(input.Tolerance));
            var centerline = Clean(input.Centerline, tolerance);
            if (centerline.Count < 2) throw new ArgumentException("Wall-pier path requires at least two distinct centerline points.", nameof(input.Centerline));

            var footprint = new WallFootprintEngine().Build(centerline, thickness, miterLimit, tolerance);
            switch (input.Mode)
            {
                case WallPierProfileMode.Rectangular:
                    return BuildResult(footprint.Polygon, input.Mode, footprint.CenterlineLength, thickness, height, 0d, footprint.UsedBevelJoin);
                case WallPierProfileMode.Chamfered:
                {
                    var chamfer = Positive(input.ChamferM, nameof(input.ChamferM));
                    var polygon = ChamferTerminalCorners(centerline, footprint.Polygon, thickness, chamfer, tolerance);
                    return BuildResult(polygon, input.Mode, footprint.CenterlineLength, thickness, height, chamfer, footprint.UsedBevelJoin);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(input.Mode), "Unsupported wall-pier path profile mode: " + input.Mode);
            }
        }

        private static WallPierPathProfile BuildResult(
            IReadOnlyList<Point2> polygon,
            WallPierProfileMode mode,
            double centerlineLength,
            double thickness,
            double height,
            double chamfer,
            bool usedBevelJoin)
        {
            var area = Positive(PolygonArea(polygon), "wall-pier path footprint area");
            var perimeter = Positive(ClosedPerimeter(polygon), "wall-pier path footprint perimeter");
            var volume = Multiply(area, height, "wall-pier path volume");
            var lateral = Multiply(perimeter, height, "wall-pier path lateral area");
            return new WallPierPathProfile(
                new List<Point2>(polygon).AsReadOnly(),
                mode,
                Positive(centerlineLength, "wall-pier path centerline length"),
                thickness,
                height,
                chamfer,
                area,
                perimeter,
                volume,
                lateral,
                usedBevelJoin);
        }

        private static IReadOnlyList<Point2> ChamferTerminalCorners(
            IReadOnlyList<Point2> centerline,
            IReadOnlyList<Point2> polygon,
            double thickness,
            double chamfer,
            double tolerance)
        {
            if (polygon == null || polygon.Count < 4) throw new InvalidOperationException("Wall-pier footprint requires at least four vertices before chamfering.");
            var half = thickness / 2d;
            var first = Direction(centerline[0], centerline[1], tolerance);
            var last = Direction(centerline[centerline.Count - 2], centerline[centerline.Count - 1], tolerance);
            var start = centerline[0];
            var end = centerline[centerline.Count - 1];
            var expected = new[]
            {
                Offset(start, first.dx, first.dy, half, +1d),
                Offset(start, first.dx, first.dy, half, -1d),
                Offset(end, last.dx, last.dy, half, +1d),
                Offset(end, last.dx, last.dy, half, -1d)
            };

            var matchTolerance = TerminalMatchTolerance(polygon, thickness, tolerance);
            var targets = new HashSet<int>();
            foreach (var point in expected)
            {
                var index = FindUniqueVertex(polygon, point, matchTolerance);
                if (!targets.Add(index)) throw new InvalidOperationException("Wall-pier terminal footprint corners are not distinct.");
            }
            if (targets.Count != 4) throw new InvalidOperationException("Wall-pier terminal chamfer requires exactly four footprint corners.");

            foreach (var index in targets)
            {
                var corner = polygon[index];
                var previous = polygon[(index - 1 + polygon.Count) % polygon.Count];
                var next = polygon[(index + 1) % polygon.Count];
                var previousLength = corner.DistanceTo(previous);
                var nextLength = corner.DistanceTo(next);
                if (!(previousLength > 2d * chamfer + tolerance) || !(nextLength > 2d * chamfer + tolerance))
                    throw new InvalidOperationException("Wall-pier chamfer must be smaller than half both terminal footprint edges. Reduce WallPierChamferM or simplify the path end geometry.");
            }

            var result = new List<Point2>(polygon.Count + 4);
            for (var index = 0; index < polygon.Count; index++)
            {
                if (!targets.Contains(index))
                {
                    result.Add(polygon[index]);
                    continue;
                }
                var corner = polygon[index];
                var previous = polygon[(index - 1 + polygon.Count) % polygon.Count];
                var next = polygon[(index + 1) % polygon.Count];
                result.Add(PointAtDistance(corner, previous, chamfer));
                result.Add(PointAtDistance(corner, next, chamfer));
            }
            if (result.Count != polygon.Count + 4) throw new InvalidOperationException("Wall-pier terminal chamfer produced an unexpected vertex count.");
            return result.AsReadOnly();
        }

        private static double TerminalMatchTolerance(IReadOnlyList<Point2> polygon, double thickness, double tolerance)
        {
            var scale = CoordinateScale(polygon);
            var requested = Multiply(tolerance, 10d, "wall-pier terminal requested match tolerance");
            var precision = Multiply(scale, 32d * MachineEpsilon, "wall-pier terminal coordinate precision");
            var matchTolerance = Math.Max(requested, precision);
            var distinctCornerLimit = thickness / 4d;
            if (!(matchTolerance < distinctCornerLimit))
                throw new InvalidOperationException("Wall-pier terminal coordinates are too coarse relative to thickness for an unambiguous chamfer. Move the drawing closer to a numerically stable origin or increase profile thickness.");
            return matchTolerance;
        }

        private static (double dx, double dy) Direction(Point2 start, Point2 end, double tolerance)
        {
            var dx = Finite(end.X - start.X, "wall-pier path direction X");
            var dy = Finite(end.Y - start.Y, "wall-pier path direction Y");
            var length = start.DistanceTo(end);
            if (!(length > tolerance)) throw new InvalidOperationException("Wall-pier path contains a degenerate terminal segment.");
            return (dx / length, dy / length);
        }

        private static Point2 Offset(Point2 point, double dx, double dy, double half, double side)
        {
            var nx = -dy;
            var ny = dx;
            return Point(Finite(point.X + nx * half * side, "wall-pier terminal X"), Finite(point.Y + ny * half * side, "wall-pier terminal Y"));
        }

        private static int FindUniqueVertex(IReadOnlyList<Point2> polygon, Point2 expected, double tolerance)
        {
            var found = -1;
            for (var index = 0; index < polygon.Count; index++)
            {
                if (polygon[index].DistanceTo(expected) > tolerance) continue;
                if (found >= 0) throw new InvalidOperationException("Wall-pier terminal corner matched multiple footprint vertices.");
                found = index;
            }
            if (found < 0) throw new InvalidOperationException("Wall-pier terminal corner could not be mapped to the guarded wall footprint.");
            return found;
        }

        private static Point2 PointAtDistance(Point2 from, Point2 toward, double distance)
        {
            var edge = from.DistanceTo(toward);
            if (!(edge > distance)) throw new InvalidOperationException("Wall-pier chamfer exceeds a terminal footprint edge.");
            var ratio = distance / edge;
            return Point(
                Finite(from.X + (toward.X - from.X) * ratio, "wall-pier chamfer X"),
                Finite(from.Y + (toward.Y - from.Y) * ratio, "wall-pier chamfer Y"));
        }

        private static List<Point2> Clean(IReadOnlyList<Point2> source, double tolerance)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = new List<Point2>(source.Count);
            foreach (var point in source)
            {
                Validate(point, "wall-pier centerline point");
                if (result.Count == 0 || result[result.Count - 1].DistanceTo(point) > tolerance) result.Add(point);
            }
            return result;
        }

        private static double PolygonArea(IReadOnlyList<Point2> polygon)
        {
            if (polygon == null || polygon.Count < 3) throw new InvalidOperationException("Wall-pier profile polygon is degenerate.");
            return PolylineMetrics.Area(polygon);
        }

        private static double ClosedPerimeter(IReadOnlyList<Point2> polygon)
        {
            var sum = 0d;
            var compensation = 0d;
            for (var index = 0; index < polygon.Count; index++)
                AddCompensated(ref sum, ref compensation, polygon[index].DistanceTo(polygon[(index + 1) % polygon.Count]), "wall-pier path perimeter");
            return FinalizeCompensated(sum, compensation, "wall-pier path perimeter");
        }

        private static void AddCompensated(ref double sum, ref double compensation, double value, string label)
        {
            if (!IsFinite(sum) || !IsFinite(compensation) || !IsFinite(value))
                throw new OverflowException(label + " contains a non-finite value.");

            var next = sum + value;
            if (!IsFinite(next)) throw new OverflowException(label + " overflowed.");
            var correction = Math.Abs(sum) >= Math.Abs(value)
                ? (sum - next) + value
                : (value - next) + sum;
            var nextCompensation = compensation + correction;
            if (!IsFinite(nextCompensation)) throw new OverflowException(label + " overflowed.");

            sum = next == 0d ? 0d : next;
            compensation = nextCompensation == 0d ? 0d : nextCompensation;
        }

        private static double FinalizeCompensated(double sum, double compensation, string label)
        {
            if (!IsFinite(sum) || !IsFinite(compensation))
                throw new OverflowException(label + " contains a non-finite value.");
            var result = sum + compensation;
            if (!IsFinite(result)) throw new OverflowException(label + " overflowed.");
            return result == 0d ? 0d : result;
        }

        private static double CoordinateScale(IReadOnlyList<Point2> polygon)
        {
            var scale = 1d;
            foreach (var point in polygon)
            {
                scale = Math.Max(scale, Math.Abs(Finite(point.X, "wall-pier coordinate X")));
                scale = Math.Max(scale, Math.Abs(Finite(point.Y, "wall-pier coordinate Y")));
            }
            return scale;
        }

        private static Point2 Point(double x, double y)
        {
            var point = new Point2(x, y);
            Validate(point, "wall-pier path point");
            return point;
        }

        private static void Validate(Point2 point, string label)
        {
            Finite(point.X, label + " X");
            Finite(point.Y, label + " Y");
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (!(value > 0d)) throw new ArgumentOutOfRangeException(label, "Value must be greater than zero.");
            return value;
        }

        private static double Add(double left, double right, string label)
        {
            var value = Finite(left, label + " left") + Finite(right, label + " right");
            return Finite(value, label);
        }

        private static double Multiply(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var value = Finite(left * right, label);
            if (left != 0d && right != 0d && value == 0d)
                throw new OverflowException(label + " underflowed below the representable positive range.");
            return value;
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " must be finite.");
            return value;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
