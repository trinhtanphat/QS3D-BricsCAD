using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsTakeoffPackageRetainedSheetRevisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            AcceptsRetainedUnchangedDrawingRevision();
            RejectsMutatedEvidenceUnderSameDrawingRevision();
            RejectsEqualPackageRevisionIdentity();
        }

        private static void AcceptsRetainedUnchangedDrawingRevision()
        {
            var previousPackage = Package("P5");
            var currentPackage = Package("P6");
            var retainedPrevious = Sheet("A101", "R3", "A101-R3.pdf", "M1", 2d);
            var retainedCurrent = Sheet("A101", "R3", "A101-R3.pdf", "M1", 2d);
            var changedPrevious = Sheet("A102", "R7", "A102-R7.pdf", "M2", 3d);
            var changedCurrent = Sheet("A102", "R8", "A102-R8.pdf", "M2", 4d);

            var result = new AutodeskTakeoffPackageRevisionComparer().Compare(
                previousPackage,
                new[] { retainedPrevious, changedPrevious },
                currentPackage,
                new[] { retainedCurrent, changedCurrent });

            Equal(2, result.SheetDeltas.Count, "sheet delta count");
            var retained = result.SheetDeltas.Single(x => x.SheetId == "A101");
            Equal("R3", retained.PreviousRevision, "retained previous revision");
            Equal("R3", retained.CurrentRevision, "retained current revision");
            Equal(1, retained.UnchangedCount, "retained unchanged markup count");
            Equal(0, retained.ChangedCount, "retained changed markup count");
            True(!retained.HasMaterialChange, "retained sheet is not materially changed");
            Equal(1, result.ChangedSheetCount, "only revised sheet requires review");
            Near(1d, result.QuantityDelta, "package quantity delta excludes retained sheet");
        }

        private static void RejectsMutatedEvidenceUnderSameDrawingRevision()
        {
            var previous = Sheet("A201", "R4", "A201-R4.pdf", "M1", 2d);
            var mutated = Sheet("A201", "R4", "A201-R4.pdf", "M1", 2.5d);
            ThrowsInvalidOperation(
                () => new AutodeskTakeoffPackageRevisionComparer().Compare(Package("P1"), new[] { previous }, Package("P2"), new[] { mutated }),
                "same revision mutated evidence");

            var movedSource = Sheet("A201", "R4", "A201-R4-copy.pdf", "M1", 2d);
            ThrowsInvalidOperation(
                () => new AutodeskTakeoffPackageRevisionComparer().Compare(Package("P1"), new[] { previous }, Package("P2"), new[] { movedSource }),
                "same revision changed source identity");
        }

        private static void RejectsEqualPackageRevisionIdentity()
        {
            var sheet = Sheet("A301", "R1", "A301-R1.pdf", "M1", 1d);
            ThrowsInvalidOperation(
                () => new AutodeskTakeoffPackageRevisionComparer().Compare(Package("P9"), new[] { sheet }, Package("P9"), new[] { sheet }),
                "equal package revision identity");
        }

        private static TakeoffPackageDefinition Package(string revision)
        {
            return new TakeoffPackageDefinition("PKG-RET", "Tender Set", revision, "Uniclass", "Default");
        }

        private static TakeoffSheetResult2D Sheet(string id, string revision, string sourceReference, string markupId, double quantity)
        {
            var drawing = new DrawingSheet2D(id, id, DrawingSheetSourceKind.Pdf, sourceReference, revision, new DrawingCalibration(1d, 1d, "m"));
            var evidence = new TakeoffQuantityEvidence2D(markupId, id, revision, sourceReference, "H-" + markupId, "ARC.WALL", "L01", "A-WALL", quantity, "m");
            return new TakeoffSheetResult2D(drawing, new[] { evidence });
        }

        private static void ThrowsInvalidOperation(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException(label + ": expected InvalidOperationException.");
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
