using System;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class TakeoffPackageEvidenceSourceAffinitySmoke
    {
        internal static void Run()
        {
            var package = new TakeoffPackageDefinition("PKG-SOURCE", "Architecture", "R2", "Uniclass", "Default");
            var previousSheet = new DrawingSheet2D("A501", "Plan", DrawingSheetSourceKind.Pdf, "A501-old.pdf", "R2", new DrawingCalibration(100d, 10d, "m"));
            var admittedSheet = new DrawingSheet2D("A501", "Plan", DrawingSheetSourceKind.Pdf, "A501-new.pdf", "R2", new DrawingCalibration(100d, 10d, "m"));
            var staleEvidence = new CalibratedTakeoffEngine2D().Extract(previousSheet, new[]
            {
                new TakeoffMarkup2D("M1", "A501", TakeoffMeasurementKind.Length, 100d, "ARC.WALL", "L01", "A-WALL", "H1")
            }).Evidence;

            var result = new AutodeskTakeoffPackageCoordinator().Build(
                package,
                new[] { admittedSheet },
                staleEvidence,
                Enumerable.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 1d);

            if (result.Readiness != TakeoffPackageReadiness.Blocked)
                throw new InvalidOperationException("Evidence from a replaced drawing source must block package readiness.");
            if (!result.Issues.Any(x => x.Code == "PKG.STALE_EVIDENCE_SOURCE" && x.SourceId == "M1"))
                throw new InvalidOperationException("Expected PKG.STALE_EVIDENCE_SOURCE for the stale markup.");
            if (result.CanEstimate || result.Inventory.Count != 0)
                throw new InvalidOperationException("Stale-source evidence must not reach inventory or estimate publication.");
        }
    }
}
