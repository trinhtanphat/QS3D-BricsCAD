using System;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingStateProvenanceCoherenceSmoke
    {
        internal static void Run()
        {
            ConstructorRejectsInactiveReasons();
            ConstructorAcceptsCanonicalActiveReasons();
            StaleStateBlocksBulkAssignmentAndSurvivesManualRateTransitions();
            BlockedBulkAssignmentFailsWithoutAuditMutation();
        }

        private static void ConstructorRejectsInactiveReasons()
        {
            Throws<ArgumentException>(() => new EstimatingLine(
                "L1", "Q1", "R1", 2m, "m",
                blockReason: "latent block reason"));

            Throws<ArgumentException>(() => new EstimatingLine(
                "L2", "Q2", "R2", 2m, "m",
                staleReason: "latent stale reason"));
        }

        private static void ConstructorAcceptsCanonicalActiveReasons()
        {
            var blocked = new EstimatingLine(
                "B1", "QB", "RB", 1m, "m",
                isBlocked: true,
                blockReason: "Awaiting approved quantity source");
            Equal(true, blocked.IsBlocked);
            Equal("Awaiting approved quantity source", blocked.BlockReason);
            Equal(EstimatingReadinessState.Blocked, blocked.State);

            var stale = new EstimatingLine(
                "S1", "QS", "RS", 1m, "m",
                isStale: true,
                staleReason: "Quantity revision superseded");
            Equal(true, stale.IsStale);
            Equal("Quantity revision superseded", stale.StaleReason);
            Equal(EstimatingReadinessState.Stale, stale.State);
        }

        private static void StaleStateBlocksBulkAssignmentAndSurvivesManualRateTransitions()
        {
            var service = new EstimatingWorkflowService();
            var audit = new CommercialAuditLog();
            var stale = new EstimatingLine(
                "S2", "QS2", "RQ2", 3m, "m",
                costCode: "CC-100",
                rateSourceId: "RATEBOOK",
                rateRevision: "2026-08",
                referencedRate: 10m,
                isStale: true,
                staleReason: "Source quantity changed");
            var portfolio = new EstimatingPortfolio(new[] { stale });
            var request = new BulkRateAssignmentRequest(
                new[] { "S2" },
                "CC-100",
                "RATEBOOK",
                "2026-09",
                new[] { new UnitRateAssignment("m", 11m) });

            var preview = service.PreviewBulkRateAssignment(portfolio, request);
            Equal(false, preview.CanCommit);
            Equal(1, preview.BlockedLineIds.Count);
            Equal("S2", preview.BlockedLineIds[0]);
            Throws<InvalidOperationException>(() => service.CommitBulkRateAssignment(
                portfolio,
                preview,
                audit,
                "w1-smoke",
                "bulk-1",
                new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc)));
            Equal(0, audit.Events.Count);

            var rated = portfolio.GetLine("S2");
            Equal(true, rated.IsStale);
            Equal("Source quantity changed", rated.StaleReason);
            Equal(10m, rated.ReferencedRate!.Value);
            Equal("2026-08", rated.RateRevision);
            Equal(EstimatingReadinessState.Stale, rated.State);

            Throws<InvalidOperationException>(() => service.ApplyManualRateOverride(
                portfolio,
                "S2",
                12m,
                "Approved adjustment",
                audit,
                "w1-smoke",
                "override-1",
                new DateTime(2026, 8, 21, 0, 1, 0, DateTimeKind.Utc)));
            Equal(0, audit.Events.Count);

            var overrideRejected = portfolio.GetLine("S2");
            Equal(true, overrideRejected.IsStale);
            Equal("Source quantity changed", overrideRejected.StaleReason);
            Equal(null, overrideRejected.OverrideRate);
            Equal(10m, overrideRejected.ReferencedRate!.Value);
            Equal("2026-08", overrideRejected.RateRevision);
            Equal(EstimatingReadinessState.Stale, overrideRejected.State);
        }

        private static void BlockedBulkAssignmentFailsWithoutAuditMutation()
        {
            var service = new EstimatingWorkflowService();
            var audit = new CommercialAuditLog();
            var blocked = new EstimatingLine(
                "B2", "QB2", "RQ2", 4m, "m",
                isBlocked: true,
                blockReason: "Missing commercial approval");
            var portfolio = new EstimatingPortfolio(new[] { blocked });
            var request = new BulkRateAssignmentRequest(
                new[] { "B2" },
                "CC-200",
                "RATEBOOK",
                "2026-08",
                new[] { new UnitRateAssignment("m", 20m) });

            var preview = service.PreviewBulkRateAssignment(portfolio, request);
            Equal(false, preview.CanCommit);
            Equal(1, preview.BlockedLineIds.Count);
            Equal("B2", preview.BlockedLineIds[0]);

            Throws<InvalidOperationException>(() => service.CommitBulkRateAssignment(
                portfolio,
                preview,
                audit,
                "w1-smoke",
                "bulk-blocked",
                new DateTime(2026, 8, 21, 0, 3, 0, DateTimeKind.Utc)));

            Equal(0, audit.Events.Count);
            var unchanged = portfolio.GetLine("B2");
            Equal(true, unchanged.IsBlocked);
            Equal("Missing commercial approval", unchanged.BlockReason);
            Equal(null, unchanged.ReferencedRate);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
