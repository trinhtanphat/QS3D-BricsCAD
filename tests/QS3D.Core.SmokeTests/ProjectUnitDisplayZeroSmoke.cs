using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectUnitDisplayZeroSmoke
    {
        internal static void Run()
        {
            CanonicalizesRoundedNegativeZero();
            CanonicalizesExplicitNegativeZero();
            PreservesOrdinaryRounding();
        }

        private static void CanonicalizesRoundedNegativeZero()
        {
            var policy = new ProjectUnitPolicy(LengthUnit.Millimeter, 3);
            var rounded = policy.RoundForDisplay(-0.0004d);
            if (BitConverter.DoubleToInt64Bits(rounded) != 0L)
                throw new InvalidOperationException("Display rounding preserved an IEEE negative-zero sign bit.");
        }

        private static void CanonicalizesExplicitNegativeZero()
        {
            var policy = new ProjectUnitPolicy(LengthUnit.Millimeter, 3);
            var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
            var rounded = policy.RoundForDisplay(negativeZero);
            if (BitConverter.DoubleToInt64Bits(rounded) != 0L)
                throw new InvalidOperationException("Explicit IEEE negative zero was not canonicalized for display.");
        }

        private static void PreservesOrdinaryRounding()
        {
            var policy = new ProjectUnitPolicy(LengthUnit.Millimeter, 3);
            if (policy.RoundForDisplay(1.2346d) != 1.235d)
                throw new InvalidOperationException("Positive display rounding changed.");
            if (policy.RoundForDisplay(-1.2346d) != -1.235d)
                throw new InvalidOperationException("Negative display rounding changed.");
        }
    }

    internal static class ProjectUnitDisplayZeroSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectUnitDisplayZeroSmoke.Run();
    }
}
