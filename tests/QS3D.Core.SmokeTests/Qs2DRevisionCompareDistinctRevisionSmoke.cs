using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DRevisionCompareDistinctRevisionSmoke
    {
        internal static void Run()
        {
            RejectsSameRevisionCompare();
            PreservesCrossRevisionDeltaSemantics();
        }

        private static void RejectsSameRevisionCompare()
        {
            var previous = Result("R1", "drawings/A101-R1.pdf", Evidence("M-1", "R1", "drawings/A101-R1.pdf", 2d));
            var current = Result("r1", "drawings/A101-republished.pdf", Evidence("M-1", "r1", "drawings/A101-republished.pdf", 3d));

            Throws<InvalidOperationException>(
                () => new DrawingRevisionComparer2D().Compare(previous, current),
                "Revision compare must fail closed when previous/current carry the same logical revision identifier.");
        }

        private static void PreservesCrossRevisionDeltaSemantics()
        {
            var previous = Result(
                "R1",
                "drawings/A101-R1.pdf",
                Evidence("M-UNCHANGED", "R1", "drawings/A101-R1.pdf", 2d),
                Evidence("M-CHANGED", "R1", "drawings/A101-R1.pdf", 5d),
                Evidence("M-REMOVED", "R1", "drawings/A101-R1.pdf", 4d));
            var current = Result(
                "R2",
                "drawings/A101-R2.pdf",
                Evidence("M-UNCHANGED", "R2", "drawings/A101-R2.pdf", 2d),
                Evidence("M-CHANGED", "R2", "drawings/A101-R2.pdf", 7d),
                Evidence("M-ADDED", "R2", "drawings/A101-R2.pdf", 3d));

            var delta = new DrawingRevisionComparer2D().Compare(previous, current);

            Equal(4, delta.Count, "Cross-revision compare must preserve one delta row per markup id.");
            Equal(RevisionMarkupChangeKind.Added, delta[0].Kind, "Added markup classification must remain intact.");
            Equal(3d, delta[0].QuantityDelta, "Added markup delta must remain positive current quantity.");
            Equal(RevisionMarkupChangeKind.Changed, delta[1].Kind, "Changed markup classification must remain intact.");
            Equal(2d, delta[1].QuantityDelta, "Changed markup delta must preserve signed quantity arithmetic.");
            Equal(RevisionMarkupChangeKind.Removed, delta[2].Kind, "Removed markup classification must remain intact.");
            Equal(-4d, delta[2].QuantityDelta, "Removed markup delta must remain negative previous quantity.");
            Equal(RevisionMarkupChangeKind.Unchanged, delta[3].Kind, "Unchanged markup classification must remain intact.");
            Equal(0d, delta[3].QuantityDelta, "Unchanged markup delta must remain zero.");
        }

        private static TakeoffSheetResult2D Result(string revision, string sourceReference, params TakeoffQuantityEvidence2D[] evidence)
        {
            return new TakeoffSheetResult2D(
                new DrawingSheet2D("A101", "Floor plan", DrawingSheetSourceKind.Pdf, sourceReference, revision, new DrawingCalibration(1d, 1d, "m")),
                evidence);
        }

        private static TakeoffQuantityEvidence2D Evidence(string id, string revision, string sourceReference, double quantity)
        {
            return new TakeoffQuantityEvidence2D(id, "A101", revision, sourceReference, "H-" + id, "Walls", "L01", "A-WALL", quantity, "m");
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class Qs2DRevisionCompareDistinctRevisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Qs2DRevisionCompareDistinctRevisionSmoke.Run();
        }
    }
}
