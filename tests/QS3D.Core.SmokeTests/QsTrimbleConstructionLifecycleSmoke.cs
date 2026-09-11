using System;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsTrimbleConstructionLifecycleSmoke
    {
        internal static void Run()
        {
            var suppliers = new[]
            {
                new ConstructionSupplier("S1", "Supplier A", SupplierLifecycleStatus.Approved),
                new ConstructionSupplier("S2", "Supplier B", SupplierLifecycleStatus.Approved)
            };
            var commitments = new[]
            {
                new SubcontractCommitment("SC1", "S1", "PKG-A", 100m, 10m),
                new SubcontractCommitment("SC2", "S2", "PKG-B", 200m, 0m)
            };
            var orders = new[]
            {
                new PurchaseOrderDelivery("PO1", "S1", "PKG-A", 80m, 60m, DeliveryStatus.PartiallyDelivered),
                new PurchaseOrderDelivery("PO2", "S2", "PKG-B", 150m, 150m, DeliveryStatus.Delivered)
            };
            var progress = new[]
            {
                new FieldProgressRecord("PKG-A", 0.6d, 0.5d, 55m),
                new FieldProgressRecord("PKG-B", 0.4d, 0.45d, 120m)
            };

            var controls = new TrimbleConstructionLifecycleEngine().BuildProjectControls(suppliers, commitments, orders, progress);
            Equal(2, controls.Count, "package controls");
            var packageA = controls.Single(x => x.PackageId == "PKG-A");
            Equal(110m, packageA.Commitment, "current commitment");
            Equal(80m, packageA.Ordered, "ordered value");
            Equal(60m, packageA.Delivered, "delivered value");
            Equal(55m, packageA.ActualCost, "actual cost");
            Near(-0.1d, packageA.ScheduleVariance, 1e-12, "schedule variance");
            Equal(55m, packageA.CommitmentRemaining, "commitment remaining");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}