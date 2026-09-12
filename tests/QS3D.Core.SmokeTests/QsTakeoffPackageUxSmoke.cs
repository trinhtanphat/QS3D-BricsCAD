using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            PreservesHighDynamicRangePackageQuantityDelta();
            AdmitsPdfAndRasterSheetPayloads();
            RejectsMalformedSheetPayloads();
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
            Near(4d, comparison.QuantityDelta, 1e-12, "revision quantity delta");
            True(comparison.RequiresReview, "revision review required");
        }

        private static void PreservesHighDynamicRangePackageQuantityDelta()
        {
            var oldPackage = new TakeoffPackageDefinition("PKG-D", "Architecture", "R1", "Uniclass", "Default");
            var newPackage = new TakeoffPackageDefinition("PKG-D", "Architecture", "R2", "Uniclass", "Default");
            TakeoffSheetResult2D Build(string id, string revision, double count)
            {
                var sheet = new DrawingSheet2D(id, "Plan", DrawingSheetSourceKind.Pdf, id + ".pdf", revision, new DrawingCalibration(1d, 1d, "m"));
                return new CalibratedTakeoffEngine2D().Extract(sheet, new[] { new TakeoffMarkup2D("M-" + id, id, TakeoffMeasurementKind.Count, count, "ARC.ITEM", "L01", "A-ITEM", "H-" + id) });
            }
            var comparison = new AutodeskTakeoffPackageRevisionComparer().Compare(oldPackage, new[] { Build("C", "R1", 1e16) }, newPackage, new[] { Build("A", "R2", 1e16), Build("B", "R2", 1d) });
            Near(1d, comparison.QuantityDelta, 0d, "high dynamic range revision quantity delta");
        }

        private static void AdmitsPdfAndRasterSheetPayloads()
        {
            var ingestor = new Qs2DSheetIngestor();
            var calibration = new DrawingCalibration(100d, 10d, "m");
            var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\nfixture-a");
            var pdf2 = Encoding.ASCII.GetBytes("%PDF-1.7\nfixture-b");
            var first = ingestor.IngestPdf("A401", "Plan", "A401.pdf", "R3", calibration, pdf, 2);
            var second = ingestor.IngestPdf("A401", "Plan", "A401.pdf", "R3", calibration, pdf2, 2);
            Equal(DrawingSheetSourceKind.Pdf, first.Sheet.SourceKind, "pdf source kind");
            Equal(2, first.PdfPageNumber, "pdf page number");
            Equal(64, first.SourceSha256.Length, "pdf fingerprint length");
            True(!string.Equals(first.SourceSha256, second.SourceSha256, StringComparison.Ordinal), "content drift changes fingerprint");

            var png = new byte[24] { 137,80,78,71,13,10,26,10, 0,0,0,13, 73,72,68,82, 0,0,0,32, 0,0,0,16 };
            var raster = ingestor.IngestRaster("A402", "Detail", "A402.png", "R1", calibration, png);
            Equal((RasterSheetFormat?)RasterSheetFormat.Png, raster.RasterFormat, "png format");
            Equal(32, raster.PixelWidth, "png width");
            Equal(16, raster.PixelHeight, "png height");

            var jpeg = new byte[] { 0xFF,0xD8,0xFF,0xC0,0x00,0x11,0x08,0x00,0x10,0x00,0x20,0x03,0x01,0x11,0x00,0x02,0x11,0x00,0x03,0x11,0x00 };
            var photo = ingestor.IngestRaster("A403", "Photo", "A403.jpg", "R1", calibration, jpeg);
            Equal((RasterSheetFormat?)RasterSheetFormat.Jpeg, photo.RasterFormat, "jpeg format");
            Equal(32, photo.PixelWidth, "jpeg width");
            Equal(16, photo.PixelHeight, "jpeg height");

            var evidence = new CalibratedTakeoffEngine2D().Extract(first.Sheet, new[]
            {
                new TakeoffMarkup2D("M-PDF", "A401", TakeoffMeasurementKind.Length, 100d, "ARC.WALL", "L03", "A-WALL", "PDF-M1")
            });
            Near(10d, evidence.Evidence[0].Quantity, 1e-12, "ingested pdf calibrated quantity");
            Equal("A401.pdf", evidence.Evidence[0].SourceReference, "ingested pdf evidence source");
        }

        private static void RejectsMalformedSheetPayloads()
        {
            var ingestor = new Qs2DSheetIngestor();
            var calibration = new DrawingCalibration(1d, 1d, "m");
            Throws<ArgumentException>(() => ingestor.IngestPdf("A", "Plan", "a.pdf", "R1", calibration, new byte[0], 1), "empty pdf");
            Throws<ArgumentOutOfRangeException>(() => ingestor.IngestPdf("A", "Plan", "a.pdf", "R1", calibration, Encoding.ASCII.GetBytes("%PDF-1.7"), 0), "invalid page");
            Throws<InvalidOperationException>(() => ingestor.IngestPdf("A", "Plan", "a.pdf", "R1", calibration, Encoding.ASCII.GetBytes("not-pdf"), 1), "bad pdf signature");
            Throws<InvalidOperationException>(() => ingestor.IngestRaster("A", "Plan", "a.png", "R1", calibration, Encoding.ASCII.GetBytes("not-image")), "bad raster signature");
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

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            catch (Exception ex) { throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ", actual " + ex.GetType().Name + "."); }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }
    }
}
