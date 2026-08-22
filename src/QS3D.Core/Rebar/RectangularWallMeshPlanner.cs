using System;
using System.Collections.Generic;

namespace QS3D.Core.Rebar
{
    public enum WallMeshFace { Near, Far }
    public enum WallMeshDirection { Horizontal, Vertical }

    public sealed class RectangularWallMeshInput
    {
        public double LengthM { get; set; }
        public double HeightM { get; set; }
        public double ThicknessM { get; set; }
        public double CoverM { get; set; }
        public double HorizontalDiameterMm { get; set; }
        public double VerticalDiameterMm { get; set; }
        public double? HorizontalSpacingMm { get; set; }
        public int? HorizontalCount { get; set; }
        public double? VerticalSpacingMm { get; set; }
        public int? VerticalCount { get; set; }
        public bool IncludeNear { get; set; } = true;
        public bool IncludeFar { get; set; } = true;
        public bool HorizontalClosestToFace { get; set; } = true;
    }

    public sealed class WallMeshBarPlacement
    {
        public WallMeshFace Face { get; set; }
        public WallMeshDirection Direction { get; set; }
        public double DistributionOffsetM { get; set; }
        public double FaceOffsetM { get; set; }
        public double LengthM { get; set; }
        public double DiameterMm { get; set; }
    }

    public sealed class RectangularWallMeshLayout
    {
        public RectangularWallMeshLayout(IReadOnlyList<WallMeshBarPlacement> bars, double horizontalActualSpacingM, double verticalActualSpacingM)
        {
            Bars = bars ?? throw new ArgumentNullException(nameof(bars));
            HorizontalActualSpacingM = horizontalActualSpacingM;
            VerticalActualSpacingM = verticalActualSpacingM;
        }

        public IReadOnlyList<WallMeshBarPlacement> Bars { get; }
        public double HorizontalActualSpacingM { get; }
        public double VerticalActualSpacingM { get; }
        public int Count => Bars.Count;
    }

    public static class RectangularWallMeshPlanner
    {
        private const int MaxBars = 8192;

