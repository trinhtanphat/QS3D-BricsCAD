using System;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkRateAssignmentStaleAdmissionSmoke
    {
        internal static void Run()
        {
            AlreadyStaleLineIsBlockedAtPreview();
            StaleAfterPreviewIsRejectedWithoutMutation();
            ActiveLineStillCommits();
            Console.WriteLine("PASS bulk rate assignment stale admission");
        }

        private static void AlreadyStaleLineIsBlockedAtPreview()
        {
            var service = new EstimatingWorkflowService();
            var portfolio = new EstimatingPortfolio(new[]
            {
                new EstimatingLine("L1", "quantity-source-1", "qrev-1", 2m, "m2", isStale: true, staleReason: "quantity provenance changed")
            });
            var request = Request("L1", 12.5m);
            var preview = service.PreviewBulkRateAssignment(portfolio, request);

            Require(!preview.CanCommit, "A stale estimating line must block bulk-rate preview commit.");
            Require(preview.BlockedLineIds.Count == 1 && preview.BlockedLineIds[0] == "L1",
                "The stale line must be surfaced in the preview blocking set.");

            var audit = new CommercialAuditLog();
            RequireThrows<InvalidOperationException>(() => service.CommitBulkRateAssignment(
                portfolio, preview, audit, "smoke", "stale-preview", Utc(1)),
                "Commit must fail closed when the preview was created from an already-stale line.");
            Require(audit.Events.Count == 0, "Rejected stale preview must not append commercial audit records.");
            Require(!portfolio.GetLine("L1").ReferencedRate.HasValue && portfolio.GetLine("L1").CostCode.Length == 0,
                "Rejected stale preview must not mutate rate state.");
        }

        private static void StaleAfterPreviewIsRejectedWithoutMutation()
        {
            var service = new EstimatingWorkflowService();
            var active = new EstimatingPortfolio(new[]
            {
                new EstimatingLine("L2", "quantity-source-2", "qrev-1", 3m, "m2")
            });
            var request = Request("L2", 8m);
            var ready = service.PreviewBulkRateAssignment(active, request);
            Require(ready.CanCommit, "Active-line control preview must be committable before staleness is introduced.");

            var stale = service.MarkQuantitySourceStale(active, "L2", "quantity source changed after preview");
            var audit = new CommercialAuditLog();
            RequireThrows<InvalidOperationException>(() => service.CommitBulkRateAssignment(
                stale, ready, audit, "smoke", "stale-race", Utc(2)),
                "Commit must reject a preview when the selected line becomes stale after preview.");

            var line = stale.GetLine("L2");
            Require(line.IsStale && line.StaleReason == "quantity source changed after preview",
                "Rejected commit must preserve stale provenance.");
            Require(!line.ReferencedRate.HasValue && line.CostCode.Length == 0,
                "Rejected stale-after-preview commit must not mutate rate state.");
            Require(audit.Events.Count == 0, "Rejected stale-after-preview commit must not append audit records.");
        }

        private static void ActiveLineStillCommits()
        {
            var service = new EstimatingWorkflowService();
            var portfolio = new EstimatingPortfolio(new[]
            {
                new EstimatingLine("L3", "quantity-source-3", "qrev-1", 4m, "m2")
            });
            var request = Request("L3", 5m);
            var preview = service.PreviewBulkRateAssignment(portfolio, request);
            Require(preview.CanCommit, "Non-stale estimating lines must remain eligible for bulk rate assignment.");

            var audit = new CommercialAuditLog();
            var committed = service.CommitBulkRateAssignment(
                portfolio, preview, audit, "smoke", "active-control", Utc(3));
            var line = committed.GetLine("L3");
            Require(line.CostCode == "COST-01" && line.RateSourceId == "rate-book" && line.RateRevision == "r1",
                "Successful control commit must retain referenced-rate provenance.");
            Require(line.ReferencedRate == 5m && line.Amount == 20m,
                "Successful control commit must apply the requested rate exactly.");
            Require(audit.Events.Count == 1 && audit.Events[0].Action == "rate-assigned",
                "Successful control commit must append one rate-assigned audit record.");
        }

        private static BulkRateAssignmentRequest Request(string lineId, decimal rate)
        {
            return new BulkRateAssignmentRequest(
                new[] { lineId },
                "COST-01",
                "rate-book",
                "r1",
                new[] { new UnitRateAssignment("m2", rate) });
        }

        private static DateTime Utc(int minute)
        {
            return new DateTime(2026, 9, 3, 0, minute, 0, DateTimeKind.Utc);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void RequireThrows<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}