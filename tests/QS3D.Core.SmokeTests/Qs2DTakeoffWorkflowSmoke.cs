using System;
using System.Linq;
using System.Text;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DTakeoffWorkflowSmoke
    {
        internal static void Run()
        {
            var calibration = new DrawingCalibration(100d, 5d, "m");
            ExpectThrows<ArgumentOutOfRangeException>(() => new DrawingSheet2D(
                "A099", "Invalid source", (DrawingSheetSourceKind)999, "drawings/A099.bin", "R1", calibration),
                "invalid drawing source kind rejection");
            ExpectThrows<ArgumentOutOfRangeException>(() => new TakeoffMarkup2D(
                "M-INVALID", "A099", (TakeoffMeasurementKind)999, 1d, "WALL", "ZONE-A", "Takeoff-Wall", "pdf:M-INVALID"),
                "invalid measurement kind rejection");

            var ingestor = new Qs2DSheetIngestor();
            var validPdf = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<<>>\nendobj\n%%EOF\r\n");
            var ingested = ingestor.IngestPdf("A100", "Valid PDF", "drawings/A100.pdf", "R1", calibration, validPdf, 1);
            Expect(ingested.PdfPageNumber == 1 && ingested.ByteLength == validPdf.Length, "valid PDF ingestion");
            Expect(ingested.SourceSha256.Length == 64, "valid PDF SHA-256 evidence");
            ExpectThrows<InvalidOperationException>(() => ingestor.IngestPdf(
                "A100", "Bad header", "drawings/A100.pdf", "R1", calibration,
                Encoding.ASCII.GetBytes("%PDF-x.y\n%%EOF\n"), 1), "malformed PDF header rejection");
            ExpectThrows<InvalidOperationException>(() => ingestor.IngestPdf(
                "A100", "Truncated", "drawings/A100.pdf", "R1", calibration,
                Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<<>>\nendobj\n"), 1), "truncated PDF rejection");

            var oldSheet = new DrawingSheet2D("A101", "Ground Floor", DrawingSheetSourceKind.Pdf, "drawings/A101-r1.pdf", "R1", calibration);
            var newSheet = new DrawingSheet2D("A101", "Ground Floor", DrawingSheetSourceKind.Pdf, "drawings/A101-r2.pdf", "R2", calibration);
            var engine = new CalibratedTakeoffEngine2D();

            var validEvidence = new TakeoffQuantityEvidence2D("M-AFFINITY", "A101", "R1", "drawings/A101-r1.pdf", "pdf:M-AFFINITY", "WALL", "ZONE-A", "Takeoff-Wall", 1d, "m");
            ExpectThrows<ArgumentException>(() => new TakeoffSheetResult2D(oldSheet, new TakeoffQuantityEvidence2D[] { null! }), "null sheet evidence rejection");
            ExpectThrows<InvalidOperationException>(() => new TakeoffSheetResult2D(oldSheet, new[]
            {
                new TakeoffQuantityEvidence2D("M-OTHER-SHEET", "A102", "R1", "drawings/A101-r1.pdf", "pdf:M-OTHER-SHEET", "WALL", "ZONE-A", "Takeoff-Wall", 1d, "m")
            }), "cross-sheet evidence rejection");
            ExpectThrows<InvalidOperationException>(() => new TakeoffSheetResult2D(oldSheet, new[]
            {
                new TakeoffQuantityEvidence2D("M-STALE-REV", "A101", "R0", "drawings/A101-r1.pdf", "pdf:M-STALE-REV", "WALL", "ZONE-A", "Takeoff-Wall", 1d, "m")
            }), "stale-revision evidence rejection");
            ExpectThrows<InvalidOperationException>(() => new TakeoffSheetResult2D(oldSheet, new[]
            {
                new TakeoffQuantityEvidence2D("M-STALE-SOURCE", "A101", "R1", "drawings/A101-old.pdf", "pdf:M-STALE-SOURCE", "WALL", "ZONE-A", "Takeoff-Wall", 1d, "m")
            }), "stale-source evidence rejection");
            ExpectThrows<InvalidOperationException>(() => new TakeoffSheetResult2D(oldSheet, new[] { validEvidence, validEvidence }), "duplicate markup evidence rejection");

            var oldResult = engine.Extract(oldSheet, new[]
            {
                new TakeoffMarkup2D("M1", "A101", TakeoffMeasurementKind.Length, 40d, "WALL", "ZONE-A", "Takeoff-Wall", "pdf:M1"),
                new TakeoffMarkup2D("M2", "A101", TakeoffMeasurementKind.Area, 200d, "FLOOR", "ZONE-A", "Takeoff-Floor", "pdf:M2")
            });
            var newResult = engine.Extract(newSheet, new[]
            {
                new TakeoffMarkup2D("M1", "A101", TakeoffMeasurementKind.Length, 50d, "WALL", "ZONE-A", "Takeoff-Wall", "pdf:M1"),
                new TakeoffMarkup2D("M3", "A101", TakeoffMeasurementKind.Count, 4d, "DOOR", "ZONE-A", "Takeoff-Door", "pdf:M3")
            });

            Expect(Math.Abs(oldResult.Evidence.Single(x => x.MarkupId == "M1").Quantity - 2d) < 1e-12, "length calibration");
            Expect(Math.Abs(oldResult.Evidence.Single(x => x.MarkupId == "M2").Quantity - 0.5d) < 1e-12, "area calibration");
            Expect(oldResult.Evidence.All(x => x.SourceReference == "drawings/A101-r1.pdf" && x.Revision == "R1"), "source evidence");
            Expect(oldResult.Evidence.Single(x => x.MarkupId == "M1").Zone == "ZONE-A", "zone evidence");
            Expect(oldResult.Evidence.Single(x => x.MarkupId == "M1").Layer == "Takeoff-Wall", "layer evidence");

            var deltas = new DrawingRevisionComparer2D().Compare(oldResult, newResult);
            Expect(deltas.Single(x => x.MarkupId == "M1").Kind == RevisionMarkupChangeKind.Changed, "changed markup");
            Expect(Math.Abs(deltas.Single(x => x.MarkupId == "M1").QuantityDelta - 0.5d) < 1e-12, "changed quantity delta");
            Expect(deltas.Single(x => x.MarkupId == "M2").Kind == RevisionMarkupChangeKind.Removed, "removed markup");
            Expect(deltas.Single(x => x.MarkupId == "M3").Kind == RevisionMarkupChangeKind.Added, "added markup");

            var extremeOld = new TakeoffSheetResult2D(oldSheet, new[]
            {
                new TakeoffQuantityEvidence2D("M-EXTREME", "A101", "R1", "drawings/A101-r1.pdf", "pdf:M-EXTREME", "WALL", "ZONE-A", "Takeoff-Wall", double.MaxValue, "m")
            });
            var extremeNew = new TakeoffSheetResult2D(newSheet, new[]
            {
                new TakeoffQuantityEvidence2D("M-EXTREME", "A101", "R2", "drawings/A101-r2.pdf", "pdf:M-EXTREME", "WALL", "ZONE-A", "Takeoff-Wall", 0d, "m")
            });
            var extremeDelta = new DrawingRevisionComparer2D().Compare(extremeOld, extremeNew).Single(x => x.MarkupId == "M-EXTREME");
            Expect(extremeDelta.Kind == RevisionMarkupChangeKind.Changed, "extreme changed markup");
            Expect(extremeDelta.QuantityDelta == -double.MaxValue, "finite extreme negative revision quantity delta");

            var workflow = new AutodeskTakeoffWorkflow();
            var inventory = workflow.BuildInventoryAndEstimate(
                newResult.Evidence,
                new[] { new IfcQtoItem("ifc-1", "IfcWall", "ZONE-A", "WALL", "Length", 7.5d, "m") },
                (classification, quantity) => classification == "WALL" ? quantity * 1.1d : quantity,
                (classification, unit) => classification == "WALL" && unit == "m" ? 100d : 10d);

            var wallRows = inventory.Where(x => x.Classification == "WALL" && x.Zone == "ZONE-A" && x.Unit == "m").ToList();
            Expect(wallRows.Count == 2, "drawing and BIM rows remain layer-segmented");
            var drawingWall = wallRows.Single(x => x.Layer == "Takeoff-Wall");
            var bimWall = wallRows.Single(x => x.Layer == string.Empty);
            Expect(Math.Abs(drawingWall.MeasuredQuantity - 2.5d) < 1e-12, "drawing layer measured quantity");
            Expect(Math.Abs(drawingWall.FormulaQuantity - 2.75d) < 1e-12, "drawing layer formula stage");
            Expect(Math.Abs(drawingWall.EstimatedCost - 275d) < 1e-12, "drawing layer estimate stage");
            Expect(drawingWall.EvidenceCount == 1, "drawing layer evidence count");
            Expect(Math.Abs(bimWall.MeasuredQuantity - 7.5d) < 1e-12, "BIM empty-layer measured quantity");
            Expect(Math.Abs(bimWall.FormulaQuantity - 8.25d) < 1e-12, "BIM empty-layer formula stage");
            Expect(Math.Abs(bimWall.EstimatedCost - 825d) < 1e-12, "BIM empty-layer estimate stage");
            Expect(bimWall.EvidenceCount == 1, "BIM empty-layer evidence count");
            Expect(Math.Abs(wallRows.Sum(x => x.MeasuredQuantity) - 10d) < 1e-12, "segmentation preserves total measured quantity");
            Expect(Math.Abs(wallRows.Sum(x => x.EstimatedCost) - 1100d) < 1e-12, "segmentation preserves total estimate");

            var precisionInventory = workflow.BuildInventoryAndEstimate(
                new[]
                {
                    new TakeoffQuantityEvidence2D("M-LARGE", "A900", "R1", "drawings/A900.pdf", "pdf:M-LARGE", "PRECISION", "ZONE-P", "Takeoff", 1e16d, "m")
                },
                new[]
                {
                    new IfcQtoItem("ifc-small-1", "IfcWall", "ZONE-P", "PRECISION", "Length", 1d, "m"),
                    new IfcQtoItem("ifc-small-2", "IfcWall", "ZONE-P", "PRECISION", "Length", 1d, "m")
                },
                (classification, quantity) => quantity,
                (classification, unit) => 2d);
            var precisionDrawing = precisionInventory.Single(x => x.Classification == "PRECISION" && x.Zone == "ZONE-P" && x.Layer == "Takeoff" && x.Unit == "m");
            var precisionBim = precisionInventory.Single(x => x.Classification == "PRECISION" && x.Zone == "ZONE-P" && x.Layer == string.Empty && x.Unit == "m");
            Expect(precisionDrawing.MeasuredQuantity == 1e16d, "precision drawing layer quantity");
            Expect(precisionDrawing.FormulaQuantity == 1e16d, "precision drawing formula quantity");
            Expect(precisionDrawing.EstimatedCost == 2e16d, "precision drawing estimate");
            Expect(precisionDrawing.EvidenceCount == 1, "precision drawing evidence count");
            Expect(precisionBim.MeasuredQuantity == 2d, "compensated BIM empty-layer aggregation");
            Expect(precisionBim.FormulaQuantity == 2d, "formula receives compensated BIM quantity");
            Expect(precisionBim.EstimatedCost == 4d, "estimate uses compensated BIM quantity");
            Expect(precisionBim.EvidenceCount == 2, "precision BIM evidence count");
            Expect(precisionDrawing.MeasuredQuantity + precisionBim.MeasuredQuantity == 10000000000000002d, "layer segmentation preserves high-dynamic-range total quantity");
            Expect(precisionDrawing.EstimatedCost + precisionBim.EstimatedCost == 20000000000000004d, "layer segmentation preserves high-dynamic-range total estimate");

            var ordered = workflow.BuildInventoryAndEstimate(
                new[]
                {
                    new TakeoffQuantityEvidence2D("M-U2", "A901", "R1", "drawings/A901.pdf", "pdf:M-U2", "ORDER", "ZONE-O", "Takeoff", 1d, "m2"),
                    new TakeoffQuantityEvidence2D("M-U1", "A901", "R1", "drawings/A901.pdf", "pdf:M-U1", "ORDER", "ZONE-O", "Takeoff", 1d, "m")
                },
                Array.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 1d);
            Expect(ordered.Count == 2 && ordered[0].Unit == "m" && ordered[1].Unit == "m2", "deterministic unit ordering");

            ExpectThrows<ArgumentException>(() => workflow.BuildInventoryAndEstimate(
                new TakeoffQuantityEvidence2D[] { null! },
                Array.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 1d), "null drawing evidence rejection");
            ExpectThrows<ArgumentException>(() => workflow.BuildInventoryAndEstimate(
                Array.Empty<TakeoffQuantityEvidence2D>(),
                new IfcQtoItem[] { null! },
                (classification, quantity) => quantity,
                (classification, unit) => 1d), "null BIM quantity rejection");
        }

        private static void ExpectThrows<T>(Action action, string name) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("2D takeoff workflow smoke failed: " + name + ".");
        }

        private static void Expect(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("2D takeoff workflow smoke failed: " + name + ".");
        }
    }
}
