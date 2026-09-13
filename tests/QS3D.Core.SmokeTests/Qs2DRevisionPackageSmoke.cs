using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DRevisionPackageSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            UsesOnlyCurrentRevisionEvidenceForEstimate();
        }

        private static void UsesOnlyCurrentRevisionEvidenceForEstimate()
        {
            var calibration = new DrawingCalibration(1d, 1d, "m");
            var oldSheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "A101-R1.pdf", "R1", calibration);
            var newSheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "A101-R2.pdf", "R2", calibration);
            var engine = new CalibratedTakeoffEngine2D();

            var previous = engine.Extract(oldSheet, new[]
            {
                new TakeoffMarkup2D("M-KEEP", "A101", TakeoffMeasurementKind.Count, 1d, "ARC.KEEP", "L01", "A-ANNO", "H-KEEP"),
                new TakeoffMarkup2D("M-REMOVED", "A101", TakeoffMeasurementKind.Length, 5d, "ARC.REMOVED", "L01", "A-WALL", "H-REMOVED")
            });
            var current = engine.Extract(newSheet, new[]
            {
                new TakeoffMarkup2D("M-KEEP", "A101", TakeoffMeasurementKind.Count, 2d, "ARC.KEEP", "L01", "A-ANNO", "H-KEEP"),
                new TakeoffMarkup2D("M-ADDED", "A101", TakeoffMeasurementKind.Area, 3d, "ARC.ADDED", "L01", "A-FLOR", "H-ADDED")
            });

            var package = new RevisionTakeoffPackage2D(previous, current);
            Equal(1, package.AddedCount, "added count");
            Equal(1, package.RemovedCount, "removed count");
            Equal(1, package.ChangedCount, "changed count");
            Equal(0, package.UnchangedCount, "unchanged count");
            Equal("M-ADDED", package.CurrentEvidence[0].MarkupId, "deterministic current evidence order");
            Equal("M-KEEP", package.CurrentEvidence[1].MarkupId, "deterministic current evidence order 2");

            var inventory = package.BuildInventoryAndEstimate(
                Array.Empty<IfcQtoItem>(),
                (classification, measured) => measured,
                (classification, unit) => 10d);

            Equal(2, inventory.Count, "current-only inventory row count");
            if (inventory.Any(x => string.Equals(x.Classification, "ARC.REMOVED", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Removed previous-revision evidence leaked into Inventory/Estimate.");
            Near(2d, inventory.Single(x => x.Classification == "ARC.KEEP").MeasuredQuantity, "changed current quantity");
            Near(3d, inventory.Single(x => x.Classification == "ARC.ADDED").MeasuredQuantity, "added current quantity");
            Near(50d, inventory.Sum(x => x.EstimatedCost), "current-only estimate total");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}