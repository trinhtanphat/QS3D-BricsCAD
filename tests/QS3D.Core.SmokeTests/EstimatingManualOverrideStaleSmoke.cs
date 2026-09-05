using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingManualOverrideStaleSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            StaleLineRejectsManualOverrideWithoutAuditMutation();
            ValidCurrentLineStillAcceptsManualOverride();
            StaleLineRejectsManualOverrideRemovalWithoutAuditMutation();
            BlockedLineRejectsManualOverrideRemovalWithoutAuditMutation();
            ValidCurrentLineStillRemovesManualOverride();
        }

        private static void StaleLineRejectsManualOverrideWithoutAuditMutation()
        {
            var line = new EstimatingLine(
                "line-stale",
                "quantity-source",
                "q-rev-1",
                2m,
                "m3",
                "COST-01",
                "rate-source",
                "r-rev-1",
                10m,
                null,
                string.Empty,
                false,
                string.Empty,
                true,
                "quantity source changed");
            var portfolio = new EstimatingPortfolio(new[] { line });
            var audit = new CommercialAuditLog();
            var service = new EstimatingWorkflowService();

            var rejected = false;
            try
            {
                service.ApplyManualRateOverride(
                    portfolio,
                    line.LineId,
                    12m,
                    "commercial adjustment",
                    audit,
                    "tester",
                    "corr-stale",
                    new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc));
            }
            catch (InvalidOperationException ex)
            {
                rejected = ex.Message.IndexOf("stale", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!rejected)
                throw new Exception("A stale estimating line must reject a manual rate override fail-closed.");
            if (audit.Events.Count != 0)
                throw new Exception("Rejected stale manual override must not append an audit event.");
            var unchanged = portfolio.GetLine(line.LineId);
            if (unchanged.OverrideRate.HasValue || !unchanged.IsStale)
                throw new Exception("Rejected stale manual override must preserve the original portfolio state.");
        }

        private static void ValidCurrentLineStillAcceptsManualOverride()
        {
            var line = new EstimatingLine(
                "line-current",
                "quantity-source",
                "q-rev-1",
                2m,
                "m3",
                "COST-01",
                "rate-source",
                "r-rev-1",
                10m);
            var portfolio = new EstimatingPortfolio(new[] { line });
            var audit = new CommercialAuditLog();
            var service = new EstimatingWorkflowService();

            var result = service.ApplyManualRateOverride(
                portfolio,
                line.LineId,
                12m,
                "commercial adjustment",
                audit,
                "tester",
                "corr-current",
                new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc));

            var updated = result.GetLine(line.LineId);
            if (updated.OverrideRate != 12m || updated.State != EstimatingReadinessState.PricedWithOverride)
                throw new Exception("A current priced line must retain valid manual override behavior.");
            if (audit.Events.Count != 1 || !string.Equals(audit.Events[0].Action, "rate-override-created", StringComparison.Ordinal))
                throw new Exception("Valid manual override must retain its audit contract.");
        }

        private static void StaleLineRejectsManualOverrideRemovalWithoutAuditMutation()
        {
            var line = OverriddenLine(
                "line-stale-remove",
                isBlocked: false,
                blockReason: string.Empty,
                isStale: true,
                staleReason: "quantity source changed");
            var portfolio = new EstimatingPortfolio(new[] { line });
            var audit = new CommercialAuditLog();
            var service = new EstimatingWorkflowService();

            var rejected = false;
            try
            {
                service.RemoveManualRateOverride(
                    portfolio,
                    line.LineId,
                    "restore base rate",
                    audit,
                    "tester",
                    "corr-stale-remove",
                    new DateTime(2026, 9, 5, 0, 1, 0, DateTimeKind.Utc));
            }
            catch (InvalidOperationException ex)
            {
                rejected = ex.Message.IndexOf("stale", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!rejected)
                throw new Exception("A stale estimating line must reject manual rate override removal fail-closed.");
            if (audit.Events.Count != 0)
                throw new Exception("Rejected stale override removal must not append an audit event.");
            var unchanged = portfolio.GetLine(line.LineId);
            if (unchanged.OverrideRate != 12m || !unchanged.IsStale)
                throw new Exception("Rejected stale override removal must preserve the original portfolio state.");
        }

        private static void BlockedLineRejectsManualOverrideRemovalWithoutAuditMutation()
        {
            var line = OverriddenLine(
                "line-blocked-remove",
                isBlocked: true,
                blockReason: "commercial review hold",
                isStale: false,
                staleReason: string.Empty);
            var portfolio = new EstimatingPortfolio(new[] { line });
            var audit = new CommercialAuditLog();
            var service = new EstimatingWorkflowService();

            var rejected = false;
            try
            {
                service.RemoveManualRateOverride(
                    portfolio,
                    line.LineId,
                    "restore base rate",
                    audit,
                    "tester",
                    "corr-blocked-remove",
                    new DateTime(2026, 9, 5, 0, 2, 0, DateTimeKind.Utc));
            }
            catch (InvalidOperationException ex)
            {
                rejected = ex.Message.IndexOf("blocked", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!rejected)
                throw new Exception("A blocked estimating line must reject manual rate override removal fail-closed.");
            if (audit.Events.Count != 0)
                throw new Exception("Rejected blocked override removal must not append an audit event.");
            var unchanged = portfolio.GetLine(line.LineId);
            if (unchanged.OverrideRate != 12m || !unchanged.IsBlocked)
                throw new Exception("Rejected blocked override removal must preserve the original portfolio state.");
        }

        private static void ValidCurrentLineStillRemovesManualOverride()
        {
            var line = OverriddenLine(
                "line-current-remove",
                isBlocked: false,
                blockReason: string.Empty,
                isStale: false,
                staleReason: string.Empty);
            var portfolio = new EstimatingPortfolio(new[] { line });
            var audit = new CommercialAuditLog();
            var service = new EstimatingWorkflowService();

            var result = service.RemoveManualRateOverride(
                portfolio,
                line.LineId,
                "restore base rate",
                audit,
                "tester",
                "corr-current-remove",
                new DateTime(2026, 9, 5, 0, 3, 0, DateTimeKind.Utc));

            var updated = result.GetLine(line.LineId);
            if (updated.OverrideRate.HasValue || updated.State != EstimatingReadinessState.Priced)
                throw new Exception("A current overridden line must retain valid manual override removal behavior.");
            if (audit.Events.Count != 1 || !string.Equals(audit.Events[0].Action, "rate-override-removed", StringComparison.Ordinal))
                throw new Exception("Valid manual override removal must retain its audit contract.");
        }

        private static EstimatingLine OverriddenLine(
            string lineId,
            bool isBlocked,
            string blockReason,
            bool isStale,
            string staleReason)
        {
            return new EstimatingLine(
                lineId,
                "quantity-source",
                "q-rev-1",
                2m,
                "m3",
                "COST-01",
                "rate-source",
                "r-rev-1",
                10m,
                12m,
                "existing override",
                isBlocked,
                blockReason,
                isStale,
                staleReason);
        }
    }
}
