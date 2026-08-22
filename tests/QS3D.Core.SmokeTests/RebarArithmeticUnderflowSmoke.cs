using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarArithmeticUnderflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPositiveMultiplicationUnderflow();
            RejectsPositiveDivisionUnderflow();
            PreservesRepresentableSubnormalUnitWeight();
            PreservesLegitimateZeroLengthWeight();
            PreservesOrdinaryUnitWeight();
        }

        private static void RejectsPositiveMultiplicationUnderflow()
        {
            Throws<OverflowException>(() => RebarWeight.KilogramsPerMeter(1e-200d));
        }

        private static void RejectsPositiveDivisionUnderflow()
        {
            Throws<OverflowException>(() => RebarWeight.KilogramsPerMeter(1e-161d));
        }

        private static void PreservesRepresentableSubnormalUnitWeight()
        {
            var value = RebarWeight.KilogramsPerMeter(1e-155d);
            if (value == 0d || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("RebarArithmeticUnderflowSmoke expected a representable subnormal unit weight to remain non-zero and finite.");
        }

        private static void PreservesLegitimateZeroLengthWeight()
        {
            if (RebarWeight.TotalKilograms(12d, 0d) != 0d)
                throw new InvalidOperationException("RebarArithmeticUnderflowSmoke expected zero total length to preserve zero weight.");
        }

        private static void PreservesOrdinaryUnitWeight()
        {
            var value = RebarWeight.KilogramsPerMeter(12d);
            if (Math.Abs(value - (144d / 162d)) > 1e-12d)
                throw new InvalidOperationException("RebarArithmeticUnderflowSmoke changed the ordinary D12 unit-weight formula.");
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
            throw new InvalidOperationException("RebarArithmeticUnderflowSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
