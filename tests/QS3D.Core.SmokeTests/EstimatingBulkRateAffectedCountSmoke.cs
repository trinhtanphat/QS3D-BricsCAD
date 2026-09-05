using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingBulkRateAffectedCountSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            MixedExistingAndUnknownSelectionCountsOnlyExistingLines();
            AllExistingSelectionRetainsAffectedCountAndCommitReadiness();
        }

        private static void MixedExistingAndUnknownSelectionCountsOnlyExistingLines()
        {
            var existing = new EstimatingLine(
                "line-existing",
                "quantity-source",
                "q-rev-1",
                2m,
                "m3");
            var portfolio = new EstimatingPortfolio(new[] { existing });
            var request = new BulkRateAssignmentRequest(
                new[] { existing.LineId, "line-missing" },
                "COST-01",
                "rate-source",
                "r-rev-1",
                new[] { new UnitRateAssignment("m3", 10m) });

            var preview = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request);

            if (preview.AffectedCount != 1)
                throw new Exception("Bulk rate preview affected count must include only selected ids that resolve to existing portfolio lines.");
            if (preview.UnmatchedLineIds.Count != 1 ||
                !string.Equals(preview.UnmatchedLineIds[0], "line-missing", StringComparison.Ordinal))
                throw new Exception("Unknown selected ids must remain explicit unmatched rows.");
            if (preview.CanCommit)
                throw new Exception("A bulk rate preview with an unknown selected id must remain non-committable.");
            if (preview.UnitDistribution.Count != 1 || preview.UnitDistribution[0].LineCount != 1 || preview.UnitDistribution[0].Quantity != 2m)
                throw new Exception("Unknown selected ids must not inflate bulk preview unit distribution.");
            if (preview.TotalBefore != 0m || preview.TotalAfter != 20m)
                throw new Exception("Unknown selected ids must not inflate bulk preview commercial totals.");
        }

        private static void AllExistingSelectionRetainsAffectedCountAndCommitReadiness()
        {
            var first = new EstimatingLine(
                "line-a",
                "quantity-source",
                "q-rev-1",
                2m,
                "m3");
            var second = new EstimatingLine(
                "line-b",
                "quantity-source",
                "q-rev-1",
                3m,
                "m3");
            var portfolio = new EstimatingPortfolio(new[] { first, second });
            var request = new BulkRateAssignmentRequest(
                new[] { first.LineId, second.LineId },
                "COST-01",
                "rate-source",
                "r-rev-1",
                new[] { new UnitRateAssignment("m3", 10m) });

            var preview = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request);

            if (preview.AffectedCount != 2)
                throw new Exception("All-existing bulk rate preview must retain one affected row per resolved selected line.");
            if (!preview.CanCommit || preview.UnmatchedLineIds.Count != 0 || preview.BlockedLineIds.Count != 0)
                throw new Exception("All-existing compatible bulk rate preview must remain committable.");
            if (preview.UnitDistribution.Count != 1 || preview.UnitDistribution[0].LineCount != 2 || preview.UnitDistribution[0].Quantity != 5m)
                throw new Exception("All-existing bulk rate preview must preserve unit distribution semantics.");
            if (preview.TotalBefore != 0m || preview.TotalAfter != 50m)
                throw new Exception("All-existing bulk rate preview must preserve commercial total semantics.");
        }
    }
}
