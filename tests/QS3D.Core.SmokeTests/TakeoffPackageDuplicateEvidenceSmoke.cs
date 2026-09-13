using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class TakeoffPackageDuplicateEvidenceSmoke
    {
        internal static void Run()
        {
            BlocksDuplicateEvidenceOnSameSheet();
            AllowsSameMarkupIdOnDifferentSheets();
        }

        private static void BlocksDuplicateEvidenceOnSameSheet()
        {
            var sheet = Sheet("A101", "A101.pdf");
            var evidence = Evidence("M-1", "A101", "A101.pdf", 5d);
            var result = Build(new[] { sheet }, new[] { evidence, evidence });

            Equal(TakeoffPackageReadiness.Blocked, result.Readiness, "Duplicate evidence must block package admission.");
            True(result.Issues.Any(x => x.Code == "PKG.DUPLICATE_EVIDENCE"), "Duplicate evidence must produce PKG.DUPLICATE_EVIDENCE.");
            Equal(0, result.Inventory.Count, "Blocked duplicate evidence must not reach inventory or estimate.");
        }

        private static void AllowsSameMarkupIdOnDifferentSheets()
        {
            var sheetA = Sheet("A101", "A101.pdf");
            var sheetB = Sheet("A102", "A102.pdf");
            var result = Build(
                new[] { sheetA, sheetB },
                new[]
                {
                    Evidence("M-1", "A101", "A101.pdf", 5d),
                    Evidence("M-1", "A102", "A102.pdf", 7d)
                });

            True(!result.Issues.Any(x => x.Code == "PKG.DUPLICATE_EVIDENCE"), "Markup ids are sheet-local and may repeat across different sheets.");
            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "Valid cross-sheet evidence must remain publishable.");
            Equal(12d, result.Inventory.Single().MeasuredQuantity, "Cross-sheet quantities must aggregate once each.");
        }

        private static TakeoffPackageBuildResult Build(IEnumerable<DrawingSheet2D> sheets, IEnumerable<TakeoffQuantityEvidence2D> evidence)
        {
            return new AutodeskTakeoffPackageCoordinator().Build(
                new TakeoffPackageDefinition("PKG-1", "Concrete", "R1", "Uniclass", "Default"),
                sheets,
                evidence,
                Enumerable.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 10d);
        }

        private static DrawingSheet2D Sheet(string id, string sourceReference)
        {
            return new DrawingSheet2D(id, id, DrawingSheetSourceKind.Pdf, sourceReference, "R1", new DrawingCalibration(1d, 1d, "m"));
        }

        private static TakeoffQuantityEvidence2D Evidence(string markupId, string sheetId, string sourceReference, double quantity)
        {
            return new TakeoffQuantityEvidence2D(markupId, sheetId, "R1", sourceReference, "handle-" + sheetId + "-" + markupId, "03-CONCRETE", "L01", "TAKEOFF", quantity, "m3");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class TakeoffPackageDuplicateEvidenceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TakeoffPackageDuplicateEvidenceSmoke.Run();
        }
    }
}
