using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WallQuantityArithmeticUnderflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsOpeningAreaUnderflow();
            RejectsGrossAreaUnderflow();
            RejectsGrossVolumeUnderflow();
            PreservesRepresentableSubnormalOpeningArea();
            PreservesLegitimateZeroDimensions();
            PreservesOrdinaryWallQuantities();
        }

        private static void RejectsOpeningAreaUnderflow()
        {
            var opening = new OpeningCut { WidthM = 1e-200d, HeightM = 1e-200d };
            Throws<OverflowException>(() => _ = opening.AreaM2);
        }

        private static void RejectsGrossAreaUnderflow()
        {
            Throws<OverflowException>(() => WallQuantityCalculator.Calculate(1e-200d, 1e-200d, 1d));
        }

        private static void RejectsGrossVolumeUnderflow()
        {
            Throws<OverflowException>(() => WallQuantityCalculator.Calculate(1e-160d, 1e-160d, 1e-20d));
        }

        private static void PreservesRepresentableSubnormalOpeningArea()
        {
            var opening = new OpeningCut { WidthM = 1e-160d, HeightM = 1e-160d };
            var area = opening.AreaM2;
            if (area == 0d || double.IsNaN(area) || double.IsInfinity(area))
                throw new InvalidOperationException("WallQuantityArithmeticUnderflowSmoke expected a representable subnormal opening area to remain non-zero and finite.");
        }

        private static void PreservesLegitimateZeroDimensions()
        {
            var result = WallQuantityCalculator.Calculate(0d, 3d, 0.2d);
            if (result.GrossAreaM2 != 0d || result.GrossVolumeM3 != 0d || result.NetVolumeM3 != 0d)
                throw new InvalidOperationException("WallQuantityArithmeticUnderflowSmoke changed legitimate zero-dimension wall quantities.");

            var opening = new OpeningCut { WidthM = 0d, HeightM = 3d };
            if (opening.AreaM2 != 0d)
                throw new InvalidOperationException("WallQuantityArithmeticUnderflowSmoke changed legitimate zero opening area.");
        }

        private static void PreservesOrdinaryWallQuantities()
        {
            var result = WallQuantityCalculator.Calculate(10d, 3d, 0.2d, new[]
            {
                new OpeningCut { WidthM = 1d, HeightM = 2d }
            });
            Near(30d, result.GrossAreaM2, "ordinary gross area");
            Near(2d, result.OpeningAreaM2, "ordinary opening area");
            Near(28d, result.NetAreaM2, "ordinary net area");
            Near(6d, result.GrossVolumeM3, "ordinary gross volume");
            Near(0.4d, result.DeductionVolumeM3, "ordinary deduction volume");
            Near(5.6d, result.NetVolumeM3, "ordinary net volume");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new InvalidOperationException("WallQuantityArithmeticUnderflowSmoke changed " + label + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("WallQuantityArithmeticUnderflowSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
