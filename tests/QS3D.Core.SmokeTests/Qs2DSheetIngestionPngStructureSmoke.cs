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
            AcceptsAncillaryAndMultipleIdatChunks();
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
            RejectsMissingIdat();
            RejectsDuplicateIhdr();
            RejectsCorruptedIntermediateChunkCrc();
            RejectsOversizedChunkLength();
            RejectsDataAfterIend();
            RejectsCorruptedIendCrc();
        }

        private static void AcceptsCanonicalIhdr()
        {
            var result = Ingest(Png(8, 2, 0, 0, 0));
            Equal(RasterSheetFormat.Png, result.RasterFormat.GetValueOrDefault(), "format");
            Equal(640, result.PixelWidth, "width");
            Equal(480, result.PixelHeight, "height");
        }

        private static void AcceptsSupportedIhdrSemantics()
        {
            Ingest(Png(1, 0, 0, 0, 1));
            Ingest(Png(16, 2, 0, 0, 0));
            Ingest(Png(4, 3, 0, 0, 0));
            Ingest(Png(8, 4, 0, 0, 0));
            Ingest(Png(16, 6, 0, 0, 1));
        }

        private static void AcceptsAncillaryAndMultipleIdatChunks()
        {
            var bytes = Header(8, 2, 0, 0, 0);
            AddChunk(bytes, "tEXt", new byte[] { 65, 0, 66 });
            AddChunk(bytes, "IDAT", new byte[] { 1, 2 });
            AddChunk(bytes, "IDAT", new byte[] { 3, 4 });
            AddChunk(bytes, "IEND", new byte[0]);
            Ingest(bytes.ToArray());
        }

        private static void RejectsWrongFirstChunkType()
        {
            var bytes = Header(8, 2, 0, 0, 0);
            bytes[12] = (byte)'J';
            ThrowsInvalidOperation(() => Ingest(bytes.ToArray()), "wrong first chunk type");
        }

        private static void RejectsWrongIhdrLength()
        {
            var payload = Png(8, 2, 0, 0, 0);
            payload[11] = 12;
            ThrowsInvalidOperation(() => Ingest(payload), "wrong IHDR length");
        }

        private static void RejectsTruncatedIhdr()
        {
            var payload = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 2, 128 };
            ThrowsInvalidOperation(() => Ingest(payload), "truncated IHDR");
        }

        private static void RejectsCorruptedIhdrData()
        {
            var payload = Png(8, 2, 0, 0, 0);
            payload[19] ^= 1;
            ThrowsInvalidOperation(() => Ingest(payload), "corrupted IHDR data");
        }

        private static void RejectsCorruptedIhdrCrc()
        {
            var payload = Png(8, 2, 0, 0, 0);
            payload[32] ^= 1;
            ThrowsInvalidOperation(() => Ingest(payload), "corrupted IHDR CRC");
        }

        private static void RejectsInvalidColorType() { ThrowsInvalidOperation(() => Ingest(Png(8, 5, 0, 0, 0)), "invalid color type"); }
        private static void RejectsInvalidBitDepthForColorType()
        {
            ThrowsInvalidOperation(() => Ingest(Png(4, 2, 0, 0, 0)), "invalid truecolor bit depth");
            ThrowsInvalidOperation(() => Ingest(Png(16, 3, 0, 0, 0)), "invalid indexed bit depth");
        }
        private static void RejectsInvalidCompressionMethod() { ThrowsInvalidOperation(() => Ingest(Png(8, 2, 1, 0, 0)), "invalid compression method"); }
        private static void RejectsInvalidFilterMethod() { ThrowsInvalidOperation(() => Ingest(Png(8, 2, 0, 1, 0)), "invalid filter method"); }
        private static void RejectsInvalidInterlaceMethod() { ThrowsInvalidOperation(() => Ingest(Png(8, 2, 0, 0, 2)), "invalid interlace method"); }

        private static void RejectsMissingIdat()
        {
            var bytes = Header(8, 2, 0, 0, 0);
            AddChunk(bytes, "IEND", new byte[0]);
            ThrowsInvalidOperation(() => Ingest(bytes.ToArray()), "missing IDAT");
        }

        private static void RejectsDuplicateIhdr()
        {
            var bytes = Header(8, 2, 0, 0, 0);
            AddChunk(bytes, "IHDR", new byte[13]);
            AddChunk(bytes, "IDAT", new byte[] { 1 });
            AddChunk(bytes, "IEND", new byte[0]);
            ThrowsInvalidOperation(() => Ingest(bytes.ToArray()), "duplicate IHDR");
        }

        private static void RejectsCorruptedIntermediateChunkCrc()
        {
            var payload = Png(8, 2, 0, 0, 0);
            payload[45] ^= 1;
            ThrowsInvalidOperation(() => Ingest(payload), "corrupted IDAT CRC");
        }

        private static void RejectsOversizedChunkLength()
        {
            var payload = Png(8, 2, 0, 0, 0);
            payload[33] = 0x7f;
            payload[34] = 0xff;
            payload[35] = 0xff;
            payload[36] = 0xff;
            ThrowsInvalidOperation(() => Ingest(payload), "oversized chunk length");
        }

        private static void RejectsDataAfterIend()
        {
            var bytes = new List<byte>(Png(8, 2, 0, 0, 0));
            bytes.Add(0);
            ThrowsInvalidOperation(() => Ingest(bytes.ToArray()), "data after IEND");
        }

        private static void RejectsCorruptedIendCrc()
        {
            var payload = Png(8, 2, 0, 0, 0);
            payload[payload.Length - 1] ^= 1;
            ThrowsInvalidOperation(() => Ingest(payload), "corrupted IEND CRC");
        }

        private static IngestedDrawingSheet2D Ingest(byte[] payload)
        {
            return new Qs2DSheetIngestor().IngestRaster("A101", "Ground Floor", "A101.png", "R1", new DrawingCalibration(100d, 1d, "m"), payload);
        }

        private static byte[] Png(byte bitDepth, byte colorType, byte compressionMethod, byte filterMethod, byte interlaceMethod)
        {
            var bytes = Header(bitDepth, colorType, compressionMethod, filterMethod, interlaceMethod);
            AddChunk(bytes, "IDAT", new byte[] { 1 });
            AddChunk(bytes, "IEND", new byte[0]);
            return bytes.ToArray();
        }

        private static List<byte> Header(byte bitDepth, byte colorType, byte compressionMethod, byte filterMethod, byte interlaceMethod)
        {
            var bytes = new List<byte> { 137, 80, 78, 71, 13, 10, 26, 10 };
            AddChunk(bytes, "IHDR", new byte[] { 0, 0, 2, 128, 0, 0, 1, 224, bitDepth, colorType, compressionMethod, filterMethod, interlaceMethod });
            return bytes;
        }

        private static void AddChunk(List<byte> bytes, string type, byte[] data)
        {
            var start = bytes.Count;
            var length = data.Length;
            bytes.Add((byte)(length >> 24)); bytes.Add((byte)(length >> 16)); bytes.Add((byte)(length >> 8)); bytes.Add((byte)length);
            bytes.Add((byte)type[0]); bytes.Add((byte)type[1]); bytes.Add((byte)type[2]); bytes.Add((byte)type[3]);
            bytes.AddRange(data);
            bytes.AddRange(new byte[4]);
            WriteCrc(bytes, start + 4, length, start + 8 + length);
        }

        private static void WriteCrc(List<byte> bytes, int typeOffset, int dataLength, int crcOffset)
        {
            uint crc = 0xffffffffu;
            for (var i = typeOffset; i < typeOffset + 4 + dataLength; i++)
            {
                crc ^= bytes[i];
                for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1u) != 0u ? 0xedb88320u : 0u);
            }
            crc ^= 0xffffffffu;
            bytes[crcOffset] = (byte)(crc >> 24); bytes[crcOffset + 1] = (byte)(crc >> 16); bytes[crcOffset + 2] = (byte)(crc >> 8); bytes[crcOffset + 3] = (byte)crc;
        }

        private static void ThrowsInvalidOperation(Action action, string label)
        {
            try { action(); } catch (InvalidOperationException) { return; }
            throw new InvalidOperationException(label + ": expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
