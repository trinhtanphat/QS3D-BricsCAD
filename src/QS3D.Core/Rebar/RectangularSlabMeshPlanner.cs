using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public enum SlabMeshFace { Bottom, Top }
    public enum SlabMeshDirection { X, Y }

    public sealed class RectangularSlabMeshInput
    {
        public double SpanXM { get; set; }
        public double SpanYM { get; set; }
        public double ThicknessM { get; set; }
        public double CoverM { get; set; }
        public double XDiameterMm { get; set; }
        public double YDiameterMm { get; set; }
        public double? XSpacingMm { get; set; }
        public int? XCount { get; set; }
        public double? YSpacingMm { get; set; }
        public int? YCount { get; set; }
        public bool IncludeBottom { get; set; } = true;
        public bool IncludeTop { get; set; }
        public bool XClosestToFace { get; set; } = true;
    }

    public sealed class SlabMeshBarPlacement
    {
        public SlabMeshFace Face { get; set; }
        public SlabMeshDirection Direction { get; set; }
        public double DistributionOffsetM { get; set; }
        public double ElevationOffsetM { get; set; }
        public double LengthM { get; set; }
        public double DiameterMm { get; set; }
    }

    public sealed class RectangularSlabMeshLayout
    {
        public RectangularSlabMeshLayout(IReadOnlyList<SlabMeshBarPlacement> bars, double xActualSpacingM, double yActualSpacingM)
        {
            Bars = bars ?? throw new ArgumentNullException(nameof(bars));
            XActualSpacingM = xActualSpacingM;
            YActualSpacingM = yActualSpacingM;
        }

        public IReadOnlyList<SlabMeshBarPlacement> Bars { get; }
        public double XActualSpacingM { get; }
        public double YActualSpacingM { get; }
        public int Count => Bars.Count;
    }

    public static class RectangularSlabMeshPlanner
    {
        private const int MaxBars = 8192;

        public static RectangularSlabMeshLayout Plan(RectangularSlabMeshInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var spanX = RebarMath.Positive(input.SpanXM, nameof(input.SpanXM));
            var spanY = RebarMath.Positive(input.SpanYM, nameof(input.SpanYM));
            var thickness = RebarMath.Positive(input.ThicknessM, nameof(input.ThicknessM));
            var cover = RebarMath.NonNegative(input.CoverM, nameof(input.CoverM));
            var xDiameter = RebarMath.Positive(input.XDiameterMm, nameof(input.XDiameterMm));
            var yDiameter = RebarMath.Positive(input.YDiameterMm, nameof(input.YDiameterMm));
            if (!input.IncludeBottom && !input.IncludeTop) throw new InvalidOperationException("At least one slab mesh face must be enabled.");

            var xRadius = RebarMath.Divide(xDiameter, 2000d, "slab X radius");
            var yRadius = RebarMath.Divide(yDiameter, 2000d, "slab Y radius");
            var xEndCover = RebarMath.Add(cover, xRadius, "slab X end center cover");
            var yEndCover = RebarMath.Add(cover, yRadius, "slab Y end center cover");
            var xEndDeduction = 2d * xEndCover;
            var yEndDeduction = 2d * yEndCover;
            var xLength = spanX - xEndDeduction;
            var yLength = spanY - yEndDeduction;
            if (xEndDeduction > 0d && xLength == spanX)
                throw new OverflowException("Slab X bar length lost positive end clearance at the current numeric scale.");
            if (yEndDeduction > 0d && yLength == spanY)
                throw new OverflowException("Slab Y bar length lost positive end clearance at the current numeric scale.");
            if (!FinitePositive(xLength)) throw new InvalidOperationException("Slab X span is too short for cover + bar radius.");
            if (!FinitePositive(yLength)) throw new InvalidOperationException("Slab Y span is too short for cover + bar radius.");

            // X bars run along X and are distributed across Y; Y bars are the opposite.
            var xDistribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = spanY,
                CoverM = cover,
                DiameterMm = xDiameter,
                SpacingMm = input.XSpacingMm,
                Count = input.XCount
            });
            var yDistribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = spanX,
                CoverM = cover,
                DiameterMm = yDiameter,
                SpacingMm = input.YSpacingMm,
                Count = input.YCount
            });

            var faceCount = (input.IncludeBottom ? 1L : 0L) + (input.IncludeTop ? 1L : 0L);
            var projectedBars = faceCount * ((long)xDistribution.OffsetsM.Count + yDistribution.OffsetsM.Count);
            if (projectedBars > MaxBars) throw new InvalidOperationException("Slab mesh exceeds the supported " + MaxBars + " bar limit.");

            var half = RebarMath.Divide(thickness, 2d, "slab half thickness");
            double bottomX;
            double bottomY;
            double topX;
            double topY;
            if (input.XClosestToFace)
            {
                bottomX = -half + cover + xRadius;
                bottomY = bottomX + xRadius + yRadius;
                topX = half - cover - xRadius;
                topY = topX - xRadius - yRadius;
            }
            else
            {
                bottomY = -half + cover + yRadius;
                bottomX = bottomY + xRadius + yRadius;
                topY = half - cover - yRadius;
                topX = topY - xRadius - yRadius;
            }
            ValidateFinite(bottomX, "bottom X elevation");
            ValidateFinite(bottomY, "bottom Y elevation");
            ValidateFinite(topX, "top X elevation");
            ValidateFinite(topY, "top Y elevation");

            var usableLow = -half + cover;
            var usableHigh = half - cover;
            if (input.IncludeBottom && input.IncludeTop)
            {
                var bottomHigh = Math.Max(bottomX + xRadius, bottomY + yRadius);
                var topLow = Math.Min(topX - xRadius, topY - yRadius);
                if (!(topLow > bottomHigh)) throw new InvalidOperationException("Slab thickness is insufficient for the requested top + bottom two-direction mesh and cover.");
            }
            else if (input.IncludeBottom)
            {
                var low = Math.Min(bottomX - xRadius, bottomY - yRadius);
                var high = Math.Max(bottomX + xRadius, bottomY + yRadius);
                if (low < usableLow - 1e-12d || high > usableHigh + 1e-12d)
                    throw new InvalidOperationException("Bottom slab mesh does not fit within the concrete cover envelope.");
            }
            else if (input.IncludeTop)
            {
                var low = Math.Min(topX - xRadius, topY - yRadius);
                var high = Math.Max(topX + xRadius, topY + yRadius);
                if (low < usableLow - 1e-12d || high > usableHigh + 1e-12d)
                    throw new InvalidOperationException("Top slab mesh does not fit within the concrete cover envelope.");
            }

            var bars = new List<SlabMeshBarPlacement>((int)projectedBars);
            if (input.IncludeBottom) AppendFace(bars, SlabMeshFace.Bottom, xDistribution.OffsetsM, yDistribution.OffsetsM, bottomX, bottomY, xLength, yLength, xDiameter, yDiameter);
            if (input.IncludeTop) AppendFace(bars, SlabMeshFace.Top, xDistribution.OffsetsM, yDistribution.OffsetsM, topX, topY, xLength, yLength, xDiameter, yDiameter);
            return new RectangularSlabMeshLayout(bars.AsReadOnly(), xDistribution.ActualSpacingM, yDistribution.ActualSpacingM);
        }

        private static void AppendFace(
            ICollection<SlabMeshBarPlacement> bars,
            SlabMeshFace face,
            IReadOnlyList<double> xOffsets,
            IReadOnlyList<double> yOffsets,
            double xElevation,
            double yElevation,
            double xLength,
            double yLength,
            double xDiameter,
            double yDiameter)
        {
            foreach (var offset in xOffsets)
            {
                ValidateFinite(offset, "slab X distribution offset");
                bars.Add(new SlabMeshBarPlacement { Face = face, Direction = SlabMeshDirection.X, DistributionOffsetM = offset, ElevationOffsetM = xElevation, LengthM = xLength, DiameterMm = xDiameter });
            }
            foreach (var offset in yOffsets)
            {
                ValidateFinite(offset, "slab Y distribution offset");
                bars.Add(new SlabMeshBarPlacement { Face = face, Direction = SlabMeshDirection.Y, DistributionOffsetM = offset, ElevationOffsetM = yElevation, LengthM = yLength, DiameterMm = yDiameter });
            }
        }

        private static bool FinitePositive(double value) => value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        private static void ValidateFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " is not finite.");
        }
    }
}
