using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialEstimatingWorkflowSmoke
    {
        internal static void Run()
        {
            var service = new EstimatingWorkflowService();
            var audit = new CommercialAuditLog();
            var original = new EstimatingPortfolio(new[]
            {
                new EstimatingLine("L1", "model-a", "Q1", 10m, "m2"),
                new EstimatingLine("L2", "model-a", "Q1", 5m, "m2", "OLD", "rates-old", "R1", 2m),
                new EstimatingLine("L3", "model-a", "Q1", 3m, "m3")
            });

            Equal(EstimatingReadinessState.Unclassified, original.GetLine("L1").State);
            Equal(EstimatingReadinessState.Priced, original.GetLine("L2").State);
            Equal(10m, original.GetLine("L2").Amount.Value);

            var invalidRequest = new BulkRateAssignmentRequest(
                new[] { "L1", "L2", "L3" },
                "CONC",
                "rates-main",
                "R5",
                new[] { new UnitRateAssignment("m2", 4m) });
            var invalidPreview = service.PreviewBulkRateAssignment(original, invalidRequest);
            True(!invalidPreview.CanCommit);
            Equal(3, invalidPreview.AffectedCount);
            Equal(2, invalidPreview.UnitDistribution.Count);
            Equal(1, invalidPreview.UnmatchedLineIds.Count);
            Equal("L3", invalidPreview.UnmatchedLineIds[0]);
            Throws<InvalidOperationException>(() => service.CommitBulkRateAssignment(
                original,
                invalidPreview,
                audit,
                "estimator",
                "bulk-1",
                Utc(2026, 8, 19, 5, 40, 0)));
            Equal(EstimatingReadinessState.Unclassified, original.GetLine("L1").State);
            Equal("OLD", original.GetLine("L2").CostCode);
            Equal(0, audit.Events.Count);

            var validRequest = new BulkRateAssignmentRequest(
                new[] { "L1", "L2", "L3" },
                "CONC",
                "rates-main",
                "R5",
                new[]
                {
                    new UnitRateAssignment("m2", 4m),
                    new UnitRateAssignment("m3", 9m)
                });
            var preview = service.PreviewBulkRateAssignment(original, validRequest);
            True(preview.CanCommit);
            Equal(1, preview.ReplacementCount);
            Equal(10m, preview.TotalBefore);
            Equal(87m, preview.TotalAfter);
            Equal(77m, preview.ValueDelta);

            var priced = service.CommitBulkRateAssignment(
                original,
                preview,
                audit,
                "estimator",
                "bulk-2",
                Utc(2026, 8, 19, 5, 41, 0));
            Equal(EstimatingReadinessState.Priced, priced.GetLine("L1").State);
            Equal("CONC", priced.GetLine("L2").CostCode);
            Equal(4m, priced.GetLine("L2").ReferencedRate.Value);
            Equal(3, audit.Events.Count);
            Equal("R5", audit.Events[0].SourceRevisions[0].RevisionId);

            Throws<ArgumentException>(() => service.ApplyManualRateOverride(
                priced,
                "L1",
                5m,
                " ",
                audit,
                "estimator",
                "override-bad",
                Utc(2026, 8, 19, 5, 42, 0)));

            var overridden = service.ApplyManualRateOverride(
                priced,
                "L1",
                5m,
                "Site-specific access premium",
                audit,
                "estimator",
                "override-1",
                Utc(2026, 8, 19, 5, 43, 0));
            Equal(EstimatingReadinessState.PricedWithOverride, overridden.GetLine("L1").State);
            Equal(4m, overridden.GetLine("L1").ReferencedRate.Value);
            Equal(5m, overridden.GetLine("L1").EffectiveRate.Value);
            Equal(50m, overridden.GetLine("L1").Amount.Value);

            var restored = service.RemoveManualRateOverride(
                overridden,
                "L1",
                "Premium no longer applies",
                audit,
                "estimator",
                "override-2",
                Utc(2026, 8, 19, 5, 44, 0));
            Equal(EstimatingReadinessState.Priced, restored.GetLine("L1").State);
            Equal(4m, restored.GetLine("L1").EffectiveRate.Value);
            Equal(40m, restored.GetLine("L1").Amount.Value);

            var stale = service.MarkQuantitySourceStale(restored, "L1", "Active model is newer than quantity snapshot Q1");
            Equal(EstimatingReadinessState.Stale, stale.GetLine("L1").State);
            Equal(40m, stale.GetLine("L1").Amount.Value);
            Equal("Q1", stale.GetLine("L1").QuantityRevision);
            Equal(5, audit.Events.Count);
        }

        private static DateTime Utc(int year, int month, int day, int hour, int minute, int second)
            => new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("Commercial estimating smoke assertion failed.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException("Commercial estimating smoke expected '" + expected + "' but got '" + actual + "'.");
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
            throw new InvalidOperationException("Commercial estimating smoke expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class CommercialEstimatingWorkflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CommercialEstimatingWorkflowSmoke.Run();
    }
}
