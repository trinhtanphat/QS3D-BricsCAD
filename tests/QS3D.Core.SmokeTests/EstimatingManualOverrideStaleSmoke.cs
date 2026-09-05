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
    }
}
