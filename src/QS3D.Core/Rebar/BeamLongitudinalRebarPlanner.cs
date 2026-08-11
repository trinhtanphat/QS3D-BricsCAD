using System;
using System.Collections.Generic;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public sealed class BeamLongitudinalRebarLayoutInput
    {
        public double WidthM { get; set; }
        public double HeightM { get; set; }
        public double CoverM { get; set; }
        public double DiameterMm { get; set; }
        public int TopCount { get; set; }
        public int BottomCount { get; set; }
    }

    public sealed class BeamLongitudinalRebarLayout
    {
        public BeamLongitudinalRebarLayout(IReadOnlyList<Point2> topBarCenters, IReadOnlyList<Point2> bottomBarCenters, double topElevationM, double bottomElevationM)
        {
            if (topBarCenters == null) throw new ArgumentNullException(nameof(topBarCenters));
            if (bottomBarCenters == null) throw new ArgumentNullException(nameof(bottomBarCenters));
            TopBarCenters = new List<Point2>(topBarCenters).AsReadOnly();
            BottomBarCenters = new List<Point2>(bottomBarCenters).AsReadOnly();
            TopElevationM = topElevationM;
            BottomElevationM = bottomElevationM;
        }
        public IReadOnlyList<Point2> TopBarCenters { get; }
        public IReadOnlyList<Point2> BottomBarCenters { get; }
        public int Count => TopBarCenters.Count + BottomBarCenters.Count;
        public double TopElevationM { get; }
        public double BottomElevationM { get; }
    }

    public static class BeamLongitudinalRebarPlanner
    {
        private const int MaxBarsPerLayer = 512;
        public static BeamLongitudinalRebarLayout Plan(BeamLongitudinalRebarLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var widthM = RebarMath.Positive(input.WidthM, nameof(input.WidthM));
            var heightM = RebarMath.Positive(input.HeightM, nameof(input.HeightM));
            var coverM = RebarMath.NonNegative(input.CoverM, nameof(input.CoverM));
            var diameterMm = RebarMath.Positive(input.DiameterMm, nameof(input.DiameterMm));
            if (input.TopCount < 2 || input.TopCount > MaxBarsPerLayer) throw new ArgumentOutOfRangeException(nameof(input.TopCount));
            if (input.BottomCount < 2 || input.BottomCount > MaxBarsPerLayer) throw new ArgumentOutOfRangeException(nameof(input.BottomCount));

            var diameterM = RebarMath.Divide(diameterMm, 1000d, "beam rebar diameter");
            var radiusM = RebarMath.Divide(diameterM, 2d, "beam rebar radius");
            var edgeClearanceM = RebarMath.Add(coverM, radiusM, "beam rebar edge clearance");
            var halfHeightM = RebarMath.Divide(heightM, 2d, "beam half height");
            var topElevationM = halfHeightM - edgeClearanceM;
            var bottomElevationM = -halfHeightM + edgeClearanceM;
            if (double.IsNaN(topElevationM) || double.IsInfinity(topElevationM) || double.IsNaN(bottomElevationM) || double.IsInfinity(bottomElevationM)) throw new OverflowException("Beam rebar layer elevation is not finite.");
            if (!(topElevationM > bottomElevationM)) throw new InvalidOperationException("Cover + bar radius leaves no usable vertical beam reinforcement envelope.");
            if (topElevationM - bottomElevationM + 1e-12d < diameterM) throw new InvalidOperationException("Top and bottom beam rebar layers overlap.");

            var top = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput { SpanM = widthM, CoverM = coverM, DiameterMm = diameterMm, Count = input.TopCount });
            var bottom = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput { SpanM = widthM, CoverM = coverM, DiameterMm = diameterMm, Count = input.BottomCount });
            if (top.Count >= 2 && top.ActualSpacingM + 1e-12d < diameterM) throw new InvalidOperationException("Beam top rebar centers are closer than one bar diameter.");
            if (bottom.Count >= 2 && bottom.ActualSpacingM + 1e-12d < diameterM) throw new InvalidOperationException("Beam bottom rebar centers are closer than one bar diameter.");
            var topPoints = new List<Point2>(top.Count);
            foreach (var offset in top.OffsetsM) topPoints.Add(new Point2(offset, topElevationM));
            var bottomPoints = new List<Point2>(bottom.Count);
            foreach (var offset in bottom.OffsetsM) bottomPoints.Add(new Point2(offset, bottomElevationM));
            return new BeamLongitudinalRebarLayout(topPoints.AsReadOnly(), bottomPoints.AsReadOnly(), topElevationM, bottomElevationM);
        }
    }
}
