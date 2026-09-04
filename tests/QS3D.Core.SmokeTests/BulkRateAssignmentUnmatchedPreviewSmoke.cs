using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkRateAssignmentUnmatchedPreviewSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MixedKnownUnknownSelectionReturnsReviewablePreview();
            AllUnknownSelectionRemainsReviewable();
            KnownLineLookupRemainsCaseInsensitive();
            UnmatchedPreviewCannotCommitOrPublishAudit();
            Console.WriteLine("PASS bulk rate assignment unmatched preview");
        }

        private static void MixedKnownUnknownSelectionReturnsReviewablePreview()
        {
            var portfolio = Portfolio(
                new EstimatingLine("LINE-1", "Q-1", "QREV-1", 10m, "m"),
                new EstimatingLine(
                    "BLOCKED-1", "Q-2", "QREV-2", 5m, "m",
                    isBlocked: true, blockReason: "Awaiting commercial review"));
            var request = Request("MISSING-B", "LINE-1", "MISSING-A", "BLOCKED-1");

            var preview = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request);

            Require(preview.AffectedCount == 4, "affected count must preserve requested selection cardinality");
            Require(preview.UnmatchedLineIds.Count == 2, "two unknown selected ids must be reported as unmatched");
            Require(preview.UnmatchedLineIds[0] == "MISSING-A" && preview.UnmatchedLineIds[1] == "MISSING-B",
                "unknown selected ids must be sorted deterministically");
            Require(preview.BlockedLineIds.Count == 1 && preview.BlockedLineIds[0] == "BLOCKED-1",
                "blocked matched line must coexist with unknown selected ids");
            Require(preview.UnitDistribution.Count == 1, "only matched lines may contribute to unit distribution");
            Require(preview.UnitDistribution[0].Unit == "m", "matched unit distribution must preserve canonical unit");
            Require(preview.UnitDistribution[0].LineCount == 2, "only two matched lines may contribute to unit line count");
            Require(preview.UnitDistribution[0].Quantity == 15m, "unknown selected ids must not contribute quantity");
            Require(preview.TotalBefore == 0m, "unpriced matched controls must keep total before at zero");
            Require(preview.TotalAfter == 20m, "unknown and blocked-unpriced rows must not fabricate value");
            Require(preview.ReplacementCount == 0, "unknown selected ids must not fabricate replacement count");
            Require(!preview.CanCommit, "preview containing unknown selected ids must fail closed for commit");
        }

        private static void AllUnknownSelectionRemainsReviewable()
        {
            var portfolio = Portfolio(new EstimatingLine("LINE-1", "Q-1", "QREV-1", 10m, "m"));
            var request = Request("GHOST-2", "GHOST-1");

            var preview = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request);

            Require(preview.AffectedCount == 2, "all-unknown preview must preserve requested cardinality");
            Require(preview.UnmatchedLineIds.Count == 2, "all unknown ids must be reviewable");
            Require(preview.UnmatchedLineIds[0] == "GHOST-1" && preview.UnmatchedLineIds[1] == "GHOST-2",
                "all-unknown output must be deterministic");
            Require(preview.UnitDistribution.Count == 0, "all-unknown selection must not create unit distribution");
            Require(preview.BlockedLineIds.Count == 0, "all-unknown selection must not fabricate blocked ids");
            Require(preview.TotalBefore == 0m && preview.TotalAfter == 0m,
                "all-unknown selection must not contribute commercial totals");
            Require(preview.ReplacementCount == 0, "all-unknown selection must not fabricate replacements");
            Require(!preview.CanCommit, "all-unknown preview must not be committable");
        }

        private static void KnownLineLookupRemainsCaseInsensitive()
        {
            var portfolio = Portfolio(new EstimatingLine("LINE-Case", "Q-1", "QREV-1", 3m, "M"));
            var request = Request("line-case");

            var preview = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request);

            Require(preview.UnmatchedLineIds.Count == 0, "known line id lookup must remain case-insensitive");
            Require(preview.BlockedLineIds.Count == 0, "known ordinary line must not be blocked");
            Require(preview.UnitDistribution.Count == 1 && preview.UnitDistribution[0].LineCount == 1,
                "case-insensitive known line must contribute exactly once");
            Require(preview.TotalAfter == 6m, "case-insensitive known line must retain rate assignment semantics");
            Require(preview.CanCommit, "fully matched ordinary preview must remain committable");
        }

        private static void UnmatchedPreviewCannotCommitOrPublishAudit()
        {
            var portfolio = Portfolio(new EstimatingLine("LINE-1", "Q-1", "QREV-1", 10m, "m"));
            var service = new EstimatingWorkflowService();
            var preview = service.PreviewBulkRateAssignment(portfolio, Request("LINE-1", "MISSING-1"));
            var audit = new CommercialAuditLog();

            try
            {
                service.CommitBulkRateAssignment(
                    portfolio,
                    preview,
                    audit,
                    "C02 regression",
                    "bulk-rate-unmatched",
                    new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc));
            }
            catch (InvalidOperationException)
            {
                Require(audit.Events.Count == 0, "unmatched commit refusal must publish no audit event");
                return;
            }

            throw new InvalidOperationException("bulk rate assignment preview with unmatched selected ids must refuse commit");
        }

        private static EstimatingPortfolio Portfolio(params EstimatingLine[] lines) => new EstimatingPortfolio(lines);

        private static BulkRateAssignmentRequest Request(params string[] lineIds) =>
            new BulkRateAssignmentRequest(
                lineIds,
                "03.10",
                "RATE-BOOK",
                "RATE-REV-1",
                new[] { new UnitRateAssignment("m", 2m) });

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
