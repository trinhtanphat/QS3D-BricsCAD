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
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Yard),
                "A positive non-identity yard conversion rounded back to the unchanged subnormal value should fail closed.");
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(-double.Epsilon, DrawingUnit.Yard),
                "A negative non-identity yard conversion rounded back to the unchanged subnormal value should fail closed.");
        }

        private static void RejectsSwallowedFromMetersConversion()
        {
            AssertThrows<OverflowException>(() => UnitScale.FromMeters(double.Epsilon, DrawingUnit.Yard),
                "A non-identity meter-to-yard conversion rounded back to the unchanged subnormal value should fail closed.");
            AssertThrows<OverflowException>(() => UnitScale.FromMeters(-double.Epsilon, DrawingUnit.Yard),
                "A negative meter-to-yard conversion rounded back to the unchanged subnormal value should fail closed.");
        }

        private static void RejectsSwallowedAreaAndVolumeConversions()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToSquareMeters(double.Epsilon, DrawingUnit.Yard),
                "A non-identity square-yard conversion rounded back to the unchanged subnormal value should fail closed.");
            AssertThrows<OverflowException>(() => UnitScale.ToCubicMeters(double.Epsilon, DrawingUnit.Yard),
                "A non-identity cubic-yard conversion rounded back to the unchanged subnormal value should fail closed.");
        }

        private static void PreservesMeterIdentity()
        {
            var positive = UnitScale.ToMeters(double.Epsilon, DrawingUnit.Meter);
            Assert(positive == double.Epsilon, "Meter identity conversion must preserve the smallest positive representable value.");

            var negative = UnitScale.FromMeters(-double.Epsilon, DrawingUnit.Meter);
            Assert(negative == -double.Epsilon, "Meter identity conversion must preserve the smallest negative representable value.");
        }

        private static void PreservesZeroConversion()
        {
            var positiveZero = UnitScale.ToMeters(0d, DrawingUnit.Yard);
            Assert(positiveZero == 0d, "Zero should remain a valid unit-conversion input.");
            Assert(BitConverter.DoubleToInt64Bits(positiveZero) == 0L,
                "Positive zero conversion should remain canonical positive zero.");

            var negativeZero = UnitScale.ToMeters(-0d, DrawingUnit.Yard);
            Assert(negativeZero == 0d, "Negative zero should remain a valid unit-conversion input.");
            Assert(BitConverter.DoubleToInt64Bits(negativeZero) == 0L,
                "Negative zero conversion should canonicalize to positive zero.");
        }

        private static void PreservesOrdinaryLinearConversions()
        {
            var yardsToMeters = UnitScale.ToMeters(2d, DrawingUnit.Yard);
            AssertNear(yardsToMeters, 1.8288d, 1e-12d,
                "Ordinary yard-to-meter conversion changed unexpectedly.");

            var negativeYardsToMeters = UnitScale.ToMeters(-2d, DrawingUnit.Yard);
            AssertNear(negativeYardsToMeters, -1.8288d, 1e-12d,
                "Ordinary negative yard-to-meter conversion changed unexpectedly.");

            var metersToYards = UnitScale.FromMeters(1.8288d, DrawingUnit.Yard);
            AssertNear(metersToYards, 2d, 1e-12d,
                "Ordinary meter-to-yard conversion changed unexpectedly.");
        }

        private static void PreservesOrdinaryAreaAndVolumeConversions()
        {
            var square = UnitScale.ToSquareMeters(2d, DrawingUnit.Yard);
            AssertNear(square, 2d * 0.9144d * 0.9144d, 1e-12d,
                "Ordinary square-yard conversion changed unexpectedly.");

            var cubic = UnitScale.ToCubicMeters(2d, DrawingUnit.Yard);
            AssertNear(cubic, 2d * 0.9144d * 0.9144d * 0.9144d, 1e-12d,
                "Ordinary cubic-yard conversion changed unexpectedly.");
        }

        private static void KeepsUnderflowToZeroGuard()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Foot),
                "Existing underflow-to-zero behavior must remain fail closed.");
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(-double.Epsilon, DrawingUnit.Foot),
                "Existing negative underflow-to-zero behavior must remain fail closed.");
        }

        private static void KeepsNonFiniteInputGuard()
        {
            AssertThrows<ArgumentOutOfRangeException>(() => UnitScale.ToMeters(double.NaN, DrawingUnit.Yard),
                "NaN unit-conversion input must remain rejected.");
            AssertThrows<ArgumentOutOfRangeException>(() => UnitScale.ToMeters(double.PositiveInfinity, DrawingUnit.Yard),
                "Infinite unit-conversion input must remain rejected.");
        }

        private static void KeepsNonFiniteResultGuard()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(double.MaxValue, DrawingUnit.Parsec),
                "A finite input whose unit conversion overflows must remain rejected.");
        }

        private static void AssertThrows<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void AssertNear(double actual, double expected, double tolerance, string message)
        {
            if (Math.Abs(actual - expected) > tolerance)
            {
                throw new InvalidOperationException($"{message} Expected {expected:R}, got {actual:R}.");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
