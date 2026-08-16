using System;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class UnitScaleSwallowedConversionSmoke
    {
        internal static void Run()
        {
            RejectsSwallowedToMetersConversions();
            RejectsSwallowedFromMetersConversion();
            RejectsSwallowedAreaAndVolumeConversions();
            PreservesMeterIdentity();
            PreservesZeroConversion();
            PreservesOrdinaryLinearConversions();
            PreservesOrdinaryAreaAndVolumeConversions();
            KeepsUnderflowToZeroGuard();
            KeepsNonFiniteInputGuard();
            KeepsNonFiniteResultGuard();
        }

        private static void RejectsSwallowedToMetersConversions()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Yard), "Positive swallowed yard conversion must fail closed.");
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(-double.Epsilon, DrawingUnit.Yard), "Negative swallowed yard conversion must fail closed.");
        }

        private static void RejectsSwallowedFromMetersConversion()
        {
            AssertThrows<OverflowException>(() => UnitScale.FromMeters(double.Epsilon, DrawingUnit.Yard), "Swallowed meter-to-yard conversion must fail closed.");
            AssertThrows<OverflowException>(() => UnitScale.FromMeters(-double.Epsilon, DrawingUnit.Yard), "Negative swallowed meter-to-yard conversion must fail closed.");
        }

        private static void RejectsSwallowedAreaAndVolumeConversions()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToSquareMeters(double.Epsilon, DrawingUnit.Yard), "Swallowed square-yard conversion must fail closed.");
            AssertThrows<OverflowException>(() => UnitScale.ToCubicMeters(double.Epsilon, DrawingUnit.Yard), "Swallowed cubic-yard conversion must fail closed.");
        }

        private static void PreservesMeterIdentity()
        {
            Assert(UnitScale.ToMeters(double.Epsilon, DrawingUnit.Meter) == double.Epsilon, "Meter identity must preserve positive subnormal.");
            Assert(UnitScale.FromMeters(-double.Epsilon, DrawingUnit.Meter) == -double.Epsilon, "Meter identity must preserve negative subnormal.");
        }

        private static void PreservesZeroConversion()
        {
            var positiveZero = UnitScale.ToMeters(0d, DrawingUnit.Yard);
            var negativeZero = UnitScale.ToMeters(-0d, DrawingUnit.Yard);
            Assert(BitConverter.DoubleToInt64Bits(positiveZero) == 0L, "Positive zero must remain canonical.");
            Assert(BitConverter.DoubleToInt64Bits(negativeZero) == 0L, "Negative zero must canonicalize to positive zero.");
        }

        private static void PreservesOrdinaryLinearConversions()
        {
            AssertNear(UnitScale.ToMeters(2d, DrawingUnit.Yard), 1.8288d, 1e-12d, "Ordinary yard-to-meter conversion changed.");
            AssertNear(UnitScale.ToMeters(-2d, DrawingUnit.Yard), -1.8288d, 1e-12d, "Ordinary negative yard-to-meter conversion changed.");
            AssertNear(UnitScale.FromMeters(1.8288d, DrawingUnit.Yard), 2d, 1e-12d, "Ordinary meter-to-yard conversion changed.");
        }

        private static void PreservesOrdinaryAreaAndVolumeConversions()
        {
            AssertNear(UnitScale.ToSquareMeters(2d, DrawingUnit.Yard), 2d * 0.9144d * 0.9144d, 1e-12d, "Ordinary square-yard conversion changed.");
            AssertNear(UnitScale.ToCubicMeters(2d, DrawingUnit.Yard), 2d * 0.9144d * 0.9144d * 0.9144d, 1e-12d, "Ordinary cubic-yard conversion changed.");
        }

        private static void KeepsUnderflowToZeroGuard()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Foot), "Existing underflow-to-zero behavior must remain fail closed.");
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(-double.Epsilon, DrawingUnit.Foot), "Existing negative underflow-to-zero behavior must remain fail closed.");
        }

        private static void KeepsNonFiniteInputGuard()
        {
            AssertThrows<ArgumentOutOfRangeException>(() => UnitScale.ToMeters(double.NaN, DrawingUnit.Yard), "NaN input must remain rejected.");
            AssertThrows<ArgumentOutOfRangeException>(() => UnitScale.ToMeters(double.PositiveInfinity, DrawingUnit.Yard), "Infinite input must remain rejected.");
        }

        private static void KeepsNonFiniteResultGuard()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(double.MaxValue, DrawingUnit.Parsec), "Finite input whose conversion overflows must remain rejected.");
        }

        private static void AssertThrows<TException>(Action action, string message) where TException : Exception
        {
            try { action(); } catch (TException) { return; }
            throw new InvalidOperationException(message);
        }

        private static void AssertNear(double actual, double expected, double tolerance, string message)
        {
            if (Math.Abs(actual - expected) > tolerance) throw new InvalidOperationException($"{message} Expected {expected:R}, got {actual:R}.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
