using System;
using System.Linq;
using QS3D.Core.BenchmarkParity;
using Xunit;

namespace QS3D.Core.SmokeTests
{
    public sealed class AutodeskTakeoffPackageWorkflowSmoke
    {
        [Fact]
        public void Package_preserves_evidence_and_builds_deterministic_estimate()
        {
            var calibration = new DrawingCalibration(2d, "m");
            var sheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "plan.pdf", "R2", calibration);
            var takeoff = new CalibratedTakeoffEngine2D().Extract(sheet, new[]
            {
                new TakeoffMarkup2D("m2", "A101", TakeoffMeasurementKind.Area, 3d, "Floor", "L1", "TAKEOFF", "h2"),
                new TakeoffMarkup2D("m1", "A101", TakeoffMeasurementKind.Length, 5d, "Wall", "L1", "A-WALL", "h1")
            });
            var ingested = FakeIngested(sheet);
            var sut = new AutodeskTakeoffPackageWorkflow();

            var result = sut.Build("PKG-01", "g7", new[] { new AutodeskTakeoffPackageMember(ingested, takeoff) }, Array.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity * 1.1d,
                (classification, unit) => classification == "Wall" ? 20d : 10d);

            Assert.Equal("PKG-01", result.Id);
            Assert.Equal(2, result.Inventory.Count);
            Assert.Equal(new[] { "Floor", "Wall" }, result.Inventory.Select(x => x.Classification).ToArray());
            Assert.Equal("A-WALL", takeoff.Evidence.Single(x => x.MarkupId == "m1").Layer);
            Assert.Equal("L1", takeoff.Evidence.Single(x => x.MarkupId == "m1").Zone);
            sut.RequireCurrentGeneration(result, "g7");
            Assert.Throws<InvalidOperationException>(() => sut.RequireCurrentGeneration(result, "g8"));
        }

        [Fact]
        public void Package_refuses_multiple_current_revisions_for_same_sheet()
        {
            var calibration = new DrawingCalibration(1d, "m");
            var engine = new CalibratedTakeoffEngine2D();
            var oldSheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "old.pdf", "R1", calibration);
            var newSheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "new.pdf", "R2", calibration);
            var oldResult = engine.Extract(oldSheet, new[] { new TakeoffMarkup2D("m1", "A101", TakeoffMeasurementKind.Count, 1d, "Door", "L1", "A-DOOR", "old") });
            var newResult = engine.Extract(newSheet, new[] { new TakeoffMarkup2D("m1", "A101", TakeoffMeasurementKind.Count, 2d, "Door", "L1", "A-DOOR", "new") });
            var sut = new AutodeskTakeoffPackageWorkflow();

            Assert.Single(sut.CompareRevision(oldResult, newResult).Overlay);
            Assert.Throws<InvalidOperationException>(() => sut.Build("PKG", "g2", new[]
            {
                new AutodeskTakeoffPackageMember(FakeIngested(oldSheet), oldResult),
                new AutodeskTakeoffPackageMember(FakeIngested(newSheet), newResult)
            }, Array.Empty<IfcQtoItem>(), (c, q) => q, (c, u) => 1d));
        }

        private static IngestedDrawingSheet2D FakeIngested(DrawingSheet2D sheet)
        {
            var ctor = typeof(IngestedDrawingSheet2D).GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single();
            return (IngestedDrawingSheet2D)ctor.Invoke(new object[] { sheet, new string('a', 64), 100, 1, null, 0, 0 });
        }
    }
}
