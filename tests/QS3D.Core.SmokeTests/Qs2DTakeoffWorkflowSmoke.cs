using System;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DTakeoffWorkflowSmoke
    {
        internal static void Run()
        {
            var calibration = new DrawingCalibration(100d, 5d, "m");
            var oldSheet = new DrawingSheet2D("A101", "Ground Floor", DrawingSheetSourceKind.Pdf, "drawings/A101-r1.pdf", "R1", calibration);
            var newSheet = new DrawingSheet2D("A101", "Ground Floor", DrawingSheetSourceKind.Pdf, "drawings/A101-r2.pdf", "R2", calibration);
            var engine = new CalibratedTakeoffEngine2D();

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

            var workflow = new AutodeskTakeoffWorkflow();
            var inventory = workflow.BuildInventoryAndEstimate(
                newResult.Evidence,
                new[] { new IfcQtoItem("ifc-1", "IfcWall", "ZONE-A", "WALL", "Length", 7.5d, "m") },
                (classification, quantity) => classification == "WALL" ? quantity * 1.1d : quantity,
                (classification, unit) => classification == "WALL" && unit == "m" ? 100d : 10d);

            var wall = inventory.Single(x => x.Classification == "WALL" && x.Zone == "ZONE-A" && x.Unit == "m");
            Expect(Math.Abs(wall.MeasuredQuantity - 10d) < 1e-12, "drawing+BIM inventory aggregation");
            Expect(Math.Abs(wall.FormulaQuantity - 11d) < 1e-12, "formula stage");
            Expect(Math.Abs(wall.EstimatedCost - 1100d) < 1e-12, "estimate stage");
            Expect(wall.EvidenceCount == 2, "drawing+BIM evidence count");
        }

        private static void Expect(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("2D takeoff workflow smoke failed: " + name + ".");
        }
    }
}
