using System;
using System.Globalization;
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

        public IngestedDrawingSheet2D IngestPdf(string id, string name, string sourceReference, string revision, DrawingCalibration calibration, byte[] payload, int pageNumber)
        {
            RequirePayload(payload);
            if (pageNumber < 1) throw new ArgumentOutOfRangeException("pageNumber");
            if (payload.Length < 5 || payload[0] != (byte)'%' || payload[1] != (byte)'P' || payload[2] != (byte)'D' || payload[3] != (byte)'F' || payload[4] != (byte)'-')
                throw new InvalidOperationException("PDF payload signature is invalid.");

            var sheet = new DrawingSheet2D(id, name, DrawingSheetSourceKind.Pdf, sourceReference, revision, calibration);
            return new IngestedDrawingSheet2D(sheet, Sha256(payload), payload.Length, pageNumber, null, 0, 0);
        }

        public IngestedDrawingSheet2D IngestRaster(string id, string name, string sourceReference, string revision, DrawingCalibration calibration, byte[] payload)
        {
            RequirePayload(payload);
            RasterSheetFormat format;
            int width;
            int height;
            if (TryPng(payload, out width, out height)) format = RasterSheetFormat.Png;
            else if (TryJpeg(payload, out width, out height)) format = RasterSheetFormat.Jpeg;
            else throw new InvalidOperationException("Raster payload must be a supported PNG or JPEG image.");

            var sheet = new DrawingSheet2D(id, name, DrawingSheetSourceKind.RasterImage, sourceReference, revision, calibration);
            return new IngestedDrawingSheet2D(sheet, Sha256(payload), payload.Length, 0, format, width, height);
        }

        private static void RequirePayload(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            if (payload.Length == 0) throw new ArgumentException("Sheet payload cannot be empty.", "payload");
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
            if (payload.Length < 24) return false;
            for (var i = 0; i < PngSignature.Length; i++) if (payload[i] != PngSignature[i]) return false;
            width = ReadInt32BigEndian(payload, 16);
            height = ReadInt32BigEndian(payload, 20);
            if (width <= 0 || height <= 0) throw new InvalidOperationException("PNG dimensions are invalid.");
            return true;
        }

        private static bool TryJpeg(byte[] payload, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (payload.Length < 4 || payload[0] != 0xFF || payload[1] != 0xD8) return false;
            var offset = 2;
            while (offset + 3 < payload.Length)
            {
                if (payload[offset] != 0xFF) { offset++; continue; }
                while (offset < payload.Length && payload[offset] == 0xFF) offset++;
                if (offset >= payload.Length) break;
                var marker = payload[offset++];
                if (marker == 0xD9 || marker == 0xDA) break;
                if (offset + 1 >= payload.Length) break;
                var length = (payload[offset] << 8) | payload[offset + 1];
                if (length < 2 || offset + length > payload.Length) throw new InvalidOperationException("JPEG segment length is invalid.");
                if (IsStartOfFrame(marker))
                {
                    if (length < 7) throw new InvalidOperationException("JPEG frame header is invalid.");
                    height = (payload[offset + 3] << 8) | payload[offset + 4];
                    width = (payload[offset + 5] << 8) | payload[offset + 6];
                    if (width <= 0 || height <= 0) throw new InvalidOperationException("JPEG dimensions are invalid.");
                    return true;
                }
                offset += length;
            }
            throw new InvalidOperationException("JPEG payload has no supported frame dimensions.");
        }

        private static bool IsStartOfFrame(byte marker)
        {
            return marker == 0xC0 || marker == 0xC1 || marker == 0xC2 || marker == 0xC3 || marker == 0xC5 || marker == 0xC6 || marker == 0xC7 || marker == 0xC9 || marker == 0xCA || marker == 0xCB || marker == 0xCD || marker == 0xCE || marker == 0xCF;
        }

        private static int ReadInt32BigEndian(byte[] payload, int offset)
        {
            return (payload[offset] << 24) | (payload[offset + 1] << 16) | (payload[offset + 2] << 8) | payload[offset + 3];
        }
    }
}
