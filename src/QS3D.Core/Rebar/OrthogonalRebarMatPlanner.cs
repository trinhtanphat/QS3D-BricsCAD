using System;
using System.Collections.Generic;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public enum OrthogonalRebarMatFace
    {
        Bottom,
        Top
    }

    public enum OrthogonalRebarDirection
    {
        X,
        Y
    }

    public sealed class OrthogonalRebarMatInput
    {
        public double WidthM { get; set; }
        public double DepthM { get; set; }
        public double ThicknessM { get; set; }
        public double CoverM { get; set; }
        public double XDiameterMm { get; set; }
        public double YDiameterMm { get; set; }
        public double XSpacingMm { get; set; }
        public double YSpacingMm { get; set; }
        public bool BottomEnabled { get; set; } = true;
        public bool TopEnabled { get; set; }
    }

    public sealed class OrthogonalRebarMatBar
    {
        public OrthogonalRebarMatBar(OrthogonalRebarMatFace face, OrthogonalRebarDirection direction, Point2 start, Point2 end, double elevationFromBottomM, double diameterMm)
        {
            Face = face;
            Direction = direction;
            Start = start;
            End = end;
            ElevationFromBottomM = elevationFromBottomM;
            DiameterMm = diameterMm;
        }

        public OrthogonalRebarMatFace Face { get; }
        public OrthogonalRebarDirection Direction { get; }
        public Point2 Start { get; }
        public Point2 End { get; }
        public double ElevationFromBottomM { get; }
        public double DiameterMm { get; }
    }

    public sealed class OrthogonalRebarMatLayout
    {
        public OrthogonalRebarMatLayout(IReadOnlyList<OrthogonalRebarMatBar> bars, double xActualSpacingM, double yActualSpacingM)
        {
            Bars = bars ?? throw new ArgumentNullException(nameof(bars));
            XActualSpacingM = xActualSpacingM;
            YActualSpacingM = yActualSpacingM;
        }

        public IReadOnlyList<OrthogonalRebarMatBar> Bars { get; }
        public int Count => Bars.Count;
        public double XActualSpacingM { get; }
        public double YActualSpacingM { get; }
    }

    public static class OrthogonalRebarMatPlanner
    {
        public const int MaxBars = 10000;

        public static OrthogonalRebarMatLayout Plan(OrthogonalRebarMatInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var widthM = RebarMath.Positive(input.WidthM, nameof(input.WidthM));
            var depthM = RebarMath.Positive(input.DepthM, nameof(input.DepthM));
            var thicknessM = RebarMath.Positive(input.ThicknessM, nameof(input.ThicknessM));
            var coverM = RebarMath.NonNegative(input.CoverM, nameof(input.CoverM));
            var xDiameterMm = RebarMath.Positive(input.XDiameterMm, nameof(input.XDiameterMm));
            var yDiameterMm = RebarMath.Positive(input.YDiameterMm, nameof(input.YDiameterMm));
            var xSpacingMm = RebarMath.Positive(input.XSpacingMm, nameof(input.XSpacingMm));
            var ySpacingMm = RebarMath.Positive(input.YSpacingMm, nameof(input.YSpacingMm));
            if (!input.BottomEnabled && !input.TopEnabled) throw new InvalidOperationException("At least one rebar mat face must be enabled.");

            var xRadiusM = RebarMath.Divide(xDiameterMm, 2000d, "mat X radius");
            var yRadiusM = RebarMath.Divide(yDiameterMm, 2000d, "mat Y radius");
            var crossCenterGapM = RebarMath.Add(xRadiusM, yRadiusM, "mat cross-layer center gap");

            var xDistribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = depthM,
                CoverM = coverM,
                DiameterMm = xDiameterMm,
                SpacingMm = xSpacingMm
            });
            var yDistribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = widthM,
                CoverM = coverM,
                DiameterMm = yDiameterMm,
                SpacingMm = ySpacingMm
            });
            RequireCenterSpacing(xDistribution, xDiameterMm / 1000d, "X");
            RequireCenterSpacing(yDistribution, yDiameterMm / 1000d, "Y");

            var xHalfLengthM = widthM / 2d - coverM - xRadiusM;
            var yHalfLengthM = depthM / 2d - coverM - yRadiusM;
            Finite(xHalfLengthM, "mat X half length");
            Finite(yHalfLengthM, "mat Y half length");
            if (!(xHalfLengthM > 0d) || !(yHalfLengthM > 0d)) throw new InvalidOperationException("Cover + bar radius leaves no usable mat bar length inside the host footprint.");

            // Deterministic stacking: X bars are closest to the host face; Y bars sit immediately inward.
            // This avoids X/Y cylinders occupying the same Z while keeping both layers tangent by radius.
            var bottomX = coverM + xRadiusM;
            var bottomY = bottomX + crossCenterGapM;
            var topX = thicknessM - coverM - xRadiusM;
            var topY = topX - crossCenterGapM;
            Finite(bottomX, "bottom X elevation");
            Finite(bottomY, "bottom Y elevation");
            Finite(topX, "top X elevation");
            Finite(topY, "top Y elevation");

            if (input.BottomEnabled && bottomY + yRadiusM > thicknessM - coverM + 1e-12d)
                throw new InvalidOperationException("Host thickness is too small for the bottom orthogonal rebar mat stack.");
            if (input.TopEnabled && topY - yRadiusM < coverM - 1e-12d)
                throw new InvalidOperationException("Host thickness is too small for the top orthogonal rebar mat stack.");
            if (input.BottomEnabled && input.TopEnabled && bottomY + yRadiusM > topY - yRadiusM + 1e-12d)
                throw new InvalidOperationException("Top and bottom orthogonal rebar mats overlap inside the host thickness.");

            var faces = (input.BottomEnabled ? 1 : 0) + (input.TopEnabled ? 1 : 0);
            var requested = checked((long)(xDistribution.Count + yDistribution.Count) * faces);
            if (requested > MaxBars) throw new InvalidOperationException("Orthogonal rebar mat exceeds the maximum of " + MaxBars + " bars.");
            var bars = new List<OrthogonalRebarMatBar>((int)requested);
            if (input.BottomEnabled) AddFace(bars, OrthogonalRebarMatFace.Bottom, bottomX, bottomY, xDiameterMm, yDiameterMm, xHalfLengthM, yHalfLengthM, xDistribution, yDistribution);
            if (input.TopEnabled) AddFace(bars, OrthogonalRebarMatFace.Top, topX, topY, xDiameterMm, yDiameterMm, xHalfLengthM, yHalfLengthM, xDistribution, yDistribution);
            return new OrthogonalRebarMatLayout(bars.AsReadOnly(), xDistribution.ActualSpacingM, yDistribution.ActualSpacingM);
        }

        private static void AddFace(List<OrthogonalRebarMatBar> bars, OrthogonalRebarMatFace face, double xElevationM, double yElevationM, double xDiameterMm, double yDiameterMm, double xHalfLengthM, double yHalfLengthM, LinearRebarLayout xDistribution, LinearRebarLayout yDistribution)
        {
            foreach (var station in xDistribution.OffsetsM)
                bars.Add(new OrthogonalRebarMatBar(face, OrthogonalRebarDirection.X, new Point2(-xHalfLengthM, station), new Point2(xHalfLengthM, station), xElevationM, xDiameterMm));
            foreach (var station in yDistribution.OffsetsM)
                bars.Add(new OrthogonalRebarMatBar(face, OrthogonalRebarDirection.Y, new Point2(station, -yHalfLengthM), new Point2(station, yHalfLengthM), yElevationM, yDiameterMm));
        }

        private static void RequireCenterSpacing(LinearRebarLayout layout, double diameterM, string direction)
        {
            if (layout.Count >= 2 && layout.ActualSpacingM + 1e-12d < diameterM)
                throw new InvalidOperationException("Orthogonal mat " + direction + " bar centers are closer than one bar diameter.");
        }

        private static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(name + " is not finite.");
            return value;
        }
    }
}
