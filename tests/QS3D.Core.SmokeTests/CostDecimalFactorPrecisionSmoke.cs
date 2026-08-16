using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostDecimalFactorPrecisionSmoke
    {
        private const decimal MinimumPositive = 0.0000000000000000000000000001m;

        internal static void Run()
        {
            RejectsSwallowedRightFactor();
            RejectsSwallowedLeftFactor();
            PreservesIdentityAndZero();
            PreservesOrdinaryMultiplicationAndUnderflowGuard();
        }

        private static void RejectsSwallowedRightFactor()
        {
            var component = new CostResourceComponent("TINY-QTY", "Tiny quantity", "ea", MinimumPositive, 1.1m);
            Throws<OverflowException>(() => { var _ = component.ExtendedUnitCost; });
        }

        private static void RejectsSwallowedLeftFactor()
        {
            var component = new CostResourceComponent("TINY-RATE", "Tiny rate", "ea", 1.1m, MinimumPositive);
            Throws<OverflowException>(() => { var _ = component.ExtendedUnitCost; });
        }

        private static void PreservesIdentityAndZero()
        {
            var identity = new CostResourceComponent("IDENTITY", "Identity", "ea", MinimumPositive, 1m);
            Equal(MinimumPositive, identity.ExtendedUnitCost, "Exact identity multiplication must remain accepted.");

            var zero = new CostResourceComponent("ZERO", "Zero", "ea", 0m, 1.1m);
            Equal(0m, zero.ExtendedUnitCost, "Zero multiplication semantics changed.");
        }

        private static void PreservesOrdinaryMultiplicationAndUnderflowGuard()
        {
            var ordinary = new CostResourceComponent("ORDINARY", "Ordinary", "ea", 2m, 3m);
            Equal(6m, ordinary.ExtendedUnitCost, "Ordinary representable multiplication changed.");

            var underflow = new CostResourceComponent("UNDERFLOW", "Underflow", "ea", MinimumPositive, 0.1m);
            Throws<OverflowException>(() => { var _ = underflow.ExtendedUnitCost; });
        }

        private static void Equal(decimal expected, decimal actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }

    internal static class CostDecimalFactorPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CostDecimalFactorPrecisionSmoke.Run();
        }
    }
}
