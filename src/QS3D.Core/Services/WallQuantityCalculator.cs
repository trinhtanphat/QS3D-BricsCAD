using System;
using System.Collections.Generic;

namespace QS3D.Core.Services
{
    public sealed class OpeningCut
    {
        public double WidthM { get; set; }
        public double HeightM { get; set; }
        public double AreaM2 => Math.Max(0d, WidthM) * Math.Max(0d, HeightM);
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
            if (lengthM < 0d) throw new ArgumentOutOfRangeException(nameof(lengthM));
            if (heightM < 0d) throw new ArgumentOutOfRangeException(nameof(heightM));
            if (thicknessM < 0d) throw new ArgumentOutOfRangeException(nameof(thicknessM));

            var grossArea = lengthM * heightM;
            var openingArea = 0d;
            if (openings != null)
            {
                foreach (var opening in openings)
                {
                    if (opening == null) continue;
                    openingArea += opening.AreaM2;
                }
            }

            // Never allow deductions to create negative wall quantities.
            var clampedOpeningArea = Math.Min(grossArea, openingArea);
            var netArea = grossArea - clampedOpeningArea;
            var grossVolume = grossArea * thicknessM;
            var deductionVolume = clampedOpeningArea * thicknessM;

            return new WallQuantities
            {
                GrossAreaM2 = grossArea,
                OpeningAreaM2 = clampedOpeningArea,
                NetAreaM2 = netArea,
                GrossVolumeM3 = grossVolume,
                DeductionVolumeM3 = deductionVolume,
                NetVolumeM3 = grossVolume - deductionVolume,
                TwoSideFinishAreaM2 = netArea * 2d
            };
        }
    }
}
