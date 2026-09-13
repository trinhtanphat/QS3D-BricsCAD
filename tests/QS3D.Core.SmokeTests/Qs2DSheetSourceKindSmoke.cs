using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DSheetSourceKindSmoke
    {
        internal static void Run()
        {
            RejectsRasterReferenceForPdfPayload();
            RejectsPdfReferenceForRasterPayload();
            AcceptsMatchingReferenceWithQueryAndFragment();
            PreservesOpaqueSourceReferenceCompatibility();
        }

        private static void RejectsRasterReferenceForPdfPayload()
        {
            Throws<ArgumentException>(() => new Qs2DSheetIngestor().IngestPdf(
                "S1", "Plan", "drawings/A101.png", "R1", Calibration(), PdfPayload(), 1),
                "PDF ingestion must reject a recognized raster source reference.");
        }

        private static void RejectsPdfReferenceForRasterPayload()
        {
            Throws<ArgumentException>(() => new Qs2DSheetIngestor().IngestRaster(
                "S1", "Plan", "drawings/A101.pdf", "R1", Calibration(), PngPayload()),
                "Raster ingestion must reject a recognized PDF source reference.");
        }

        private static void AcceptsMatchingReferenceWithQueryAndFragment()
        {
            var result = new Qs2DSheetIngestor().IngestPdf(
                "S1", "Plan", "https://example.test/drawings/A101.PDF?token=abc#page=1", "R1", Calibration(), PdfPayload(), 1);
            Equal(DrawingSheetSourceKind.Pdf, result.Sheet.SourceKind, "Matching PDF reference should remain valid.");
        }

        private static void PreservesOpaqueSourceReferenceCompatibility()
        {
            var result = new Qs2DSheetIngestor().IngestPdf(
                "S2", "Plan", "sheet-store:opaque-A101", "R1", Calibration(), PdfPayload(), 1);
            Equal("sheet-store:opaque-A101", result.Sheet.SourceReference, "Opaque references must remain compatible.");
        }

        private static DrawingCalibration Calibration()
        {
            return new DrawingCalibration(10d, 100d, "mm");
        }

        private static byte[] PdfPayload()
        {
            return Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF\n");
        }

        private static byte[] PngPayload()
        {
            return new byte[]
            {
                137, 80, 78, 71, 13, 10, 26, 10,
                0, 0, 0, 13, 73, 72, 68, 82,
                0, 0, 0, 1, 0, 0, 0, 1
            };
        }

        private static void Throws<TException>(Action action, string message) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class Qs2DSheetSourceKindRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Qs2DSheetSourceKindSmoke.Run();
        }
    }
}
