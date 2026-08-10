using System;
using System.Collections.Generic;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public sealed class RectangularStirrupInput
    {
        public double WidthM { get; set; }
        public double DepthM { get; set; }
        public double CoverM { get; set; }
        public double DiameterMm { get; set; }
        public double BendRadiusM { get; set; }
        public double MaximumSagittaM { get; set; } = 0.001d;
        public double HookLengthM { get; set; }
        public double HookTailAngleDeg { get; set; }
    }

    public sealed class RectangularStirrupPlan
    {
        public RectangularStirrupPlan(
            RebarShapePath path,
            double centerlineWidthM,
            double centerlineDepthM,
            double centerlineLengthM,
            double polylineLengthM)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            CenterlineWidthM = centerlineWidthM;
            CenterlineDepthM = centerlineDepthM;
            CenterlineLengthM = centerlineLengthM;
            PolylineLengthM = polylineLengthM;
        }

        public RebarShapePath Path { get; }
        public double CenterlineWidthM { get; }
        public double CenterlineDepthM { get; }
        public double CenterlineLengthM { get; }
        public double PolylineLengthM { get; }
    }

    public sealed class RectangularStirrupSetInput
    {
        public RectangularStirrupInput Shape { get; set; } = new RectangularStirrupInput();
        public double HostSpanM { get; set; }
        public double EndCoverM { get; set; }
        public double? SpacingMm { get; set; }
        public int? Count { get; set; }
    }

    public sealed class RectangularStirrupSetPlan
    {
        public RectangularStirrupSetPlan(RectangularStirrupPlan shape, LinearRebarLayout distribution, double totalCenterlineLengthM)
        {
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
            Distribution = distribution ?? throw new ArgumentNullException(nameof(distribution));
            TotalCenterlineLengthM = totalCenterlineLengthM;
        }

        public RectangularStirrupPlan Shape { get; }
        public LinearRebarLayout Distribution { get; }
        public double TotalCenterlineLengthM { get; }
    }

    public static class RectangularStirrupPlanner
    {
        private static readonly double QuarterCircleBulge = Math.Tan(Math.PI / 8d);

        public static RectangularStirrupPlan Plan(RectangularStirrupInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var width = RebarMath.Positive(input.WidthM, nameof(input.WidthM));
            var depth = RebarMath.Positive(input.DepthM, nameof(input.DepthM));
            var cover = RebarMath.NonNegative(input.CoverM, nameof(input.CoverM));
            var diameterMm = RebarMath.Positive(input.DiameterMm, nameof(input.DiameterMm));
            var bendRadius = RebarMath.NonNegative(input.BendRadiusM, nameof(input.BendRadiusM));
            var sagitta = RebarMath.Positive(input.MaximumSagittaM, nameof(input.MaximumSagittaM));
            var hookLength = RebarMath.NonNegative(input.HookLengthM, nameof(input.HookLengthM));
            var hookAngle = Finite(input.HookTailAngleDeg, nameof(input.HookTailAngleDeg));

            if (hookLength > 0d && !(hookAngle > 0d && hookAngle < 180d))
                throw new ArgumentOutOfRangeException(nameof(input.HookTailAngleDeg), "Hook tail angle must be strictly between 0 and 180 degrees when hooks are enabled.");
            if (hookLength == 0d && Math.Abs(hookAngle) > 1e-12d)
                throw new InvalidOperationException("HookTailAngleDeg must be zero when HookLengthM is zero.");

            var radiusM = RebarMath.Divide(diameterMm, 2000d, "stirrup bar radius");
            var centerlineClearance = RebarMath.Add(cover, radiusM, "stirrup centerline clearance");
            var doubleClearance = RebarMath.Multiply(centerlineClearance, 2d, "stirrup two-side clearance");
            var centerWidth = Finite(width - doubleClearance, "stirrup centerline width");
            var centerDepth = Finite(depth - doubleClearance, "stirrup centerline depth");
            if (!(centerWidth > 0d) || !(centerDepth > 0d))
                throw new InvalidOperationException("Cover + bar radius leaves no valid rectangular stirrup centerline inside the host section.");

            var halfWidth = centerWidth / 2d;
            var halfDepth = centerDepth / 2d;
            var maximumBend = Math.Min(halfWidth, halfDepth);
            if (bendRadius > maximumBend + 1e-12d)
                throw new InvalidOperationException("BendRadiusM exceeds half of the available stirrup centerline envelope.");

            var points = new List<Point2>();
            var closure = new Point2(0d, -halfDepth);
            if (hookLength > 0d)
            {
                var angle = hookAngle * Math.PI / 180d;
                var startTail = new Point2(closure.X + hookLength * Math.Cos(Math.PI - angle), closure.Y + hookLength * Math.Sin(Math.PI - angle));
                EnsureInside(startTail, halfWidth, halfDepth, "start hook tail");
                Append(points, startTail);
            }
            Append(points, closure);

            if (bendRadius <= 1e-12d)
            {
                Append(points, new Point2(halfWidth, -halfDepth));
                Append(points, new Point2(halfWidth, halfDepth));
                Append(points, new Point2(-halfWidth, halfDepth));
                Append(points, new Point2(-halfWidth, -halfDepth));
            }
            else
            {
                var bottomRightStart = new Point2(halfWidth - bendRadius, -halfDepth);
                Append(points, bottomRightStart);
                AppendArc(points, bottomRightStart, new Point2(halfWidth, -halfDepth + bendRadius), sagitta);

                var topRightStart = new Point2(halfWidth, halfDepth - bendRadius);
                Append(points, topRightStart);
                AppendArc(points, topRightStart, new Point2(halfWidth - bendRadius, halfDepth), sagitta);

                var topLeftStart = new Point2(-halfWidth + bendRadius, halfDepth);
                Append(points, topLeftStart);
                AppendArc(points, topLeftStart, new Point2(-halfWidth, halfDepth - bendRadius), sagitta);

                var bottomLeftStart = new Point2(-halfWidth, -halfDepth + bendRadius);
                Append(points, bottomLeftStart);
                AppendArc(points, bottomLeftStart, new Point2(-halfWidth + bendRadius, -halfDepth), sagitta);
            }

            Append(points, closure);
            if (hookLength > 0d)
            {
                var angle = hookAngle * Math.PI / 180d;
                var endTail = new Point2(closure.X + hookLength * Math.Cos(angle), closure.Y + hookLength * Math.Sin(angle));
                EnsureInside(endTail, halfWidth, halfDepth, "end hook tail");
                Append(points, endTail);
            }

            if (points.Count < 5) throw new InvalidOperationException("Rectangular stirrup path is incomplete.");
            var loopStraight = Finite(2d * (centerWidth + centerDepth) - 8d * bendRadius, "stirrup straight length");
            if (loopStraight < -1e-12d) throw new InvalidOperationException("Stirrup bend radius leaves a negative straight segment length.");
            loopStraight = Math.Max(0d, loopStraight);
            var loopArc = Finite(2d * Math.PI * bendRadius, "stirrup bend length");
            var hookTotal = RebarMath.Multiply(hookLength, 2d, "stirrup hook length");
            var centerlineLength = Finite(loopStraight + loopArc + hookTotal, "stirrup centerline length");
            var polylineLength = PathLength(points);

            var shapePoints = new List<RebarShapePoint>(points.Count);
            foreach (var point in points) shapePoints.Add(new RebarShapePoint(point.X, point.Y));
            var path = new RebarShapePath("RECT-STIRRUP", shapePoints.AsReadOnly());
            return new RectangularStirrupPlan(path, centerWidth, centerDepth, centerlineLength, polylineLength);
        }

        public static RectangularStirrupSetPlan PlanSet(RectangularStirrupSetInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var shape = Plan(input.Shape ?? throw new ArgumentNullException(nameof(input.Shape)));
            var distribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = input.HostSpanM,
                CoverM = input.EndCoverM,
                DiameterMm = input.Shape.DiameterMm,
                SpacingMm = input.SpacingMm,
                Count = input.Count
            });
            var total = RebarMath.Multiply(shape.CenterlineLengthM, distribution.Count, "stirrup set total centerline length");
            return new RectangularStirrupSetPlan(shape, distribution, total);
        }

        private static void AppendArc(List<Point2> points, Point2 start, Point2 end, double maximumSagitta)
        {
            var arc = BulgeArcTessellator.Tessellate(start, end, QuarterCircleBulge, maximumSagitta);
            for (var index = 1; index < arc.Count; index++) Append(points, arc[index]);
        }

        private static void Append(List<Point2> points, Point2 point)
        {
            Finite(point.X, "stirrup point X");
            Finite(point.Y, "stirrup point Y");
            if (points.Count > 0 && points[points.Count - 1].DistanceTo(point) <= 1e-12d) return;
            points.Add(point);
        }

        private static void EnsureInside(Point2 point, double halfWidth, double halfDepth, string label)
        {
            const double tolerance = 1e-12d;
            if (point.X < -halfWidth - tolerance || point.X > halfWidth + tolerance || point.Y < -halfDepth - tolerance || point.Y > halfDepth + tolerance)
                throw new InvalidOperationException(label + " extends outside the available stirrup centerline envelope. Reduce HookLengthM or change HookTailAngleDeg.");
        }

        private static double PathLength(IReadOnlyList<Point2> points)
        {
            var total = 0d;
            for (var index = 1; index < points.Count; index++)
            {
                var segment = points[index - 1].DistanceTo(points[index]);
                if (double.IsNaN(segment) || double.IsInfinity(segment)) throw new OverflowException("Stirrup path segment length is not finite.");
                total = Finite(total + segment, "stirrup polyline length");
            }
            return total;
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " is not finite.");
            return value;
        }
    }
}
