using System;

namespace QS3D.Core.Services
{
    public sealed class StructuralQuantityResult
    {
        public StructuralQuantityResult(double grossVolumeM3, double deductionM3, double formworkM2, double footprintAreaM2 = 0d, double looseVolumeM3 = 0d)
        {
            GrossVolumeM3 = grossVolumeM3;
            DeductionM3 = deductionM3;
            NetVolumeM3 = Math.Max(0d, grossVolumeM3 - deductionM3);
            FormworkM2 = formworkM2;
            FootprintAreaM2 = footprintAreaM2;
            LooseVolumeM3 = looseVolumeM3;
        }

        public double GrossVolumeM3 { get; }
        public double DeductionM3 { get; }
        public double NetVolumeM3 { get; }
        public double FormworkM2 { get; }
        public double FootprintAreaM2 { get; }
        public double LooseVolumeM3 { get; }
    }

    public static class StructuralQuantityCalculator
    {
        public static StructuralQuantityResult Beam(double lengthM, double widthM, double heightM, double deductionM3 = 0d)
        {
            PositiveOrZero(lengthM, nameof(lengthM)); PositiveOrZero(widthM, nameof(widthM)); PositiveOrZero(heightM, nameof(heightM));
            var gross = lengthM * widthM * heightM;
            var formwork = lengthM * (widthM + 2d * heightM) + 2d * widthM * heightM;
            return Result(gross, deductionM3, formwork, lengthM * widthM);
        }

        public static StructuralQuantityResult Column(double widthM, double depthM, double heightM, double deductionM3 = 0d)
        {
            PositiveOrZero(widthM, nameof(widthM)); PositiveOrZero(depthM, nameof(depthM)); PositiveOrZero(heightM, nameof(heightM));
            var gross = widthM * depthM * heightM;
            var formwork = 2d * (widthM + depthM) * heightM;
            return Result(gross, deductionM3, formwork, widthM * depthM);
        }

        public static StructuralQuantityResult Slab(double areaM2, double perimeterM, double thicknessM, double deductionM3 = 0d)
        {
            PositiveOrZero(areaM2, nameof(areaM2)); PositiveOrZero(perimeterM, nameof(perimeterM)); PositiveOrZero(thicknessM, nameof(thicknessM));
            var gross = areaM2 * thicknessM;
            var formwork = areaM2 + perimeterM * thicknessM;
            return Result(gross, deductionM3, formwork, areaM2);
        }

        public static StructuralQuantityResult StructuralWall(double lengthM, double heightM, double thicknessM, double deductionM3 = 0d)
        {
            PositiveOrZero(lengthM, nameof(lengthM)); PositiveOrZero(heightM, nameof(heightM)); PositiveOrZero(thicknessM, nameof(thicknessM));
            var gross = lengthM * heightM * thicknessM;
            var formwork = 2d * lengthM * heightM + 2d * thicknessM * heightM;
            return Result(gross, deductionM3, formwork, lengthM * thicknessM);
        }

        public static StructuralQuantityResult Foundation(double lengthM, double widthM, double heightM, double deductionM3 = 0d)
        {
            PositiveOrZero(lengthM, nameof(lengthM)); PositiveOrZero(widthM, nameof(widthM)); PositiveOrZero(heightM, nameof(heightM));
            var gross = lengthM * widthM * heightM;
            var formwork = 2d * (lengthM + widthM) * heightM;
            return Result(gross, deductionM3, formwork, lengthM * widthM);
        }

        public static StructuralQuantityResult FootprintPrism(double areaM2, double perimeterM, double heightM, double deductionM3 = 0d)
        {
            PositiveOrZero(areaM2, nameof(areaM2)); PositiveOrZero(perimeterM, nameof(perimeterM)); PositiveOrZero(heightM, nameof(heightM));
            return Result(areaM2 * heightM, deductionM3, perimeterM * heightM, areaM2);
        }

        public static StructuralQuantityResult Earthwork(double footprintAreaM2, double depthM, double swellFactor = 0d)
        {
            PositiveOrZero(footprintAreaM2, nameof(footprintAreaM2)); PositiveOrZero(depthM, nameof(depthM)); PositiveOrZero(swellFactor, nameof(swellFactor));
            var inSitu = footprintAreaM2 * depthM;
            return new StructuralQuantityResult(inSitu, 0d, 0d, footprintAreaM2, inSitu * (1d + swellFactor));
        }

        private static StructuralQuantityResult Result(double gross, double deduction, double formwork, double footprint)
        {
            PositiveOrZero(deduction, nameof(deduction));
            return new StructuralQuantityResult(gross, Math.Min(gross, deduction), formwork, footprint);
        }

        private static void PositiveOrZero(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(name, "Value must be finite and non-negative.");
        }
    }
}
