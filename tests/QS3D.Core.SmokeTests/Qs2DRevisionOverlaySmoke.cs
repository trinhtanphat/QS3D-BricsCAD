using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DRevisionOverlaySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            PublishesDeterministicOverlay();
            RejectsMalformedDeltaEvidence();
        }

        private static void PublishesDeterministicOverlay()
        {
            var calibration = new DrawingCalibration(100d, 1d, "m");
            var oldSheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "A101-R1.pdf", "R1", calibration);
            var newSheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, "A101-R2.pdf", "R2", calibration);
            var engine = new CalibratedTakeoffEngine2D();
            var oldResult = engine.Extract(oldSheet, new[]
            {
                new TakeoffMarkup2D("M2", "A101", TakeoffMeasurementKind.Length, 2d, "ARC.WALL", "L01", "A-WALL", "H2"),
                new TakeoffMarkup2D("M1", "A101", TakeoffMeasurementKind.Count, 1d, "ARC.DOOR", "L01", "A-DOOR", "H1")
            });
            var newResult = engine.Extract(newSheet, new[]
            {
                new TakeoffMarkup2D("M1", "A101", TakeoffMeasurementKind.Count, 2d, "ARC.DOOR", "L01", "A-DOOR", "H1"),
                new TakeoffMarkup2D("M3", "A101", TakeoffMeasurementKind.Area, 3d, "ARC.FLOOR", "L01", "A-FLOR", "H3")
            });

            var overlay = new DrawingRevisionOverlay2D().Build(oldResult, newResult);
            Equal(3, overlay.Count, "overlay count");
            Equal("M1", overlay[0].MarkupId, "deterministic order 1");
            Equal(RevisionMarkupChangeKind.Changed, overlay[0].Kind, "changed kind");
            Near(1d, overlay[0].QuantityDelta, "changed signed delta");
            Equal("A101-R1.pdf", overlay[0].Previous!.SourceReference, "previous provenance");
            Equal("A101-R2.pdf", overlay[0].Current!.SourceReference, "current provenance");
            Equal(RevisionMarkupChangeKind.Removed, overlay[1].Kind, "removed kind");
            Equal(RevisionMarkupChangeKind.Added, overlay[2].Kind, "added kind");
        }

        private static void RejectsMalformedDeltaEvidence()
        {
            var evidence = new TakeoffQuantityEvidence2D("M1", "A101", "R1", "A101.pdf", "H1", "ARC.WALL", "L01", "A-WALL", 1d, "m");
            var malformed = new RevisionMarkupDelta2D("M1", RevisionMarkupChangeKind.Added, evidence, null);
            try
            {
                _ = new RevisionOverlayItem2D(malformed);
                throw new InvalidOperationException("malformed delta was accepted");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("change kind")) { }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
