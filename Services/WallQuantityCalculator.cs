using System;
using System.Collections.Generic;

namespace QS3D.Core.Services
{
    public sealed class OpeningCut
    {
        public double WidthM { get; set; }
        public double HeightM { get; set; }
        public double AreaM2
        {
            get
            {
                RequireFinite(WidthM, nameof(WidthM));
                RequireFinite(HeightM, nameof(HeightM));
                var area = Math.Max(0d, WidthM) * Math.Max(0d, HeightM);
                if (double.IsNaN(area) || double.IsInfinity(area)) throw new OverflowException("Opening area is not finite.");
                return area;
            }
        }

        private static void RequireFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name, "Opening dimensions must be finite.");
        }
    }

    public sealed class WallQuantities
    {
        public double GrossAreaM2 { get; set; }
        public double OpeningAreaM2 { get; set; }
        public double NetAreaM2 { get; set; }
        public double GrossVolumeM3 { get; set; }
        public double DeductionVolumeM3 { get; set; }
        public double NetVolumeM3 { get; set; }
        public double TwoSideFinishAreaM2 { get; set; }
    }

    public static class WallQuantityCalculator
    {
        public static WallQuantities Calculate(double lengthM, double heightM, double thicknessM, IEnumerable<OpeningCut>? openings = null)
        {
            RequireFiniteNonNegative(lengthM, nameof(lengthM));
            RequireFiniteNonNegative(heightM, nameof(heightM));
            RequireFiniteNonNegative(thicknessM, nameof(thicknessM));

            var grossArea = FiniteProduct(lengthM, heightM, "gross wall area");
            var openingArea = 0d;
            if (openings != null)
            {
                foreach (var opening in openings)
                {
                    if (opening == null) continue;
                    openingArea += opening.AreaM2;
                    if (double.IsNaN(openingArea) || double.IsInfinity(openingArea)) throw new OverflowException("Total opening area is not finite.");
                }
            }

            var clampedOpeningArea = Math.Min(grossArea, openingArea);
            var netArea = grossArea - clampedOpeningArea;
            var grossVolume = FiniteProduct(grossArea, thicknessM, "gross wall volume");
            var deductionVolume = FiniteProduct(clampedOpeningArea, thicknessM, "wall deduction volume");
            var twoSideFinishArea = FiniteProduct(netArea, 2d, "two-side finish area");

            return new WallQuantities
            {
                GrossAreaM2 = grossArea,
                OpeningAreaM2 = clampedOpeningArea,
                NetAreaM2 = netArea,
                GrossVolumeM3 = grossVolume,
                DeductionVolumeM3 = deductionVolume,
                NetVolumeM3 = grossVolume - deductionVolume,
                TwoSideFinishAreaM2 = twoSideFinishArea
            };
        }

        private static void RequireFiniteNonNegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, "Wall dimensions must be finite and non-negative.");
        }

        private static double FiniteProduct(double left, double right, string label)
        {
            var result = left * right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " is not finite.");
            return result;
        }
    }
}
