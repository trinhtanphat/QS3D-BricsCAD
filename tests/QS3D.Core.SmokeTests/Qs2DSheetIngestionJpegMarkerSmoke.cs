using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DSheetIngestionJpegMarkerSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            AcceptsTemBeforeSof();
            RejectsStuffedMarkerBeforeSof();
            RejectsRestartMarkerBeforeSof();
            RejectsNestedSoiBeforeSof();
            RejectsPrematureEoiBeforeSof();
            RejectsPrematureSosBeforeSof();
            RejectsRawDataBeforeSof();
        }

        private static void AcceptsTemBeforeSof()
        {
            var result = Ingest(JpegWithPrefix(0xFF, 0x01));
            Equal(640, result.PixelWidth, "TEM width");
            Equal(480, result.PixelHeight, "TEM height");
        }

        private static void RejectsStuffedMarkerBeforeSof() { Reject(0xFF, 0x00, "stuffed marker"); }
        private static void RejectsRestartMarkerBeforeSof() { Reject(0xFF, 0xD0, "restart marker"); }
        private static void RejectsNestedSoiBeforeSof() { Reject(0xFF, 0xD8, "nested SOI"); }
        private static void RejectsPrematureEoiBeforeSof() { Reject(0xFF, 0xD9, "premature EOI"); }
        private static void RejectsPrematureSosBeforeSof() { Reject(0xFF, 0xDA, "premature SOS"); }
        private static void RejectsRawDataBeforeSof() { Reject(0x7F, "raw marker-stream data"); }

        private static void Reject(byte first, byte second, string label)
        {
            ThrowsInvalidOperation(() => Ingest(JpegWithPrefix(first, second)), label);
        }

        private static void Reject(byte first, string label)
        {
            ThrowsInvalidOperation(() => Ingest(JpegWithPrefix(first)), label);
        }

        private static IngestedDrawingSheet2D Ingest(byte[] payload)
        {
            return new Qs2DSheetIngestor().IngestRaster(
                "A101", "Ground Floor", "A101.jpg", "R1", new DrawingCalibration(100d, 1d, "m"), payload);
        }

        private static byte[] JpegWithPrefix(params byte[] prefix)
        {
            var bytes = new List<byte> { 0xFF, 0xD8 };
            if (prefix != null) bytes.AddRange(prefix);
            bytes.AddRange(new byte[]
            {
                0xFF, 0xC0,
                0x00, 0x0B,
                0x08,
                0x01, 0xE0,
                0x02, 0x80,
                0x01,
                0x01, 0x11, 0x00,
                0xFF, 0xD9
            });
            return bytes.ToArray();
        }

        private static void ThrowsInvalidOperation(Action action, string label)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException(label + ": expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
