using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DSheetIngestionJpegComponentSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            AcceptsCanonicalThreeComponentFrame();
            RejectsZeroComponentIdentifier();
            RejectsDuplicateComponentIdentifier();
            RejectsZeroHorizontalSampling();
            RejectsZeroVerticalSampling();
            RejectsSamplingFactorAboveFour();
            RejectsQuantizationTableAboveThree();
        }

        private static void AcceptsCanonicalThreeComponentFrame()
        {
            var result = Ingest(Jpeg(
                1, 0x22, 0,
                2, 0x11, 1,
                3, 0x11, 1));
            Equal(RasterSheetFormat.Jpeg, result.RasterFormat.GetValueOrDefault(), "format");
            Equal(640, result.PixelWidth, "width");
            Equal(480, result.PixelHeight, "height");
        }

        private static void RejectsZeroComponentIdentifier()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(0, 0x11, 0)), "zero component id");
        }

        private static void RejectsDuplicateComponentIdentifier()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(1, 0x11, 0, 1, 0x11, 1)), "duplicate component id");
        }

        private static void RejectsZeroHorizontalSampling()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(1, 0x01, 0)), "zero horizontal sampling");
        }

        private static void RejectsZeroVerticalSampling()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(1, 0x10, 0)), "zero vertical sampling");
        }

        private static void RejectsSamplingFactorAboveFour()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(1, 0x51, 0)), "sampling factor above four");
        }

        private static void RejectsQuantizationTableAboveThree()
        {
            ThrowsInvalidOperation(() => Ingest(Jpeg(1, 0x11, 4)), "quantization table above three");
        }

        private static IngestedDrawingSheet2D Ingest(byte[] payload)
        {
            return new Qs2DSheetIngestor().IngestRaster(
                "A101", "Ground Floor", "A101.jpg", "R1", new DrawingCalibration(100d, 1d, "m"), payload);
        }

        private static byte[] Jpeg(params byte[] componentDescriptors)
        {
            if (componentDescriptors == null || componentDescriptors.Length == 0 || componentDescriptors.Length % 3 != 0)
                throw new ArgumentException("Component descriptors must be non-empty identifier/sampling/table triples.", "componentDescriptors");

            var componentCount = componentDescriptors.Length / 3;
            var frameLength = 8 + componentDescriptors.Length;
            var bytes = new List<byte>
            {
                0xFF, 0xD8,
                0xFF, 0xC0,
                (byte)(frameLength >> 8), (byte)frameLength,
                8,
                0x01, 0xE0,
                0x02, 0x80,
                (byte)componentCount
            };
            bytes.AddRange(componentDescriptors);
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
