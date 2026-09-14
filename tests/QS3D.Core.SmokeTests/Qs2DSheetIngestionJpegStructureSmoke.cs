using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DSheetIngestionJpegStructureSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            AcceptsCanonicalSingleComponentFrame();
            RejectsZeroSamplePrecision();
            RejectsZeroComponentCount();
            RejectsFrameLengthComponentMismatch();
        }

        private static void AcceptsCanonicalSingleComponentFrame()
        {
            var result = Ingest(Jpeg(8, 1, 11));
            Equal(RasterSheetFormat.Jpeg, result.RasterFormat.GetValueOrDefault(), "format");
            Equal(640, result.PixelWidth, "width");
            Equal(480, result.PixelHeight, "height");
        }

        private static void RejectsZeroSamplePrecision()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(0, 1, 11)), "zero sample precision");
        }

        private static void RejectsZeroComponentCount()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(8, 0, 8)), "zero component count");
        }

        private static void RejectsFrameLengthComponentMismatch()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(8, 1, 14)), "frame length/component mismatch");
        }

        private static IngestedDrawingSheet2D Ingest(byte[] payload)
        {
            return new Qs2DSheetIngestor().IngestRaster(
                "A101", "Ground Floor", "A101.jpg", "R1", new DrawingCalibration(100d, 1d, "m"), payload);
        }

        private static byte[] Jpeg(byte precision, byte componentCount, int frameLength)
        {
            var bytes = new List<byte>
            {
                0xFF, 0xD8,
                0xFF, 0xC0,
                (byte)(frameLength >> 8), (byte)frameLength,
                precision,
                0x01, 0xE0,
                0x02, 0x80,
                componentCount
            };

            var componentBytes = Math.Max(0, frameLength - 8);
            for (var i = 0; i < componentBytes; i++)
            {
                var descriptorField = i % 3;
                bytes.Add(descriptorField == 0 ? (byte)((i / 3) + 1) : descriptorField == 1 ? (byte)0x11 : (byte)0);
            }
            bytes.Add(0xFF);
            bytes.Add(0xD9);
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
