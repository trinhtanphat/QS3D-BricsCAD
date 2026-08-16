using System;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class UnitScaleSwallowedConversionSmoke
    {
        internal static void Run()
        {
            RejectsSwallowedNonIdentityConversion();
            PreservesMeterIdentity();
            PreservesZeroConversion();
            PreservesOrdinaryYardConversion();
            KeepsUnderflowToZeroGuard();
        }

        private static void RejectsSwallowedNonIdentityConversion()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Yard),
                "A non-identity yard conversion rounded back to the unchanged subnormal value should fail closed.");
        }

        private static void PreservesMeterIdentity()
        {
            var result = UnitScale.ToMeters(double.Epsilon, DrawingUnit.Meter);
            Assert(result == double.Epsilon, "Meter identity conversion must preserve the smallest positive representable value.");
        }

        private static void PreservesZeroConversion()
        {
            var result = UnitScale.ToMeters(0d, DrawingUnit.Yard);
            Assert(result == 0d, "Zero should remain a valid unit-conversion input.");
            Assert(BitConverter.DoubleToInt64Bits(result) == 0L, "Zero conversion should canonicalize negative zero to positive zero.");
        }

        private static void PreservesOrdinaryYardConversion()
        {
            var result = UnitScale.ToMeters(2d, DrawingUnit.Yard);
            Assert(Math.Abs(result - 1.8288d) < 1e-12d, "Ordinary yard-to-meter conversion changed unexpectedly.");
        }

        private static void KeepsUnderflowToZeroGuard()
        {
            AssertThrows<OverflowException>(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Foot),
                "Existing underflow-to-zero behavior must remain fail closed.");
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

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
