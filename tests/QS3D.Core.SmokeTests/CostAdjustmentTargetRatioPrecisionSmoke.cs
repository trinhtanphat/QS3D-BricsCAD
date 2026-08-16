using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostAdjustmentTargetRatioPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RepresentableTinyDecreaseIsPreserved();
            LargeDecreaseUsesOverflowSafeOrdering();
            OrdinaryIncreaseRemainsStable();
            ZeroBaseSemanticsRemainStable();
        }

        private static void RepresentableTinyDecreaseIsPreserved()
        {
            var service = new CostAdjustmentService();
            var result = service.AdjustToTotal(decimal.MaxValue, decimal.MaxValue - 1m);

            Assert(result.AdjustedTotal == decimal.MaxValue - 1m, "Adjusted total changed unexpectedly.");
            Assert(result.CombinedRatioPercent < 0m, "A lower target total must produce a negative combined ratio.");
            Assert(result.CombinedRatioPercent != 0m, "A representable nonzero target-total ratio must not collapse to zero before percent scaling.");
            Assert(result.AdjustmentRatioPercent == result.CombinedRatioPercent, "AdjustToTotal must expose the computed combined ratio as the adjustment ratio.");
        }

        private static void LargeDecreaseUsesOverflowSafeOrdering()
        {
            var service = new CostAdjustmentService();
            var result = service.AdjustToTotal(decimal.MaxValue, 0m);

            Assert(result.CombinedRatioPercent == -100m, "A full decrease from a positive base must remain exactly -100 percent when scale-first arithmetic overflows.");
            Assert(result.AdjustmentRatioPercent == -100m, "A full target-total decrease must remain exactly -100 percent in the adjustment ratio.");
        }

        private static void OrdinaryIncreaseRemainsStable()
        {
            var service = new CostAdjustmentService();
            var result = service.AdjustToTotal(100m, 110m);

            Assert(result.CombinedRatioPercent == 10m, "Ordinary target-total adjustment ratio changed unexpectedly.");
            Assert(result.AdjustmentRatioPercent == 10m, "Ordinary target-total adjustment ratio projection changed unexpectedly.");
        }

        private static void ZeroBaseSemanticsRemainStable()
        {
            var service = new CostAdjustmentService();
            var zero = service.AdjustToTotal(0m, 0m);
            Assert(zero.CombinedRatioPercent == 0m, "Zero base and zero target must remain a zero adjustment ratio.");

            var error = Capture<InvalidOperationException>(() => service.AdjustToTotal(0m, 1m));
            Assert(
                error.Message == "A zero base total cannot produce a non-zero adjusted total.",
                "Zero-base nonzero-target failure semantics changed unexpectedly.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