        public static RectangularWallMeshLayout Plan(RectangularWallMeshInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var length = RebarMath.Positive(input.LengthM, nameof(input.LengthM));
            var height = RebarMath.Positive(input.HeightM, nameof(input.HeightM));
            var thickness = RebarMath.Positive(input.ThicknessM, nameof(input.ThicknessM));
            var cover = RebarMath.NonNegative(input.CoverM, nameof(input.CoverM));
            var horizontalDiameter = RebarMath.Positive(input.HorizontalDiameterMm, nameof(input.HorizontalDiameterMm));
            var verticalDiameter = RebarMath.Positive(input.VerticalDiameterMm, nameof(input.VerticalDiameterMm));
            if (!input.IncludeNear && !input.IncludeFar) throw new InvalidOperationException("At least one structural-wall mesh face must be enabled.");

            var horizontalRadius = RebarMath.Divide(horizontalDiameter, 2000d, "wall horizontal radius");
            var verticalRadius = RebarMath.Divide(verticalDiameter, 2000d, "wall vertical radius");
            var horizontalEndCover = RebarMath.Add(cover, horizontalRadius, "wall horizontal end center cover");
            var verticalEndCover = RebarMath.Add(cover, verticalRadius, "wall vertical end center cover");
            var horizontalEndDeduction = 2d * horizontalEndCover;
            var verticalEndDeduction = 2d * verticalEndCover;
            var horizontalLength = length - horizontalEndDeduction;
            var verticalLength = height - verticalEndDeduction;
            if (horizontalEndDeduction > 0d && horizontalLength == length)
                throw new OverflowException("Structural wall horizontal bar length lost positive end clearance at the current numeric scale.");
            if (verticalEndDeduction > 0d && verticalLength == height)
                throw new OverflowException("Structural wall vertical bar length lost positive end clearance at the current numeric scale.");
            if (!FinitePositive(horizontalLength)) throw new InvalidOperationException("Structural wall length is too short for horizontal-bar cover + radius.");
            if (!FinitePositive(verticalLength)) throw new InvalidOperationException("Structural wall height is too short for vertical-bar cover + radius.");

            var horizontalDistribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = height,
                CoverM = cover,
                DiameterMm = horizontalDiameter,
                SpacingMm = input.HorizontalSpacingMm,
                Count = input.HorizontalCount
            });
            var verticalDistribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = length,
                CoverM = cover,
                DiameterMm = verticalDiameter,
                SpacingMm = input.VerticalSpacingMm,
                Count = input.VerticalCount
            });

            var faceCount = (input.IncludeNear ? 1L : 0L) + (input.IncludeFar ? 1L : 0L);
            var projectedBars = faceCount * ((long)horizontalDistribution.OffsetsM.Count + verticalDistribution.OffsetsM.Count);
            if (projectedBars > MaxBars) throw new InvalidOperationException("Structural wall mesh exceeds the supported " + MaxBars + " bar limit.");

            var half = RebarMath.Divide(thickness, 2d, "wall half thickness");
            double nearHorizontal;
            double nearVertical;
            double farHorizontal;
            double farVertical;
            if (input.HorizontalClosestToFace)
            {
                nearHorizontal = -half + cover + horizontalRadius;
                nearVertical = nearHorizontal + horizontalRadius + verticalRadius;
                farHorizontal = half - cover - horizontalRadius;
                farVertical = farHorizontal - horizontalRadius - verticalRadius;
            }
            else
            {
                nearVertical = -half + cover + verticalRadius;
                nearHorizontal = nearVertical + horizontalRadius + verticalRadius;
                farVertical = half - cover - verticalRadius;
                farHorizontal = farVertical - horizontalRadius - verticalRadius;
            }
            ValidateFinite(nearHorizontal, "near horizontal face offset");
            ValidateFinite(nearVertical, "near vertical face offset");
            ValidateFinite(farHorizontal, "far horizontal face offset");
            ValidateFinite(farVertical, "far vertical face offset");

            var usableLow = -half + cover;
            var usableHigh = half - cover;
            if (input.IncludeNear && input.IncludeFar)
            {
                var nearInner = Math.Max(nearHorizontal + horizontalRadius, nearVertical + verticalRadius);
                var farInner = Math.Min(farHorizontal - horizontalRadius, farVertical - verticalRadius);
                if (!(farInner > nearInner)) throw new InvalidOperationException("Structural wall thickness is insufficient for the requested two-face mesh and cover.");
            }
            else if (input.IncludeNear)
            {
                var low = Math.Min(nearHorizontal - horizontalRadius, nearVertical - verticalRadius);
                var high = Math.Max(nearHorizontal + horizontalRadius, nearVertical + verticalRadius);
                if (low < usableLow - 1e-12d || high > usableHigh + 1e-12d)
                    throw new InvalidOperationException("Near structural-wall mesh does not fit within the concrete cover envelope.");
            }
            else if (input.IncludeFar)
            {
                var low = Math.Min(farHorizontal - horizontalRadius, farVertical - verticalRadius);
                var high = Math.Max(farHorizontal + horizontalRadius, farVertical + verticalRadius);
                if (low < usableLow - 1e-12d || high > usableHigh + 1e-12d)
                    throw new InvalidOperationException("Far structural-wall mesh does not fit within the concrete cover envelope.");
            }

            var bars = new List<WallMeshBarPlacement>((int)projectedBars);
            if (input.IncludeNear)
                AppendFace(bars, WallMeshFace.Near, horizontalDistribution.OffsetsM, verticalDistribution.OffsetsM, nearHorizontal, nearVertical, horizontalLength, verticalLength, horizontalDiameter, verticalDiameter);
            if (input.IncludeFar)
                AppendFace(bars, WallMeshFace.Far, horizontalDistribution.OffsetsM, verticalDistribution.OffsetsM, farHorizontal, farVertical, horizontalLength, verticalLength, horizontalDiameter, verticalDiameter);
            return new RectangularWallMeshLayout(bars.AsReadOnly(), horizontalDistribution.ActualSpacingM, verticalDistribution.ActualSpacingM);
        }

        private static void AppendFace(
            ICollection<WallMeshBarPlacement> bars,
            WallMeshFace face,
            IReadOnlyList<double> horizontalOffsets,
            IReadOnlyList<double> verticalOffsets,
            double horizontalFaceOffset,
            double verticalFaceOffset,
            double horizontalLength,
            double verticalLength,
            double horizontalDiameter,
            double verticalDiameter)
        {
            foreach (var offset in horizontalOffsets)
            {
                ValidateFinite(offset, "wall horizontal distribution offset");
                bars.Add(new WallMeshBarPlacement
                {
                    Face = face,
                    Direction = WallMeshDirection.Horizontal,
                    DistributionOffsetM = offset,
                    FaceOffsetM = horizontalFaceOffset,
                    LengthM = horizontalLength,
                    DiameterMm = horizontalDiameter
                });
            }
            foreach (var offset in verticalOffsets)
            {
                ValidateFinite(offset, "wall vertical distribution offset");
                bars.Add(new WallMeshBarPlacement
                {
                    Face = face,
                    Direction = WallMeshDirection.Vertical,
                    DistributionOffsetM = offset,
                    FaceOffsetM = verticalFaceOffset,
                    LengthM = verticalLength,
                    DiameterMm = verticalDiameter
                });
            }
        }

        private static bool FinitePositive(double value) => value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);

        private static void ValidateFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " is not finite.");
        }
    }
}
