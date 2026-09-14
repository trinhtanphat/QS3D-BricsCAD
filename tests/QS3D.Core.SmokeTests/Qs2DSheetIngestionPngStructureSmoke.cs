using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DSheetIngestionPngStructureSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            AcceptsCanonicalIhdr();
            AcceptsSupportedIhdrSemantics();
            RejectsWrongFirstChunkType();
            RejectsWrongIhdrLength();
            RejectsTruncatedIhdr();
            RejectsCorruptedIhdrData();
            RejectsCorruptedIhdrCrc();
            RejectsInvalidColorType();
            RejectsInvalidBitDepthForColorType();
            RejectsInvalidCompressionMethod();
            RejectsInvalidFilterMethod();
            RejectsInvalidInterlaceMethod();
            RejectsCorruptedIendCrc();
        }

        private static void AcceptsCanonicalIhdr()
        {
            var result = Ingest(Png(13, 'I', 'H', 'D', 'R', true, 8, 2, 0, 0, 0));
            Equal(RasterSheetFormat.Png, result.RasterFormat.GetValueOrDefault(), "format");
            Equal(640, result.PixelWidth, "width");
            Equal(480, result.PixelHeight, "height");
        }

        private static void AcceptsSupportedIhdrSemantics()
        {
            Ingest(Png(13, 'I', 'H', 'D', 'R', true, 1, 0, 0, 0, 1));
            Ingest(Png(13, 'I', 'H', 'D', 'R', true, 16, 2, 0, 0, 0));
            Ingest(Png(13, 'I', 'H', 'D', 'R', true, 4, 3, 0, 0, 0));
            Ingest(Png(13, 'I', 'H', 'D', 'R', true, 8, 4, 0, 0, 0));
            Ingest(Png(13, 'I', 'H', 'D', 'R', true, 16, 6, 0, 0, 1));
        }

        private static void RejectsWrongFirstChunkType()
        {
            ThrowsInvalidOperation(() => Ingest(Png(13, 'J', 'H', 'D', 'R', true, 8, 2, 0, 0, 0)), "wrong first chunk type");
        }

        private static void RejectsWrongIhdrLength()
        {
            ThrowsInvalidOperation(() => Ingest(Png(12, 'I', 'H', 'D', 'R', true, 8, 2, 0, 0, 0)), "wrong IHDR length");
        }

        private static void RejectsTruncatedIhdr()
        {
            var payload = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 2, 128 };
            ThrowsInvalidOperation(() => Ingest(payload), "truncated IHDR");
        }

        private static void RejectsCorruptedIhdrData()
        {
            var payload = Png(13, 'I', 'H', 'D', 'R', true, 8, 2, 0, 0, 0);
            payload[19] ^= 1;
            ThrowsInvalidOperation(() => Ingest(payload), "corrupted IHDR data");
        }

        private static void RejectsCorruptedIhdrCrc()
        {
            var payload = Png(13, 'I', 'H', 'D', 'R', true, 8, 2, 0, 0, 0);
            payload[32] ^= 1;
            ThrowsInvalidOperation(() => Ingest(payload), "corrupted IHDR CRC");
        }

        private static void RejectsInvalidColorType()
        {
            ThrowsInvalidOperation(() => Ingest(Png(13, 'I', 'H', 'D', 'R', true, 8, 5, 0, 0, 0)), "invalid color type");
        }

        private static void RejectsInvalidBitDepthForColorType()
        {
            ThrowsInvalidOperation(() => Ingest(Png(13, 'I', 'H', 'D', 'R', true, 4, 2, 0, 0, 0)), "invalid truecolor bit depth");
            ThrowsInvalidOperation(() => Ingest(Png(13, 'I', 'H', 'D', 'R', true, 16, 3, 0, 0, 0)), "invalid indexed bit depth");
        }

        private static void RejectsInvalidCompressionMethod()
        {
            ThrowsInvalidOperation(() => Ingest(Png(13, 'I', 'H', 'D', 'R', true, 8, 2, 1, 0, 0)), "invalid compression method");
        }

        private static void RejectsInvalidFilterMethod()
        {
            ThrowsInvalidOperation(() => Ingest(Png(13, 'I', 'H', 'D', 'R', true, 8, 2, 0, 1, 0)), "invalid filter method");
        }

        private static void RejectsInvalidInterlaceMethod()
        {
            ThrowsInvalidOperation(() => Ingest(Png(13, 'I', 'H', 'D', 'R', true, 8, 2, 0, 0, 2)), "invalid interlace method");
        }

        private static void RejectsCorruptedIendCrc()
        {
            var payload = Png(13, 'I', 'H', 'D', 'R', true, 8, 2, 0, 0, 0);
            payload[payload.Length - 1] ^= 1;
            ThrowsInvalidOperation(() => Ingest(payload), "corrupted IEND CRC");
        }

        private static IngestedDrawingSheet2D Ingest(byte[] payload)
        {
            return new Qs2DSheetIngestor().IngestRaster(
                "A101", "Ground Floor", "A101.png", "R1", new DrawingCalibration(100d, 1d, "m"), payload);
        }

        private static byte[] Png(int ihdrLength, char c0, char c1, char c2, char c3, bool includeIend,
            byte bitDepth, byte colorType, byte compressionMethod, byte filterMethod, byte interlaceMethod)
        {
            var bytes = new List<byte> { 137, 80, 78, 71, 13, 10, 26, 10,
                (byte)(ihdrLength >> 24), (byte)(ihdrLength >> 16), (byte)(ihdrLength >> 8), (byte)ihdrLength,
                (byte)c0, (byte)c1, (byte)c2, (byte)c3,
                0, 0, 2, 128, 0, 0, 1, 224,
                bitDepth, colorType, compressionMethod, filterMethod, interlaceMethod,
                0, 0, 0, 0 };
            if (ihdrLength == 13 && c0 == 'I' && c1 == 'H' && c2 == 'D' && c3 == 'R')
                WriteCrc(bytes, 12, 13, 29);
            if (includeIend)
                bytes.AddRange(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 0xAE, 0x42, 0x60, 0x82 });
            return bytes.ToArray();
        }

        private static void WriteCrc(List<byte> bytes, int typeOffset, int dataLength, int crcOffset)
        {
            uint crc = 0xffffffffu;
            for (var i = typeOffset; i < typeOffset + 4 + dataLength; i++)
            {
                crc ^= bytes[i];
                for (var bit = 0; bit < 8; bit++)
                    crc = (crc >> 1) ^ ((crc & 1u) != 0u ? 0xedb88320u : 0u);
            }
            crc ^= 0xffffffffu;
            bytes[crcOffset] = (byte)(crc >> 24);
            bytes[crcOffset + 1] = (byte)(crc >> 16);
            bytes[crcOffset + 2] = (byte)(crc >> 8);
            bytes[crcOffset + 3] = (byte)crc;
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
