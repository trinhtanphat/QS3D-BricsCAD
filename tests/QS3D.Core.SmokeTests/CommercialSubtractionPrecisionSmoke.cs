using System;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialSubtractionPrecisionSmoke
    {
        public static void Run()
        {
            BulkRatePreviewRejectsSwallowedDeduction();
            BulkRatePreviewKeepsRepresentableDeduction();
        }

        private static void BulkRatePreviewRejectsSwallowedDeduction()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("DELTA-HIGH", 0.1m)
            });
            var request = new BulkRateAssignmentRequest(
                new[] { "DELTA-HIGH" },
                "COST-NEXT",
                "rate-source-next",
                "rate-revision-next",
                new[]
                {
                    new UnitRateAssignment("m", 70000000000000000000000000000m)
                });

            ExpectOverflow(
                () => _ = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request),
                "subtraction precision loss",
                "Bulk-rate preview must reject a non-zero prior amount swallowed by decimal subtraction.");
        }

        private static void BulkRatePreviewKeepsRepresentableDeduction()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("DELTA-NORMAL", 0.1m)
            });
            var request = new BulkRateAssignmentRequest(
                new[] { "DELTA-NORMAL" },
                "COST-NEXT",
                "rate-source-next",
                "rate-revision-next",
                new[]
                {
                    new UnitRateAssignment("m", 100m)
                });

            var preview = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request);
            if (preview.ValueDelta != 99.9m)
                throw new Exception(
                    "Representable commercial subtraction changed unexpectedly. Expected=99.9, actual=" +
                    preview.ValueDelta + ".");
        }

        private static EstimatingLine PricedLine(string id, decimal rate)
        {
            return new EstimatingLine(
                id,
                "quantity-source-" + id,
                "quantity-revision",
                1m,
                "m",
                "cost-" + id,
                "rate-source",
                "rate-revision",
                referencedRate: rate);
        }

        private static void ExpectOverflow(Action action, string expectedMessageFragment, string message)
        {
            try
            {
                action();
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(message + " Actual diagnostic: " + ex.Message);
                return;
            }

            throw new Exception(message);
        }
    }
}
