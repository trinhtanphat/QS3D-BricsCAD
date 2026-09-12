using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DTakeoffRevisionProvenanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DetectsSourceHandleChangeWithoutQuantityChange();
            PreservesUnchangedEvidenceWhenHandleIsStable();
            PropagatesHandleChangeIntoPackageReview();
        }

        private static void DetectsSourceHandleChangeWithoutQuantityChange()
        {
            var previous = Sheet("R1", "drawing-r1.pdf", "polyline:100");
            var current = Sheet("R2", "drawing-r2.pdf", "polyline:200");

            var delta = new DrawingRevisionComparer2D().Compare(previous, current).Single();
            Equal(RevisionMarkupChangeKind.Changed, delta.Kind, "changed handle kind");
            Equal(0d, delta.QuantityDelta, "changed handle quantity delta");
        }

        private static void PreservesUnchangedEvidenceWhenHandleIsStable()
        {
            var previous = Sheet("R1", "drawing-r1.pdf", "polyline:100");
            var current = Sheet("R2", "drawing-r2.pdf", "polyline:100");

            var delta = new DrawingRevisionComparer2D().Compare(previous, current).Single();
            Equal(RevisionMarkupChangeKind.Unchanged, delta.Kind, "stable handle kind");
        }

        private static void PropagatesHandleChangeIntoPackageReview()
        {
            var previousPackage = new TakeoffPackageDefinition("PKG-1", "Concrete", "R1", "Uniclass", "Default");
            var currentPackage = new TakeoffPackageDefinition("PKG-1", "Concrete", "R2", "Uniclass", "Default");
            var comparison = new AutodeskTakeoffPackageRevisionComparer().Compare(
                previousPackage,
                new[] { Sheet("R1", "drawing-r1.pdf", "polyline:100") },
                currentPackage,
                new[] { Sheet("R2", "drawing-r2.pdf", "polyline:200") });

            Equal(1, comparison.ChangedMarkupCount, "package changed markup count");
            Equal(1, comparison.ChangedSheetCount, "package changed sheet count");
            Equal(true, comparison.RequiresReview, "package review flag");
            Equal(0d, comparison.QuantityDelta, "package quantity delta");
        }

        private static TakeoffSheetResult2D Sheet(string revision, string sourceReference, string sourceHandle)
        {
            var sheet = new DrawingSheet2D("SHEET-1", "Ground Floor", DrawingSheetSourceKind.Pdf, sourceReference, revision, new DrawingCalibration(100d, 1d, "m"));
            var markup = new TakeoffMarkup2D("M-1", sheet.Id, TakeoffMeasurementKind.Length, 250d, "CONCRETE", "ZONE-A", "QTO", sourceHandle);
            return new CalibratedTakeoffEngine2D().Extract(sheet, new[] { markup });
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Qs2DTakeoffRevisionProvenanceSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
