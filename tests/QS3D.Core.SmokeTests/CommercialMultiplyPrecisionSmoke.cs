using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialMultiplyPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            NonRepresentableProductRejectsInsteadOfRounding();
            ExactScaleTwentyEightProductRemainsAccepted();
            ReducibleHighScaleProductRemainsAccepted();
            OverflowContractRemainsFailClosed();
        }

        private static void NonRepresentableProductRejectsInsteadOfRounding()
        {
            var line = PricedLine(
                0.0000000000000000000000000001m,
                1.5m);

            try
            {
                var ignored = line.Amount;
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf("precision loss", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Commercial multiplication precision-loss rejection must remain explicit.", ex);
                return;
            }

            throw new Exception("A non-representable nonzero commercial product must reject instead of silently rounding.");
        }

        private static void ExactScaleTwentyEightProductRemainsAccepted()
        {
            var line = PricedLine(
                0.00000000000001m,
                0.00000000000001m);

            if (line.Amount != 0.0000000000000000000000000001m)
                throw new Exception("Exactly representable scale-28 commercial multiplication must remain exact.");
        }

        private static void ReducibleHighScaleProductRemainsAccepted()
        {
            var line = PricedLine(
                0.00000000000001m,
                0.000000000000010m);

            if (line.Amount != 0.0000000000000000000000000001m)
                throw new Exception("High-scale multiplication reducible by trailing decimal zeros must remain exact.");
        }

        private static void OverflowContractRemainsFailClosed()
        {
            var line = PricedLine(decimal.MaxValue, 2m);

            try
            {
                var ignored = line.Amount;
            }
            catch (OverflowException)
            {
                return;
            }

            throw new Exception("Commercial multiplication overflow must remain fail-closed.");
        }

        private static EstimatingLine PricedLine(decimal quantity, decimal rate)
        {
            return new EstimatingLine(
                "line-multiply-precision",
                "quantity-source",
                "quantity-revision",
                quantity,
                "ea",
                "COST-01",
                "rate-source",
                "rate-revision",
                rate);
        }
    }
}
