using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class UnitScaleUnderflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNonZeroLinearUnderflow();
            RejectsNonZeroFromMetersUnderflow();
            PreservesExactZero();
            PreservesRepresentableSubnormalResult();
            PreservesOrdinaryConversion();
        }

        private static void RejectsNonZeroLinearUnderflow()
        {
            Throws<OverflowException>(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Angstrom));
        }

        private static void RejectsNonZeroFromMetersUnderflow()
        {
            Throws<OverflowException>(() => UnitScale.FromMeters(double.Epsilon, DrawingUnit.Parsec));
        }

        private static void PreservesExactZero()
        {
            if (UnitScale.ToMeters(0d, DrawingUnit.Angstrom) != 0d)
                throw new InvalidOperationException("UnitScaleUnderflowSmoke expected exact zero input to remain zero.");
        }

        private static void PreservesRepresentableSubnormalResult()
        {
            var result = UnitScale.ToMeters(1e-313d, DrawingUnit.Angstrom);
            if (result == 0d || double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidOperationException("UnitScaleUnderflowSmoke expected a representable subnormal result to remain non-zero and finite.");
        }

        private static void PreservesOrdinaryConversion()
        {
            var result = UnitScale.ToMeters(1000d, DrawingUnit.Millimeter);
            if (Math.Abs(result - 1d) > 1e-12d)
                throw new InvalidOperationException("UnitScaleUnderflowSmoke expected 1000 mm to remain 1 m.");
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
            throw new InvalidOperationException("UnitScaleUnderflowSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
