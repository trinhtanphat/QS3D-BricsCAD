using System;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class ConstructionLifecycleWorkflowSmoke
    {
        public static void Run()
        {
            var service = new ConstructionLifecycleService();
            var snapshot = service.BuildSnapshot(
                new[] { new VendorRecord("V-1", "Vendor", VendorStatus.Active) },
                new[] { new SubcontractCommitment("SC-1", "V-1", "WBS-01", 1000m, 100m, CommitmentStatus.Active) },
                new[] { new PurchaseOrderDelivery("PO-1", "SC-1", 10m, 4m, 50m) },
                new[] { 300m, 100m },
                new[] { new FieldProgressRecord("WBS-01", 50m, 40m, 440m) });

            Equal(1100m, snapshot.Commitment, "approved changes must roll into current commitment");
            Equal(400m, snapshot.ActualCost, "actual cost must aggregate deterministically");
            Equal(700m, snapshot.ActualVsCommitmentVariance, "variance must remain commitment minus actual");
            Equal(200m, snapshot.DeliveredValue, "partial deliveries must retain delivered value");
            Equal(0.4m, snapshot.ProcurementProgress, "procurement progress must be delivered/ordered value");
            Equal(0.4m, snapshot.FieldProgress, "field progress must normalize percent to ratio");
            Equal(440m, snapshot.EarnedValue, "earned value must aggregate WBS progress");

            var envelope = service.Export("P-1", "R1", "erp:P-1:R1", snapshot);
            if (envelope.IdempotencyKey != "erp:P-1:R1") throw new Exception("ERP envelope must retain idempotency key.");
            Expect<InvalidOperationException>(() => service.Export("P-1", "R1", "erp:P-1:R1", snapshot), "duplicate ERP export must fail closed");
            Expect<InvalidOperationException>(() => service.BuildSnapshot(
                new[] { new VendorRecord("V-2", "Suspended", VendorStatus.Suspended) },
                new[] { new SubcontractCommitment("SC-2", "V-2", "WBS-02", 1m, 0m, CommitmentStatus.Active) },
                Array.Empty<PurchaseOrderDelivery>(), Array.Empty<decimal>(), Array.Empty<FieldProgressRecord>()), "suspended vendor commitment must fail closed");
        }

        private static void Equal(decimal expected, decimal actual, string message) { if (expected != actual) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + "."); }
        private static void Expect<T>(Action action, string message) where T : Exception { try { action(); } catch (T) { return; } throw new Exception(message); }
    }
}
