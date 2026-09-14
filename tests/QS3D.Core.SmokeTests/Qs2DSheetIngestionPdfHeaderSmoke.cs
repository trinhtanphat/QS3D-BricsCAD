using System;
using System.Text;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DSheetIngestionPdfHeaderSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            AcceptsLfTerminatedHeader();
            AcceptsCrTerminatedHeader();
            AcceptsCrLfTerminatedHeader();
            AcceptsPdf10And20();
            RejectsUnsupportedPdfVersions();
            RejectsInlineDataAfterVersion();
            RejectsSpaceAfterVersion();
            RejectsTruncatedVersionOnlyHeader();
        }

        private static void AcceptsLfTerminatedHeader()
        {
            var result = Ingest("%PDF-1.7\n%%EOF");
            Equal(1, result.PdfPageNumber, "LF page number");
        }

        private static void AcceptsCrTerminatedHeader()
        {
            var result = Ingest("%PDF-1.7\r%%EOF");
            Equal(1, result.PdfPageNumber, "CR page number");
        }

        private static void AcceptsCrLfTerminatedHeader()
        {
            var result = Ingest("%PDF-1.7\r\n%%EOF");
            Equal(1, result.PdfPageNumber, "CRLF page number");
        }

        private static void AcceptsPdf10And20()
        {
            Equal(1, Ingest("%PDF-1.0\n%%EOF").PdfPageNumber, "PDF 1.0 page number");
            Equal(1, Ingest("%PDF-2.0\n%%EOF").PdfPageNumber, "PDF 2.0 page number");
        }

        private static void RejectsUnsupportedPdfVersions()
        {
            ThrowsInvalidOperation(() => Ingest("%PDF-0.9\n%%EOF"), "PDF 0.9");
            ThrowsInvalidOperation(() => Ingest("%PDF-1.8\n%%EOF"), "PDF 1.8");
            ThrowsInvalidOperation(() => Ingest("%PDF-2.1\n%%EOF"), "PDF 2.1");
            ThrowsInvalidOperation(() => Ingest("%PDF-9.9\n%%EOF"), "PDF 9.9");
        }

        private static void RejectsInlineDataAfterVersion()
        {
            ThrowsInvalidOperation(() => Ingest("%PDF-1.7garbage\n%%EOF"), "inline data after PDF version");
        }

        private static void RejectsSpaceAfterVersion()
        {
            ThrowsInvalidOperation(() => Ingest("%PDF-1.7 \n%%EOF"), "space after PDF version");
        }

        private static void RejectsTruncatedVersionOnlyHeader()
        {
            ThrowsInvalidOperation(() => Ingest("%PDF-1.7"), "truncated PDF version header");
        }

        private static IngestedDrawingSheet2D Ingest(string payload)
        {
            return new Qs2DSheetIngestor().IngestPdf(
                "A101", "Ground Floor", "A101.pdf", "R1", new DrawingCalibration(100d, 1d, "m"), Encoding.ASCII.GetBytes(payload), 1);
        }

        private static void ThrowsInvalidOperation(Action action, string label)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException(label + ": expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
