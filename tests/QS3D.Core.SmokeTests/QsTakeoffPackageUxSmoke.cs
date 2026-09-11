using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsTakeoffPackageUxSmoke
    {
        internal static void Run()
        {
            BlocksStaleDrawingRevision();
            BuildsReadyMixedSourcePackage();
            ComparesPackageDrawingRevisions();
        }

        private static void BlocksStaleDrawingRevision()
        {
            var package = new TakeoffPackageDefinition("PKG-A", "Architecture", "R2", "Uniclass", "Default");
            var sheet = new DrawingSheet2D("A101", "GA", DrawingSheetSourceKind.Pdf, "A101.pdf", "R1", new DrawingCalibration(100d, 10d, "m"));
            var evidence = new CalibratedTakeoffEngine2D().Extract(sheet, new[]
            {
                new TakeoffMarkup2D("M1", "A101", TakeoffMeasurementKind.Length, 100d, "ARC.WALL", "L01", "A-WALL", "H1")
            }).Evidence;

            var result = new AutodeskTakeoffPackageCoordinator().Build(
                package,
                new[] { sheet },
                evidence,
                Enumerable.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 1d);

            Equal(TakeoffPackageReadiness.Blocked, result.Readiness, "stale package readiness");
            True(result.Issues.Any(x => x.Code == "PKG.STALE_DRAWING_REVISION"), "stale sheet issue");
            True(!result.CanEstimate, "stale package cannot estimate");
        }

        private static void BuildsReadyMixedSourcePackage()
        {
            var package = new TakeoffPackageDefinition("PKG-B", "Tender Package", "R2", "Uniclass", "Waste5Pct");
            var sheet = new DrawingSheet2D("A201", "Floor Plan", DrawingSheetSourceKind.Pdf, "A201-R2.pdf", "R2", new DrawingCalibration(100d, 10d, "m"));
            var evidence = new CalibratedTakeoffEngine2D().Extract(sheet, new[]
            {
                new TakeoffMarkup2D("M2", "A201", TakeoffMeasurementKind.Length, 200d, "ARC.WALL", "L02", "A-WALL", "H2")
            }).Evidence;
            var bim = new[]
            {
                new IfcQtoItem("G1", "IfcWall", "L02", "ARC.WALL", "Length", 5d, "m")
            };

            var result = new AutodeskTakeoffPackageCoordinator().Build(
                package,
                new[] { sheet },
                evidence,
                bim,
                (classification, quantity) => quantity * 1.05d,
                (classification, unit) => 100d);

            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "mixed package readiness");
            Equal(2, result.Sources.Count, "mixed package source count");
            Equal(1, result.Inventory.Count, "mixed package inventory count");
            Near(26.25d, result.Inventory[0].FormulaQuantity, 1e-12, "mixed package formula quantity");
            Near(2625d, result.EstimatedCost, 1e-12, "mixed package estimated cost");
            True(result.CanEstimate, "mixed package can estimate");
        }

        private static void ComparesPackageDrawingRevisions()
        {
            var oldPackage = new TakeoffPackageDefinition("PKG-C", "Architecture", "R1", "Uniclass", "Default");
            var newPackage = new TakeoffPackageDefinition("PKG-C", "Architecture", "R2", "Uniclass", "Default");
            var oldSheet = new DrawingSheet2D("A301", "Plan", DrawingSheetSourceKind.Pdf, "A301-R1.pdf", "R1", new DrawingCalibration(100d, 10d, "m"));
            var newSheet = new DrawingSheet2D("A301", "Plan", DrawingSheetSourceKind.Pdf, "A301-R2.pdf", "R2", new DrawingCalibration(100d, 10d, "m"));
            var oldResult = new CalibratedTakeoffEngine2D().Extract(oldSheet, new[]
            {
                new TakeoffMarkup2D("M1", "A301", TakeoffMeasurementKind.Length, 100d, "ARC.WALL", "L01", "A-WALL", "H1"),
                new TakeoffMarkup2D("M2", "A301", TakeoffMeasurementKind.Count, 2d, "ARC.DOOR", "L01", "A-DOOR", "H2")
            });
            var newResult = new CalibratedTakeoffEngine2D().Extract(newSheet, new[]
            {
                new TakeoffMarkup2D("M1", "A301", TakeoffMeasurementKind.Length, 150d, "ARC.WALL", "L01", "A-WALL", "H1"),
                new TakeoffMarkup2D("M3", "A301", TakeoffMeasurementKind.Count, 1d, "ARC.WINDOW", "L01", "A-WIND", "H3")
            });

            var comparison = new AutodeskTakeoffPackageRevisionComparer().Compare(
                oldPackage,
                new[] { oldResult },
                newPackage,
                new[] { newResult });

            Equal(1, comparison.ChangedSheetCount, "revision changed sheet count");
            Equal(1, comparison.AddedMarkupCount, "revision added markup count");
            Equal(1, comparison.RemovedMarkupCount, "revision removed markup count");
            Equal(1, comparison.ChangedMarkupCount, "revision changed markup count");
            Near(499d, comparison.QuantityDelta, 1e-12, "revision quantity delta");
            True(comparison.RequiresReview, "revision review required");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
