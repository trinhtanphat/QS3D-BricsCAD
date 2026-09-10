using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;

namespace QS3D.Core.Export
{
    public static class XlsxRebarScheduleExporter
    {
        private const int MaxWorksheetRows = 1048576;
        private const int HeaderRows = 1;
        private const int MaxDataRows = MaxWorksheetRows - HeaderRows;
        private const int MaxCellTextLength = 32767;
        private const long MaxWorksheetEntryBytes = 32L * 1024L * 1024L;
        private const long MaxAggregateUncompressedBytes = 64L * 1024L * 1024L;
        private const long MaxArchiveBytes = 64L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly DateTimeOffset FixedZipTimestamp = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static void Export(string path, IReadOnlyList<RebarScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var snapshot = SnapshotRows(rows);
            var rowCount = snapshot.Count;
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                long totalUncompressedBytes = 0L;
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var boundedArchive = new BoundedArchiveWriteStream(stream, MaxArchiveBytes))
                using (var archive = new ZipArchive(boundedArchive, ZipArchiveMode.Create, true, StrictUtf8))
                {
                    WriteEntry(archive, "[Content_Types].xml", ContentTypesXml, ref totalUncompressedBytes);
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml, ref totalUncompressedBytes);
                    WriteEntry(archive, "xl/workbook.xml", WorkbookXml, ref totalUncompressedBytes);
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml, ref totalUncompressedBytes);
                    WriteEntry(archive, "xl/styles.xml", StylesXml, ref totalUncompressedBytes);
                    WriteWorksheetEntry(archive, snapshot, rowCount, ref totalUncompressedBytes);
                }
                if (new FileInfo(tempPath).Length > MaxArchiveBytes)
                    throw new InvalidDataException("Rebar XLSX archive exceeds the bounded output contract.");
                ValidatePackage(tempPath);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static IReadOnlyList<RebarScheduleRow> SnapshotRows(IReadOnlyList<RebarScheduleRow> rows)
        {
            var count = rows.Count;
            if (count < 0 || count > MaxDataRows)
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    count,
                    "BBS XLSX data rows must be between 0 and " + MaxDataRows.ToString(CultureInfo.InvariantCulture) + " so the worksheet stays within its row limit.");

            var snapshot = new List<RebarScheduleRow>(count);
            for (var index = 0; index < count; index++)
            {
                var source = rows[index];
                if (source == null)
                    throw new ArgumentException(
                        "BBS row cannot be null. Invalid row index: " + index.ToString(CultureInfo.InvariantCulture) + ".",
                        nameof(rows));

                var row = new RebarScheduleRow
                {
                    ElementId = source.ElementId ?? string.Empty,
                    BarMark = source.BarMark ?? string.Empty,
                    ShapeCode = source.ShapeCode ?? string.Empty,
                    Notation = source.Notation ?? string.Empty,
                    DiameterMm = source.DiameterMm,
                    Quantity = source.Quantity,
                    CuttingLengthM = source.CuttingLengthM,
                    TotalLengthM = source.TotalLengthM,
                    UnitWeightKgM = source.UnitWeightKgM,
                    NetWeightKg = source.NetWeightKg,
                    WastePercent = source.WastePercent,
                    TotalWeightKg = source.TotalWeightKg,
                    FabricationStatus = source.FabricationStatus ?? string.Empty,
                    FabricationStandardCode = source.FabricationStandardCode ?? string.Empty,
                    FabricationDetailingRevision = source.FabricationDetailingRevision ?? string.Empty
                };

                ValidateCellText(row.ElementId, index, "Element");
                ValidateElementIdProvenance(row.ElementId, index);
                ValidateCellText(row.BarMark, index, "Bar Mark");
                ValidateCellText(row.ShapeCode, index, "Shape");
                ValidateCellText(row.Notation, index, "Notation");
                ValidateCellText(row.FabricationStatus, index, "Fabrication Status");
                ValidateCellText(row.FabricationStandardCode, index, "Standard Code");
                ValidateCellText(row.FabricationDetailingRevision, index, "Detailing Revision");
                ValidatePositive(row.DiameterMm, index, "DiameterMm");
                ValidatePositive(row.Quantity, index, "Quantity");
                ValidateNonNegative(row.CuttingLengthM, index, "CuttingLengthM");
                ValidateNonNegative(row.TotalLengthM, index, "TotalLengthM");
                ValidateNonNegative(row.UnitWeightKgM, index, "UnitWeightKgM");
                ValidateNonNegative(row.NetWeightKg, index, "NetWeightKg");
                ValidateNonNegative(row.WastePercent, index, "WastePercent");
                ValidateNonNegative(row.TotalWeightKg, index, "TotalWeightKg");
                snapshot.Add(row);
            }
            if (rows.Count != count)
                throw new InvalidOperationException("Rebar XLSX export row count changed during snapshot.");
            return snapshot;
        }

        private static void ValidateCellText(string value, int rowIndex, string field)
        {
            if ((value ?? string.Empty).Length <= MaxCellTextLength) return;
            throw new ArgumentOutOfRangeException(
                "rows",
                "BBS XLSX worksheet row " + (rowIndex + HeaderRows + 1).ToString(CultureInfo.InvariantCulture) +
                " field '" + field + "' exceeds Excel's " + MaxCellTextLength.ToString(CultureInfo.InvariantCulture) + "-character cell text limit.");
        }

        private static void ValidateElementIdProvenance(string value, int rowIndex)
        {
            var safe = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(safe))
                throw InvalidElementId(rowIndex, "is required.");
            if (!string.Equals(safe, safe.Trim(), StringComparison.Ordinal))
                throw InvalidElementId(rowIndex, "must not contain leading or trailing whitespace.");
            foreach (var ch in safe)
            {
                if (char.IsControl(ch))
                    throw InvalidElementId(rowIndex, "must not contain control characters.");
            }
            try
            {
                XmlConvert.VerifyXmlChars(safe);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(
                    "BBS XLSX worksheet row " + (rowIndex + HeaderRows + 1).ToString(CultureInfo.InvariantCulture) +
                    " field 'Element' contains characters that are invalid in XML provenance.",
                    "rows",
                    ex);
            }
        }

        private static ArgumentException InvalidElementId(int rowIndex, string reason)
        {
            return new ArgumentException(
                "BBS XLSX worksheet row " + (rowIndex + HeaderRows + 1).ToString(CultureInfo.InvariantCulture) +
                " field 'Element' " + reason,
                "rows");
        }

        private static void ValidatePositive(double value, int rowIndex, string field)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value) && value > 0d) return;
            throw new ArgumentOutOfRangeException(
                "rows",
                "BBS XLSX worksheet row " + (rowIndex + HeaderRows + 1).ToString(CultureInfo.InvariantCulture) +
                " field '" + field + "' must be finite and greater than zero.");
        }

        private static void ValidatePositive(int value, int rowIndex, string field)
        {
            if (value > 0) return;
            throw new ArgumentOutOfRangeException(
                "rows",
                "BBS XLSX worksheet row " + (rowIndex + HeaderRows + 1).ToString(CultureInfo.InvariantCulture) +
                " field '" + field + "' must be greater than zero.");
        }

        private static void ValidateNonNegative(double value, int rowIndex, string field)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d) return;
            throw new ArgumentOutOfRangeException(
                "rows",
                "BBS XLSX worksheet row " + (rowIndex + HeaderRows + 1).ToString(CultureInfo.InvariantCulture) +
                " field '" + field + "' must be finite and non-negative.");
        }

        private static void WriteWorksheetEntry(ZipArchive archive, IReadOnlyList<RebarScheduleRow> rows, int rowCount, ref long totalUncompressedBytes)
        {
            using (var buffer = new MemoryStream())
            {
                using (var boundedEntry = new BoundedEntryWriteStream(buffer, MaxWorksheetEntryBytes))
                using (var writer = new StreamWriter(boundedEntry, StrictUtf8, 4096, true))
                    WriteSheet(writer, rows, rowCount);
                ReserveUncompressed(buffer.Length, ref totalUncompressedBytes, "xl/worksheets/sheet1.xml");
                buffer.Position = 0L;
                var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
                entry.LastWriteTime = FixedZipTimestamp;
                using (var target = entry.Open()) buffer.CopyTo(target);
            }
        }

        private static void WriteSheet(TextWriter writer, IReadOnlyList<RebarScheduleRow> rows, int rowCount)
        {
            var headers = new[]
            {
                "Element", "Bar Mark", "Shape", "Notation", "Ø (mm)", "SL", "L cắt (m)", "Tổng L (m)",
                "kg/m", "KL net (kg)", "Hao hụt (%)", "KL tổng (kg)",
                "Fabrication Status", "Standard Code", "Detailing Revision"
            };
            var lastRow = Math.Max(1, rowCount + HeaderRows);
            var range = "A1:O" + lastRow.ToString(CultureInfo.InvariantCulture);
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"");
            writer.Write(range);
            writer.Write("\"/>");
            writer.Write("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetData>");
            writer.Write("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) AppendText(writer, CellRef(c, 1), headers[c], 1);
            writer.Write("</row>");
            for (var i = 0; i < rowCount; i++)
            {
                var row = rows[i] ?? throw new ArgumentException("BBS row cannot be null.", nameof(rows));
                var r = i + 2;
                writer.Write("<row r=\""); writer.Write(r.ToString(CultureInfo.InvariantCulture)); writer.Write("\">");
                AppendText(writer, CellRef(0, r), row.ElementId, 0);
                AppendText(writer, CellRef(1, r), row.BarMark, 0);
                AppendText(writer, CellRef(2, r), row.ShapeCode, 0);
                AppendText(writer, CellRef(3, r), row.Notation, 0);
                AppendNumber(writer, CellRef(4, r), row.DiameterMm);
                AppendNumber(writer, CellRef(5, r), row.Quantity);
                AppendNumber(writer, CellRef(6, r), row.CuttingLengthM);
                AppendNumber(writer, CellRef(7, r), row.TotalLengthM);
                AppendNumber(writer, CellRef(8, r), row.UnitWeightKgM);
                AppendNumber(writer, CellRef(9, r), row.NetWeightKg);
                AppendNumber(writer, CellRef(10, r), row.WastePercent);
                AppendNumber(writer, CellRef(11, r), row.TotalWeightKg);
                AppendText(writer, CellRef(12, r), row.FabricationStatus, 0);
                AppendText(writer, CellRef(13, r), row.FabricationStandardCode, 0);
                AppendText(writer, CellRef(14, r), row.FabricationDetailingRevision, 0);
                writer.Write("</row>");
            }
            writer.Write("</sheetData><autoFilter ref=\""); writer.Write(range); writer.Write("\"/></worksheet>");
        }

        private static void ValidatePackage(string path)
        {
            XlsxPackageValidator.Validate(path, "[Content_Types].xml", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml");
        }

        private static void AppendText(TextWriter writer, string cellRef, string value, int style)
        {
            writer.Write("<c r=\""); writer.Write(cellRef); writer.Write("\" t=\"inlineStr\" s=\"");
            writer.Write(style.ToString(CultureInfo.InvariantCulture)); writer.Write("\"><is><t>");
            writer.Write(XlsxXmlText.Escape(value ?? string.Empty)); writer.Write("</t></is></c>");
        }

        private static void AppendNumber(TextWriter writer, string cellRef, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "XLSX numeric values must be finite.");
            var formatted = value == 0d ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
            writer.Write("<c r=\""); writer.Write(cellRef); writer.Write("\" s=\"2\"><v>"); writer.Write(formatted); writer.Write("</v></c>");
        }

        private static string CellRef(int columnZeroBased, int row)
        {
            var n = columnZeroBased + 1; var name = string.Empty;
            while (n > 0) { n--; name = (char)('A' + n % 26) + name; n /= 26; }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content, ref long totalUncompressedBytes)
        {
            var bytes = StrictUtf8.GetBytes(content);
            ReserveUncompressed(bytes.LongLength, ref totalUncompressedBytes, name);
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = FixedZipTimestamp;
            using (var target = entry.Open()) target.Write(bytes, 0, bytes.Length);
        }

        private static void ReserveUncompressed(long entryBytes, ref long totalUncompressedBytes, string name)
        {
            if (entryBytes < 0L || entryBytes > MaxWorksheetEntryBytes)
                throw new InvalidDataException("Rebar XLSX worksheet exceeds the bounded output contract: " + name + ".");
            var projected = checked(totalUncompressedBytes + entryBytes);
            if (projected > MaxAggregateUncompressedBytes)
                throw new InvalidDataException("Rebar XLSX aggregate uncompressed XML exceeds the bounded output contract.");
            totalUncompressedBytes = projected;
        }

        private sealed class BoundedEntryWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;
            internal BoundedEntryWriteStream(Stream inner, long maxLength) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); _maxLength = maxLength; }
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true; public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush(); public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException(); public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(); public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) { if (checked(_inner.Position + count) > _maxLength) throw new InvalidDataException("Rebar XLSX worksheet exceeds the bounded output contract."); _inner.Write(buffer, offset, count); }
            public override void WriteByte(byte value) { if (checked(_inner.Position + 1L) > _maxLength) throw new InvalidDataException("Rebar XLSX worksheet exceeds the bounded output contract."); _inner.WriteByte(value); }
        }

        private sealed class BoundedArchiveWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;
            internal BoundedArchiveWriteStream(Stream inner, long maxLength) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); _maxLength = maxLength; }
            public override bool CanRead => _inner.CanRead; public override bool CanSeek => _inner.CanSeek; public override bool CanWrite => _inner.CanWrite; public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush(); public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count); public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin); public override void SetLength(long value) { if (value > _maxLength) throw new InvalidDataException("Rebar XLSX archive exceeds the bounded output contract."); _inner.SetLength(value); }
            public override void Write(byte[] buffer, int offset, int count) { var projected = checked(_inner.Position + count); if (projected > _maxLength) throw new InvalidDataException("Rebar XLSX archive exceeds the bounded output contract."); _inner.Write(buffer, offset, count); }
            public override void WriteByte(byte value) { if (checked(_inner.Position + 1L) > _maxLength) throw new InvalidDataException("Rebar XLSX archive exceeds the bounded output contract."); _inner.WriteByte(value); }
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"BBS\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}