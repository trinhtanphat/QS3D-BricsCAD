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
                new PurchaseOrderDelivery("PO-CANCELLED", "S1", "PKG-A", 999m, 999m, DeliveryStatus.Cancelled),
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
            Equal(80m, packageA.Ordered, "ordered value excludes cancelled PO");
            Equal(60m, packageA.Delivered, "delivered value excludes cancelled PO");
            Equal(55m, packageA.ActualCost, "actual cost");
            Near(-0.1d, packageA.ScheduleVariance, 1e-12, "schedule variance");
            Equal(55m, packageA.CommitmentRemaining, "commitment remaining");

            var interop = new TrimbleProjectControlsInterop();
            var exportRows = interop.BuildRows(controls.Reverse(), "REV-2026-09-12");
            Equal(2, exportRows.Count, "ERP export row count");
            Equal("PKG-A", exportRows[0].PackageId, "deterministic package ordering");
            Equal(TrimbleProjectControlsInterop.CurrentSchemaVersion, exportRows[0].SchemaVersion, "schema version");
            Equal("REV-2026-09-12", exportRows[0].SourceRevision, "source revision");
            Equal(80m / 110m, exportRows[0].OrderedToCommitment, "procurement ordered/commitment");
            Equal(60m / 80m, exportRows[0].DeliveredToOrdered, "procurement delivered/ordered");
            Equal(55m / 110m, exportRows[0].ActualToCommitment, "actual/commitment");
            Equal(55m, exportRows[0].CommitmentVariance, "commitment variance");
            Equal(55m, exportRows[0].CostToComplete, "cost to complete");

            var csv = interop.ToCsv(exportRows.Reverse());
            if (!csv.StartsWith("schemaVersion,sourceRevision,packageId,", StringComparison.Ordinal))
                throw new InvalidOperationException("ERP CSV header is not stable.");
            if (csv.IndexOf("qs3d.trimble.project-controls.v1,REV-2026-09-12,PKG-A,110,80,60,55", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("ERP CSV does not preserve package financial evidence.");
            if (csv.IndexOf("PO-CANCELLED", StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Cancelled PO leaked into project-control export.");

            var failClosed = false;
            try
            {
                interop.BuildRows(new[] { new ProjectControlLine("PKG-Z", 0m, 10m, 0m, 0m, 0d, 0d) }, "REV-Z");
            }
            catch (InvalidOperationException)
            {
                failClosed = true;
            }
            if (!failClosed) throw new InvalidOperationException("ERP export must fail closed when a ratio denominator is zero with non-zero activity.");
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
