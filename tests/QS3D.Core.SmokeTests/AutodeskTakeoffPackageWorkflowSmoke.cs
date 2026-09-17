using System;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class AutodeskTakeoffPackageWorkflowSmoke
    {
        internal static void Run()
        {
            PackagePreservesEvidenceAndBuildsDeterministicEstimate();
            PackageRefusesMultipleCurrentRevisionsForSameSheet();
        }

        private static void PackagePreservesEvidenceAndBuildsDeterministicEstimate()
        {
            var calibration = new DrawingCalibration(2d, "m");
            var sheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "plan.pdf", "R2", calibration);
            var takeoff = new CalibratedTakeoffEngine2D().Extract(sheet, new[]
            {
                new TakeoffMarkup2D("m2", "A101", TakeoffMeasurementKind.Area, 3d, "Floor", "L1", "TAKEOFF", "h2"),
                new TakeoffMarkup2D("m1", "A101", TakeoffMeasurementKind.Length, 5d, "Wall", "L1", "A-WALL", "h1")
            });
            var sut = new AutodeskTakeoffPackageWorkflow();
            var result = sut.Build("PKG-01", "g7", new[] { new AutodeskTakeoffPackageMember(FakeIngested(sheet), takeoff) }, Array.Empty<IfcQtoItem>(), (c, q) => q * 1.1d, (c, u) => c == "Wall" ? 20d : 10d);
            Require(result.Id == "PKG-01", "Package id must be preserved.");
            Require(result.Inventory.Count == 2, "Package inventory must contain both classifications.");
            Require(result.Inventory.Select(x => x.Classification).SequenceEqual(new[] { "Floor", "Wall" }), "Inventory classification ordering must be deterministic.");
            Require(takeoff.Evidence.Single(x => x.MarkupId == "m1").Layer == "A-WALL", "Layer evidence must be preserved.");
            Require(takeoff.Evidence.Single(x => x.MarkupId == "m1").Zone == "L1", "Zone evidence must be preserved.");
            sut.RequireCurrentGeneration(result, "g7");
            RequireThrows<InvalidOperationException>(() => sut.RequireCurrentGeneration(result, "g8"), "Stale package generation must be refused.");
        }

        private static void PackageRefusesMultipleCurrentRevisionsForSameSheet()
        {
            var calibration = new DrawingCalibration(1d, "m");
            var engine = new CalibratedTakeoffEngine2D();
            var oldSheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "old.pdf", "R1", calibration);
            var newSheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "new.pdf", "R2", calibration);
            var oldResult = engine.Extract(oldSheet, new[] { new TakeoffMarkup2D("m1", "A101", TakeoffMeasurementKind.Count, 1d, "Door", "L1", "A-DOOR", "old") });
            var newResult = engine.Extract(newSheet, new[] { new TakeoffMarkup2D("m1", "A101", TakeoffMeasurementKind.Count, 2d, "Door", "L1", "A-DOOR", "new") });
            var sut = new AutodeskTakeoffPackageWorkflow();
            Require(sut.CompareRevision(oldResult, newResult).Overlay.Count == 1, "Revision compare must expose one changed markup.");
            RequireThrows<InvalidOperationException>(() => sut.Build("PKG", "g2", new[] { new AutodeskTakeoffPackageMember(FakeIngested(oldSheet), oldResult), new AutodeskTakeoffPackageMember(FakeIngested(newSheet), newResult) }, Array.Empty<IfcQtoItem>(), (c, q) => q, (c, u) => 1d), "Package must refuse multiple current revisions for one logical sheet.");
        }

        private static IngestedDrawingSheet2D FakeIngested(DrawingSheet2D sheet)
        {
            var ctor = typeof(IngestedDrawingSheet2D).GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single();
            return (IngestedDrawingSheet2D)ctor.Invoke(new object[] { sheet, new string('a', 64), 100, 1, null, 0, 0 });
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void RequireThrows<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(message);
        }
    }
}
