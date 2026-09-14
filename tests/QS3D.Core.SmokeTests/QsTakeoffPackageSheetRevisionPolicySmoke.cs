using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsTakeoffPackageSheetRevisionPolicySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            BuildsPackageAcrossIndependentSheetRevisions();
            RejectsEvidenceFromWrongSheetRevision();
        }

        private static void BuildsPackageAcrossIndependentSheetRevisions()
        {
            var package = new TakeoffPackageDefinition("PKG-MIX", "Tender Set", "P5", "Uniclass", "Default");
            var calibration = new DrawingCalibration(1d, 1d, "m");
            var a101 = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "A101-R3.pdf", "R3", calibration);
            var a102 = new DrawingSheet2D("A102", "Plan", DrawingSheetSourceKind.Pdf, "A102-R7.pdf", "R7", calibration);
            var engine = new CalibratedTakeoffEngine2D();
            var evidence = engine.Extract(a101, new[]
            {
                new TakeoffMarkup2D("M1", "A101", TakeoffMeasurementKind.Count, 2d, "ARC.DOOR", "L01", "A-DOOR", "H1")
            }).Evidence.Concat(engine.Extract(a102, new[]
            {
                new TakeoffMarkup2D("M2", "A102", TakeoffMeasurementKind.Count, 3d, "ARC.DOOR", "L01", "A-DOOR", "H2")
            }).Evidence).ToArray();

            var strict = new AutodeskTakeoffPackageCoordinator().Build(
                package,
                new[] { a101, a102 },
                evidence,
                Array.Empty<IfcQtoItem>(),
                (classification, measured) => measured,
                (classification, unit) => 10d);
            Equal(TakeoffPackageReadiness.Blocked, strict.Readiness, "legacy strict package remains blocked");

            var result = new AutodeskTakeoffPackagePerSheetRevisionCoordinator().Build(
                package,
                new[] { a101, a102 },
                evidence,
                Array.Empty<IfcQtoItem>(),
                (classification, measured) => measured,
                (classification, unit) => 10d);

            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "per-sheet revision package readiness");
            True(!result.Issues.Any(x => x.Code == "PKG.STALE_DRAWING_REVISION"), "per-sheet revisions do not create package-level stale sheet errors");
            True(!result.Issues.Any(x => x.Code == "PKG.STALE_EVIDENCE_REVISION"), "matching evidence revisions are admitted");
            Equal("R3", result.Sources.Single(x => x.Id == "A101").Revision, "A101 source revision provenance");
            Equal("R7", result.Sources.Single(x => x.Id == "A102").Revision, "A102 source revision provenance");
            Equal(1, result.Inventory.Count, "cross-sheet quantity aggregation row count");
            Near(5d, result.Inventory[0].MeasuredQuantity, "cross-sheet measured quantity");
            Near(50d, result.EstimatedCost, "cross-sheet estimate total");
        }

        private static void RejectsEvidenceFromWrongSheetRevision()
        {
            var package = new TakeoffPackageDefinition("PKG-STALE", "Tender Set", "P5", "Uniclass", "Default");
            var sheet = new DrawingSheet2D("A201", "Plan", DrawingSheetSourceKind.Pdf, "A201-R3.pdf", "R3", new DrawingCalibration(1d, 1d, "m"));
            var stale = new TakeoffQuantityEvidence2D(
                "M-ST", "A201", "P5", "A201-R3.pdf", "H-ST", "ARC.WALL", "L02", "A-WALL", 4d, "m");

            var result = new AutodeskTakeoffPackagePerSheetRevisionCoordinator().Build(
                package,
                new[] { sheet },
                new[] { stale },
                Array.Empty<IfcQtoItem>(),
                (classification, measured) => measured,
                (classification, unit) => 1d);

            Equal(TakeoffPackageReadiness.Blocked, result.Readiness, "wrong sheet revision blocks package");
            True(result.Issues.Any(x => x.Code == "PKG.STALE_EVIDENCE_REVISION"), "wrong sheet revision issue");
            True(!result.CanEstimate, "wrong sheet revision cannot estimate");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
