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
    }

    public sealed class BeamStirrupLayout
    {
        public BeamStirrupLayout(IReadOnlyList<double> stationOffsetsM, IReadOnlyList<Point2> sectionLoop, double actualSpacingM)
        {
            StationOffsetsM = stationOffsetsM ?? throw new ArgumentNullException(nameof(stationOffsetsM));
            SectionLoop = sectionLoop ?? throw new ArgumentNullException(nameof(sectionLoop));
            if (SectionLoop.Count != 5) throw new ArgumentException("Beam stirrup section loop must contain four corners plus the closing point.", nameof(sectionLoop));
            ActualSpacingM = actualSpacingM;
        }

        public IReadOnlyList<double> StationOffsetsM { get; }
        public IReadOnlyList<Point2> SectionLoop { get; }
        public int Count => StationOffsetsM.Count;
        public double ActualSpacingM { get; }
    }

    public static class BeamStirrupLayoutPlanner
    {
        public static BeamStirrupLayout Plan(BeamStirrupLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var lengthM = RebarMath.Positive(input.LengthM, nameof(input.LengthM));
            var widthM = RebarMath.Positive(input.WidthM, nameof(input.WidthM));
            var heightM = RebarMath.Positive(input.HeightM, nameof(input.HeightM));
            var sectionCoverM = RebarMath.NonNegative(input.SectionCoverM, nameof(input.SectionCoverM));
            var endCoverM = RebarMath.NonNegative(input.EndCoverM, nameof(input.EndCoverM));
            var diameterMm = RebarMath.Positive(input.DiameterMm, nameof(input.DiameterMm));

            var stations = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = lengthM,
                CoverM = endCoverM,
                DiameterMm = diameterMm,
                SpacingMm = input.SpacingMm,
                Count = input.Count
            });

            var radiusM = RebarMath.Divide(diameterMm, 2000d, "beam stirrup radius");
            var centerCoverM = RebarMath.Add(sectionCoverM, radiusM, "beam stirrup center cover");
            var halfWidthM = RebarMath.Divide(widthM, 2d, "beam stirrup half width") - centerCoverM;
            var halfHeightM = RebarMath.Divide(heightM, 2d, "beam stirrup half height") - centerCoverM;
            if (double.IsNaN(halfWidthM) || double.IsInfinity(halfWidthM) || !(halfWidthM > 0d))
                throw new InvalidOperationException("Section cover + stirrup radius leaves no usable beam width.");
            if (double.IsNaN(halfHeightM) || double.IsInfinity(halfHeightM) || !(halfHeightM > 0d))
                throw new InvalidOperationException("Section cover + stirrup radius leaves no usable beam height.");

            var loop = new List<Point2>(5)
            {
                new Point2(-halfWidthM, -halfHeightM),
                new Point2( halfWidthM, -halfHeightM),
                new Point2( halfWidthM,  halfHeightM),
                new Point2(-halfWidthM,  halfHeightM),
                new Point2(-halfWidthM, -halfHeightM)
            };
            return new BeamStirrupLayout(stations.OffsetsM, loop.AsReadOnly(), stations.ActualSpacingM);
        }
    }
}
