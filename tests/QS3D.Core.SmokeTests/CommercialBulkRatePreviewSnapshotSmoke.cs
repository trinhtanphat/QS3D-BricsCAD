using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialBulkRatePreviewSnapshotSmoke
    {
        internal static void Run()
        {
            var service = new EstimatingWorkflowService();
            var request = new BulkRateAssignmentRequest(
                new[] { "L1" },
                "CONC",
                "rates-main",
                "R7",
                new[] { new UnitRateAssignment("m2", 4m) });

            var original = new EstimatingPortfolio(new[]
            {
                new EstimatingLine("L1", "model-a", "Q1", 10m, "m2")
            });
            var preview = service.PreviewBulkRateAssignment(original, request);
            Equal(0m, preview.TotalBefore);
            Equal(40m, preview.TotalAfter);

            var revisionChanged = new EstimatingPortfolio(new[]
            {
                new EstimatingLine("L1", "model-a", "Q2", 10m, "m2")
            });
            var staleAudit = new CommercialAuditLog();
            Throws<InvalidOperationException>(() => service.CommitBulkRateAssignment(
                revisionChanged,
                preview,
                staleAudit,
                "estimator",
                "snapshot-stale",
                Utc(2026, 8, 19, 7, 15, 0)));
            Equal(0, staleAudit.Events.Count);
            Equal("Q2", revisionChanged.GetLine("L1").QuantityRevision);
            Equal(string.Empty, revisionChanged.GetLine("L1").CostCode);

            var equivalent = new EstimatingPortfolio(new[]
            {
                new EstimatingLine("L1", "model-a", "Q1", 10m, "m2")
            });
            var validAudit = new CommercialAuditLog();
            var committed = service.CommitBulkRateAssignment(
                equivalent,
                preview,
                validAudit,
                "estimator",
                "snapshot-valid",
                Utc(2026, 8, 19, 7, 16, 0));
            Equal("Q1", committed.GetLine("L1").QuantityRevision);
            Equal("CONC", committed.GetLine("L1").CostCode);
            Equal(1, validAudit.Events.Count);
        }

        private static DateTime Utc(int year, int month, int day, int hour, int minute, int second)
            => new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException("Commercial preview snapshot smoke expected '" + expected + "' but got '" + actual + "'.");
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
            throw new InvalidOperationException("Commercial preview snapshot smoke expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class CommercialBulkRatePreviewSnapshotRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CommercialBulkRatePreviewSnapshotSmoke.Run();
    }
}
