using System;
using System.Collections.Generic;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public sealed class BeamStirrupLayoutInput
    {
        public double LengthM { get; set; }
        public double WidthM { get; set; }
        public double HeightM { get; set; }
        public double SectionCoverM { get; set; }
        public double EndCoverM { get; set; }
        public double DiameterMm { get; set; }
        public double? SpacingMm { get; set; }
        public int? Count { get; set; }
        public double BendRadiusM { get; set; }
        public double MaximumSagittaM { get; set; } = 0.001d;
        public double HookLengthM { get; set; }
        public double HookTailAngleDeg { get; set; }
    }

    public sealed class BeamStirrupLayout
    {
        public BeamStirrupLayout(IReadOnlyList<double> stationOffsetsM, IReadOnlyList<Point2> sectionLoop, double actualSpacingM)
            : this(stationOffsetsM, sectionLoop, actualSpacingM, PolylineLength(sectionLoop), PolylineLength(sectionLoop), false, 0d)
        {
            if (SectionLoop.Count != 5) throw new ArgumentException("Legacy beam stirrup section loop must contain four corners plus the closing point.", nameof(sectionLoop));
        }

        internal BeamStirrupLayout(
            IReadOnlyList<double> stationOffsetsM,
            IReadOnlyList<Point2> sectionLoop,
            double actualSpacingM,
            double centerlineLengthM,
            double polylineLengthM,
            bool hasHookTails,
            double bendRadiusM)
        {
            if (stationOffsetsM == null) throw new ArgumentNullException(nameof(stationOffsetsM));
            if (sectionLoop == null) throw new ArgumentNullException(nameof(sectionLoop));
            StationOffsetsM = new List<double>(stationOffsetsM).AsReadOnly();
            SectionLoop = new List<Point2>(sectionLoop).AsReadOnly();
            if (SectionLoop.Count < 5) throw new ArgumentException("Beam stirrup section path requires at least five points.", nameof(sectionLoop));
            ActualSpacingM = Finite(actualSpacingM, nameof(actualSpacingM));
            CenterlineLengthM = Positive(centerlineLengthM, nameof(centerlineLengthM));
            PolylineLengthM = Positive(polylineLengthM, nameof(polylineLengthM));
            HasHookTails = hasHookTails;
            BendRadiusM = NonNegative(bendRadiusM, nameof(bendRadiusM));
        }

        public IReadOnlyList<double> StationOffsetsM { get; }
        public IReadOnlyList<Point2> SectionLoop { get; }
        public int Count => StationOffsetsM.Count;
        public double ActualSpacingM { get; }
        public double CenterlineLengthM { get; }
        public double PolylineLengthM { get; }
        public bool HasHookTails { get; }
        public double BendRadiusM { get; }

        private static double PolylineLength(IReadOnlyList<Point2> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            var total = 0d;
            for (var index = 1; index < points.Count; index++)
            {
                var segment = points[index - 1].DistanceTo(points[index]);
                total = Finite(total + segment, "beam stirrup path length");
            }
            return total;
        }

        private static double Positive(double value, string name)
        {
            value = Finite(value, name);
            if (!(value > 0d)) throw new ArgumentOutOfRangeException(name);
            return value;
        }

        private static double NonNegative(double value, string name)
        {
            value = Finite(value, name);
            if (value < 0d) throw new ArgumentOutOfRangeException(name);
            return value;
        }

        private static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public static class BeamStirrupLayoutPlanner
    {
        private static readonly double QuarterCircleBulge = Math.Tan(Math.PI / 8d);

        public static BeamStirrupLayout Plan(BeamStirrupLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var lengthM = RebarMath.Positive(input.LengthM, nameof(input.LengthM));
            var widthM = RebarMath.Positive(input.WidthM, nameof(input.WidthM));
            var heightM = RebarMath.Positive(input.HeightM, nameof(input.HeightM));
            var sectionCoverM = RebarMath.NonNegative(input.SectionCoverM, nameof(input.SectionCoverM));
            var endCoverM = RebarMath.NonNegative(input.EndCoverM, nameof(input.EndCoverM));
            var diameterMm = RebarMath.Positive(input.DiameterMm, nameof(input.DiameterMm));
            var bendRadiusM = RebarMath.NonNegative(input.BendRadiusM, nameof(input.BendRadiusM));
            var maximumSagittaM = RebarMath.Positive(input.MaximumSagittaM, nameof(input.MaximumSagittaM));
            var hookLengthM = RebarMath.NonNegative(input.HookLengthM, nameof(input.HookLengthM));
            var hookTailAngleDeg = Finite(input.HookTailAngleDeg, nameof(input.HookTailAngleDeg));
            if (hookLengthM > 0d && !(hookTailAngleDeg > 0d && hookTailAngleDeg < 180d))
                throw new ArgumentOutOfRangeException(nameof(input.HookTailAngleDeg), "Hook tail angle must be strictly between 0 and 180 degrees when a hook tail is enabled.");
            if (hookLengthM <= 1e-12d && Math.Abs(hookTailAngleDeg) > 1e-12d)
                throw new InvalidOperationException("HookTailAngleDeg must be zero when HookLengthM is zero.");

            var stations = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = lengthM,
                CoverM = endCoverM,
                DiameterMm = diameterMm,
                SpacingMm = input.SpacingMm,
                Count = input.Count
            });
            var diameterM = RebarMath.Divide(diameterMm, 1000d, "beam stirrup diameter");
            if (stations.Count > 1 && stations.ActualSpacingM + 1e-12d < diameterM)
                throw new InvalidOperationException("Beam stirrup centers are closer than one stirrup diameter.");

            var radiusM = RebarMath.Divide(diameterMm, 2000d, "beam stirrup radius");
            var centerCoverM = RebarMath.Add(sectionCoverM, radiusM, "beam stirrup center cover");
            var halfWidthM = Finite(RebarMath.Divide(widthM, 2d, "beam stirrup half width") - centerCoverM, "beam stirrup clear half width");
            var halfHeightM = Finite(RebarMath.Divide(heightM, 2d, "beam stirrup half height") - centerCoverM, "beam stirrup clear half height");
            if (!(halfWidthM > 0d)) throw new InvalidOperationException("Section cover + stirrup radius leaves no usable beam width.");
            if (!(halfHeightM > 0d)) throw new InvalidOperationException("Section cover + stirrup radius leaves no usable beam height.");
            if (bendRadiusM > Math.Min(halfWidthM, halfHeightM) + 1e-12d)
                throw new InvalidOperationException("RebarStirrupBendRadiusM exceeds half of the available stirrup centerline envelope.");

            if (bendRadiusM <= 1e-12d && hookLengthM <= 1e-12d)
            {
                var legacyLoop = new List<Point2>(5)
                {
                    new Point2(-halfWidthM, -halfHeightM),
                    new Point2( halfWidthM, -halfHeightM),
                    new Point2( halfWidthM,  halfHeightM),
                    new Point2(-halfWidthM,  halfHeightM),
                    new Point2(-halfWidthM, -halfHeightM)
                };
                var perimeter = Finite(4d * (halfWidthM + halfHeightM), "beam stirrup centerline perimeter");
                return new BeamStirrupLayout(stations.OffsetsM, legacyLoop.AsReadOnly(), stations.ActualSpacingM, perimeter, perimeter, false, 0d);
            }

            var points = new List<Point2>();
            var closure = new Point2(0d, -halfHeightM);
            if (hookLengthM > 1e-12d)
            {
                var angle = RebarMath.Divide(
                    RebarMath.Multiply(hookTailAngleDeg, Math.PI, "beam stirrup hook tail angle radians"),
                    180d,
                    "beam stirrup hook tail angle radians");
                var startTail = new Point2(
                    closure.X + hookLengthM * Math.Cos(Math.PI - angle),
                    closure.Y + hookLengthM * Math.Sin(Math.PI - angle));
                EnsureInside(startTail, halfWidthM, halfHeightM, "start hook tail");
                Append(points, startTail);
            }
            Append(points, closure);

            if (bendRadiusM <= 1e-12d)
            {
                Append(points, new Point2(halfWidthM, -halfHeightM));
                Append(points, new Point2(halfWidthM, halfHeightM));
                Append(points, new Point2(-halfWidthM, halfHeightM));
                Append(points, new Point2(-halfWidthM, -halfHeightM));
            }
            else
            {
                var bottomRightStart = new Point2(halfWidthM - bendRadiusM, -halfHeightM);
                Append(points, bottomRightStart);
                AppendQuarterArc(points, bottomRightStart, new Point2(halfWidthM, -halfHeightM + bendRadiusM), maximumSagittaM);

                var topRightStart = new Point2(halfWidthM, halfHeightM - bendRadiusM);
                Append(points, topRightStart);
                AppendQuarterArc(points, topRightStart, new Point2(halfWidthM - bendRadiusM, halfHeightM), maximumSagittaM);

                var topLeftStart = new Point2(-halfWidthM + bendRadiusM, halfHeightM);
                Append(points, topLeftStart);
                AppendQuarterArc(points, topLeftStart, new Point2(-halfWidthM, halfHeightM - bendRadiusM), maximumSagittaM);

                var bottomLeftStart = new Point2(-halfWidthM, -halfHeightM + bendRadiusM);
                Append(points, bottomLeftStart);
                AppendQuarterArc(points, bottomLeftStart, new Point2(-halfWidthM + bendRadiusM, -halfHeightM), maximumSagittaM);
            }

            Append(points, closure);
            if (hookLengthM > 1e-12d)
            {
                var angle = RebarMath.Divide(
                    RebarMath.Multiply(hookTailAngleDeg, Math.PI, "beam stirrup hook tail angle radians"),
                    180d,
                    "beam stirrup hook tail angle radians");
                var endTail = new Point2(
                    closure.X + hookLengthM * Math.Cos(angle),
                    closure.Y + hookLengthM * Math.Sin(angle));
                EnsureInside(endTail, halfWidthM, halfHeightM, "end hook tail");
                Append(points, endTail);
            }

            var centerWidthM = halfWidthM * 2d;
            var centerHeightM = halfHeightM * 2d;
            var straightLengthM = Finite(2d * (centerWidthM + centerHeightM) - 8d * bendRadiusM, "beam stirrup straight centerline length");
            if (straightLengthM < -1e-12d) throw new InvalidOperationException("Stirrup bend radius leaves a negative straight centerline length.");
            straightLengthM = Math.Max(0d, straightLengthM);
            var arcLengthM = Finite(2d * Math.PI * bendRadiusM, "beam stirrup bend centerline length");
            var hookTotalM = Finite(2d * hookLengthM, "beam stirrup hook centerline length");
            var centerlineLengthM = Finite(straightLengthM + arcLengthM + hookTotalM, "beam stirrup centerline length");
            var polylineLengthM = PolylineLength(points);
            return new BeamStirrupLayout(
                stations.OffsetsM,
                points.AsReadOnly(),
                stations.ActualSpacingM,
                centerlineLengthM,
                polylineLengthM,
                hookLengthM > 1e-12d,
                bendRadiusM);
        }

        private static void AppendQuarterArc(List<Point2> points, Point2 start, Point2 end, double maximumSagittaM)
        {
            var arc = BulgeArcTessellator.Tessellate(start, end, QuarterCircleBulge, maximumSagittaM);
            for (var index = 1; index < arc.Count; index++) Append(points, arc[index]);
        }

        private static void Append(List<Point2> points, Point2 point)
        {
            if (points.Count > 0 && points[points.Count - 1].DistanceTo(point) <= 1e-12d) return;
            points.Add(point);
        }

        private static void EnsureInside(Point2 point, double halfWidthM, double halfHeightM, string label)
        {
            const double tolerance = 1e-12d;
            if (point.X < -halfWidthM - tolerance || point.X > halfWidthM + tolerance || point.Y < -halfHeightM - tolerance || point.Y > halfHeightM + tolerance)
                throw new InvalidOperationException(label + " extends outside the available stirrup centerline envelope. Reduce hook length or change hook angle.");
        }

        private static double PolylineLength(IReadOnlyList<Point2> points)
        {
            var total = 0d;
            for (var index = 1; index < points.Count; index++)
                total = Finite(total + points[index - 1].DistanceTo(points[index]), "beam stirrup tessellated length");
            return total;
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " is not finite.");
            return value;
        }
    }
}
