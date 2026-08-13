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
            CanonicalizesSignedZero();
            PreservesRepresentableSubnormalResult();
            PreservesOrdinaryConversion();
            PreservesOrdinaryNegativeConversion();
        }

        private static void RejectsNonZeroLinearUnderflow()
        {
            Throws<OverflowException>(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Angstrom));
        }

        private static void RejectsNonZeroFromMetersUnderflow()
        {
            Throws<OverflowException>(() => UnitScale.FromMeters(double.Epsilon, DrawingUnit.Parsec));
        }

        private static void CanonicalizesSignedZero()
        {
            var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
            PositiveZero(UnitScale.ToMeters(negativeZero, DrawingUnit.Millimeter), "ToMeters");
            PositiveZero(UnitScale.FromMeters(negativeZero, DrawingUnit.Millimeter), "FromMeters");
            PositiveZero(UnitScale.ToSquareMeters(negativeZero, DrawingUnit.Millimeter), "ToSquareMeters");
            PositiveZero(UnitScale.ToCubicMeters(negativeZero, DrawingUnit.Millimeter), "ToCubicMeters");
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

        private static void PreservesOrdinaryNegativeConversion()
        {
            var result = UnitScale.ToMeters(-1000d, DrawingUnit.Millimeter);
            if (Math.Abs(result + 1d) > 1e-12d)
                throw new InvalidOperationException("UnitScaleUnderflowSmoke expected -1000 mm to remain -1 m.");
        }

        private static void PositiveZero(double value, string operation)
        {
            if (BitConverter.DoubleToInt64Bits(value) != 0L)
                throw new InvalidOperationException("UnitScaleUnderflowSmoke expected " + operation + " to canonicalize signed zero to positive zero.");
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
