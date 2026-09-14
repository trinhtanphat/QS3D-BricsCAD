using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    public enum RasterSheetFormat { Png, Jpeg }

    public sealed class IngestedDrawingSheet2D
    {
        internal IngestedDrawingSheet2D(DrawingSheet2D sheet, string sourceSha256, int byteLength, int pdfPageNumber, RasterSheetFormat? rasterFormat, int pixelWidth, int pixelHeight)
        {
            Sheet = sheet ?? throw new ArgumentNullException("sheet");
            SourceSha256 = QsModelElementSnapshot.Require(sourceSha256, "sourceSha256");
            if (byteLength <= 0) throw new ArgumentOutOfRangeException("byteLength");
            ByteLength = byteLength;
            PdfPageNumber = pdfPageNumber;
            RasterFormat = rasterFormat;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
        }

        public DrawingSheet2D Sheet { get; private set; }
        public string SourceSha256 { get; private set; }
        public int ByteLength { get; private set; }
        public int PdfPageNumber { get; private set; }
        public RasterSheetFormat? RasterFormat { get; private set; }
        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }
    }

    public sealed class Qs2DSheetIngestor
    {
        private static readonly byte[] PngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly byte[] PdfEofMarker = new byte[] { (byte)'%', (byte)'%', (byte)'E', (byte)'O', (byte)'F' };

        public IngestedDrawingSheet2D IngestPdf(string id, string name, string sourceReference, string revision, DrawingCalibration calibration, byte[] payload, int pageNumber)
        {
            RequirePayload(payload);
            if (pageNumber < 1) throw new ArgumentOutOfRangeException("pageNumber");
            ValidateRecognizedSourceReference(sourceReference, DrawingSheetSourceKind.Pdf);
            ValidatePdfPayload(payload);

            var sheet = new DrawingSheet2D(id, name, DrawingSheetSourceKind.Pdf, sourceReference, revision, calibration);
            return new IngestedDrawingSheet2D(sheet, Sha256(payload), payload.Length, pageNumber, null, 0, 0);
        }

        public IngestedDrawingSheet2D IngestRaster(string id, string name, string sourceReference, string revision, DrawingCalibration calibration, byte[] payload)
        {
            RequirePayload(payload);
            ValidateRecognizedSourceReference(sourceReference, DrawingSheetSourceKind.RasterImage);
            RasterSheetFormat format;
            int width;
            int height;
            if (TryPng(payload, out width, out height))
            {
                ValidatePngChunkStream(payload);
                format = RasterSheetFormat.Png;
            }
            else if (TryJpeg(payload, out width, out height))
            {
                ValidateJpegTerminator(payload);
                format = RasterSheetFormat.Jpeg;
            }
            else throw new InvalidOperationException("Raster payload must be a supported PNG or JPEG image.");

            var sheet = new DrawingSheet2D(id, name, DrawingSheetSourceKind.RasterImage, sourceReference, revision, calibration);
            return new IngestedDrawingSheet2D(sheet, Sha256(payload), payload.Length, 0, format, width, height);
        }

        private static void RequirePayload(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            if (payload.Length == 0) throw new ArgumentException("Sheet payload cannot be empty.", "payload");
        }

        private static void ValidateRecognizedSourceReference(string sourceReference, DrawingSheetSourceKind sourceKind)
        {
            var reference = QsModelElementSnapshot.Require(sourceReference, "sourceReference");
            var path = SourcePath(reference);
            var extension = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(extension)) return;

            var isPdf = string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
            var isRaster = string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
            if (!isPdf && !isRaster) return;

            if (sourceKind == DrawingSheetSourceKind.Pdf && !isPdf)
                throw new ArgumentException("PDF sheet ingestion cannot use a recognized raster-image source reference.", "sourceReference");
            if (sourceKind == DrawingSheetSourceKind.RasterImage && !isRaster)
                throw new ArgumentException("Raster sheet ingestion cannot use a recognized PDF source reference.", "sourceReference");
        }

        private static string SourcePath(string sourceReference)
        {
            Uri uri;
            if (Uri.TryCreate(sourceReference, UriKind.Absolute, out uri) && !string.IsNullOrEmpty(uri.AbsolutePath))
                return uri.AbsolutePath;

            var end = sourceReference.Length;
            var query = sourceReference.IndexOf('?');
            if (query >= 0 && query < end) end = query;
            var fragment = sourceReference.IndexOf('#');
            if (fragment >= 0 && fragment < end) end = fragment;
            return sourceReference.Substring(0, end);
        }

        private static void ValidatePdfPayload(byte[] payload)
        {
            if (payload.Length < 9 ||
                payload[0] != (byte)'%' || payload[1] != (byte)'P' || payload[2] != (byte)'D' || payload[3] != (byte)'F' || payload[4] != (byte)'-' ||
                !IsSupportedPdfVersion(payload[5], payload[7]) || payload[6] != (byte)'.' ||
                !IsPdfLineTerminator(payload[8]))
                throw new InvalidOperationException("PDF payload header is invalid, unsupported, or not terminated by an end-of-line marker.");

            var end = payload.Length - 1;
            while (end >= 0 && IsPdfWhitespace(payload[end])) end--;
            var markerStart = end - PdfEofMarker.Length + 1;
            if (markerStart < 0)
                throw new InvalidOperationException("PDF payload is truncated or missing the terminal %%EOF marker.");

            for (var i = 0; i < PdfEofMarker.Length; i++)
            {
                if (payload[markerStart + i] != PdfEofMarker[i])
                    throw new InvalidOperationException("PDF payload is truncated or missing the terminal %%EOF marker.");
            }

            if (markerStart == 0 || !IsPdfLineTerminator(payload[markerStart - 1]))
                throw new InvalidOperationException("PDF terminal %%EOF marker must begin on its own line.");
        }

        private static bool IsSupportedPdfVersion(byte major, byte minor)
        {
            return (major == (byte)'1' && minor >= (byte)'0' && minor <= (byte)'7') ||
                (major == (byte)'2' && minor == (byte)'0');
        }

        private static bool IsPdfLineTerminator(byte value)
        {
            return value == 10 || value == 13;
        }

        private static bool IsPdfWhitespace(byte value)
        {
            return value == 0 || value == 9 || value == 10 || value == 12 || value == 13 || value == 32;
        }

        private static string Sha256(byte[] payload)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(payload);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static bool TryPng(byte[] payload, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (payload.Length < PngSignature.Length) return false;
            for (var i = 0; i < PngSignature.Length; i++) if (payload[i] != PngSignature[i]) return false;

            if (payload.Length < 33)
                throw new InvalidOperationException("PNG payload is truncated before the complete IHDR chunk.");
            if (ReadInt32BigEndian(payload, 8) != 13)
                throw new InvalidOperationException("PNG first chunk must be an IHDR chunk with length 13.");
            if (payload[12] != (byte)'I' || payload[13] != (byte)'H' || payload[14] != (byte)'D' || payload[15] != (byte)'R')
                throw new InvalidOperationException("PNG first chunk must be IHDR.");

            ValidatePngChunkCrc(payload, 12, 13, "IHDR");
            ValidatePngIhdrSemantics(payload);
            width = ReadInt32BigEndian(payload, 16);
            height = ReadInt32BigEndian(payload, 20);
            if (width <= 0 || height <= 0) throw new InvalidOperationException("PNG dimensions are invalid.");
            return true;
        }

        private static void ValidatePngIhdrSemantics(byte[] payload)
        {
            var bitDepth = payload[24];
            var colorType = payload[25];
            var compressionMethod = payload[26];
            var filterMethod = payload[27];
            var interlaceMethod = payload[28];

            if (!IsSupportedPngBitDepth(colorType, bitDepth))
                throw new InvalidOperationException("PNG IHDR color type and bit depth combination is invalid.");
            if (compressionMethod != 0)
                throw new InvalidOperationException("PNG IHDR compression method must be 0.");
            if (filterMethod != 0)
                throw new InvalidOperationException("PNG IHDR filter method must be 0.");
            if (interlaceMethod > 1)
                throw new InvalidOperationException("PNG IHDR interlace method must be 0 or 1.");
        }

        private static bool IsSupportedPngBitDepth(byte colorType, byte bitDepth)
        {
            if (colorType == 0) return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16;
            if (colorType == 2) return bitDepth == 8 || bitDepth == 16;
            if (colorType == 3) return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8;
            if (colorType == 4 || colorType == 6) return bitDepth == 8 || bitDepth == 16;
            return false;
        }

        private static void ValidatePngChunkCrc(byte[] payload, int typeOffset, int dataLength, string chunkName)
        {
            var crcOffset = typeOffset + 4 + dataLength;
            if (typeOffset < 0 || dataLength < 0 || crcOffset < typeOffset || crcOffset + 4 > payload.Length)
                throw new InvalidOperationException("PNG " + chunkName + " chunk is truncated before its CRC.");

            uint crc = 0xffffffffu;
            for (var i = typeOffset; i < crcOffset; i++)
            {
                crc ^= payload[i];
                for (var bit = 0; bit < 8; bit++)
                    crc = (crc >> 1) ^ ((crc & 1u) != 0u ? 0xedb88320u : 0u);
            }
            crc ^= 0xffffffffu;

            if (crc != ReadUInt32BigEndian(payload, crcOffset))
                throw new InvalidOperationException("PNG " + chunkName + " chunk CRC is invalid.");
        }

        private static void ValidatePngChunkStream(byte[] payload)
        {
            var offset = 8;
            var sawIhdr = false;
            var sawIdat = false;
            var sawIend = false;

            while (offset < payload.Length)
            {
                if (payload.Length - offset < 12)
                    throw new InvalidOperationException("PNG chunk stream is truncated before a complete chunk header and CRC.");

                var lengthValue = ReadUInt32BigEndian(payload, offset);
                if (lengthValue > int.MaxValue)
                    throw new InvalidOperationException("PNG chunk length exceeds supported bounds.");
                var dataLength = (int)lengthValue;
                var typeOffset = offset + 4;
                var dataOffset = typeOffset + 4;
                var crcOffset = dataOffset + dataLength;
                if (crcOffset < dataOffset || crcOffset > payload.Length - 4)
                    throw new InvalidOperationException("PNG chunk length exceeds the remaining payload.");

                var c0 = payload[typeOffset];
                var c1 = payload[typeOffset + 1];
                var c2 = payload[typeOffset + 2];
                var c3 = payload[typeOffset + 3];
                var isIhdr = c0 == (byte)'I' && c1 == (byte)'H' && c2 == (byte)'D' && c3 == (byte)'R';
                var isIdat = c0 == (byte)'I' && c1 == (byte)'D' && c2 == (byte)'A' && c3 == (byte)'T';
                var isIend = c0 == (byte)'I' && c1 == (byte)'E' && c2 == (byte)'N' && c3 == (byte)'D';
                var chunkName = new string(new[] { (char)c0, (char)c1, (char)c2, (char)c3 });

                if (!sawIhdr)
                {
                    if (!isIhdr || dataLength != 13)
                        throw new InvalidOperationException("PNG first chunk must be a single IHDR chunk with length 13.");
                    sawIhdr = true;
                }
                else if (isIhdr)
                    throw new InvalidOperationException("PNG chunk stream contains a duplicate IHDR chunk.");

                if (isIdat) sawIdat = true;
                if (isIend)
                {
                    if (dataLength != 0)
                        throw new InvalidOperationException("PNG IEND chunk length must be zero.");
                    if (!sawIdat)
                        throw new InvalidOperationException("PNG chunk stream must contain IDAT image data before IEND.");
                    sawIend = true;
                }

                ValidatePngChunkCrc(payload, typeOffset, dataLength, chunkName);
                offset = crcOffset + 4;

                if (sawIend)
                {
                    if (offset != payload.Length)
                        throw new InvalidOperationException("PNG payload contains data after the terminal IEND chunk.");
                    return;
                }
            }

            throw new InvalidOperationException("PNG payload is truncated or missing the terminal IEND chunk.");
        }

        private static bool TryJpeg(byte[] payload, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (payload.Length < 4 || payload[0] != 0xFF || payload[1] != 0xD8) return false;
            var offset = 2;
            while (offset + 1 < payload.Length)
            {
                if (payload[offset] != 0xFF)
                    throw new InvalidOperationException("JPEG marker stream contains data outside a length-delimited segment before SOF.");

                while (offset < payload.Length && payload[offset] == 0xFF) offset++;
                if (offset >= payload.Length) break;
                var marker = payload[offset++];

                if (marker == 0x00)
                    throw new InvalidOperationException("JPEG byte-stuffing marker is invalid before entropy-coded scan data.");
                if (marker == 0xD8)
                    throw new InvalidOperationException("JPEG payload contains a nested SOI marker before SOF.");
                if (IsRestartMarker(marker))
                    throw new InvalidOperationException("JPEG restart markers are invalid before entropy-coded scan data.");
                if (marker == 0xD9)
                    throw new InvalidOperationException("JPEG payload reached EOI before a supported SOF marker.");
                if (marker == 0xDA)
                    throw new InvalidOperationException("JPEG payload reached SOS before a supported SOF marker.");
                if (marker == 0x01)
                    continue;

                if (offset + 1 >= payload.Length)
                    throw new InvalidOperationException("JPEG segment is truncated before its length field.");
                var length = (payload[offset] << 8) | payload[offset + 1];
                if (length < 2 || offset + length > payload.Length) throw new InvalidOperationException("JPEG segment length is invalid.");
                if (IsStartOfFrame(marker))
                {
                    ValidateJpegFrameHeader(payload, offset, length);
                    height = (payload[offset + 3] << 8) | payload[offset + 4];
                    width = (payload[offset + 5] << 8) | payload[offset + 6];
                    if (width <= 0 || height <= 0) throw new InvalidOperationException("JPEG dimensions are invalid.");
                    return true;
                }
                offset += length;
            }
            throw new InvalidOperationException("JPEG payload has no supported frame dimensions.");
        }

        private static void ValidateJpegFrameHeader(byte[] payload, int offset, int length)
        {
            if (length < 8) throw new InvalidOperationException("JPEG frame header is invalid.");
            if (payload[offset + 2] == 0) throw new InvalidOperationException("JPEG frame sample precision is invalid.");

            var componentCount = payload[offset + 7];
            if (componentCount == 0) throw new InvalidOperationException("JPEG frame must declare at least one component.");
            var expectedLength = 8 + (3 * componentCount);
            if (length != expectedLength)
                throw new InvalidOperationException("JPEG frame length does not match its component count.");

            var seenComponentIds = new bool[256];
            for (var componentIndex = 0; componentIndex < componentCount; componentIndex++)
            {
                var descriptorOffset = offset + 8 + (3 * componentIndex);
                var componentId = payload[descriptorOffset];
                if (componentId == 0 || seenComponentIds[componentId])
                    throw new InvalidOperationException("JPEG frame component identifiers must be non-zero and unique.");
                seenComponentIds[componentId] = true;

                var samplingFactors = payload[descriptorOffset + 1];
                var horizontalSampling = (samplingFactors >> 4) & 0x0F;
                var verticalSampling = samplingFactors & 0x0F;
                if (horizontalSampling < 1 || horizontalSampling > 4 || verticalSampling < 1 || verticalSampling > 4)
                    throw new InvalidOperationException("JPEG frame sampling factors must be between 1 and 4.");

                var quantizationTable = payload[descriptorOffset + 2];
                if (quantizationTable > 3)
                    throw new InvalidOperationException("JPEG frame quantization-table selector must be between 0 and 3.");
            }
        }

        private static void ValidateJpegTerminator(byte[] payload)
        {
            if (payload.Length < 2 || payload[payload.Length - 2] != 0xFF || payload[payload.Length - 1] != 0xD9)
                throw new InvalidOperationException("JPEG payload is truncated or missing the terminal EOI marker.");
        }

        private static bool IsRestartMarker(byte marker)
        {
            return marker >= 0xD0 && marker <= 0xD7;
        }

        private static bool IsStartOfFrame(byte marker)
        {
            return marker == 0xC0 || marker == 0xC1 || marker == 0xC2 || marker == 0xC3 || marker == 0xC5 || marker == 0xC6 || marker == 0xC7 || marker == 0xC9 || marker == 0xCA || marker == 0xCB || marker == 0xCD || marker == 0xCE || marker == 0xCF;
        }

        private static int ReadInt32BigEndian(byte[] payload, int offset)
        {
            return (payload[offset] << 24) | (payload[offset + 1] << 16) | (payload[offset + 2] << 8) | payload[offset + 3];
        }

        private static uint ReadUInt32BigEndian(byte[] payload, int offset)
        {
            return ((uint)payload[offset] << 24) | ((uint)payload[offset + 1] << 16) | ((uint)payload[offset + 2] << 8) | payload[offset + 3];
        }
    }
}
