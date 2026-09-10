using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    public static class MaterialUsageXlsxExporter
    {
        private const int MaxDataRows = 1048575;
        private const int MaxCellTextCharacters = 32767;
        private const string ProvenanceSeparator = " | ";
        private const long MaxWorksheetEntryBytes = 32L * 1024L * 1024L;
        private const long MaxAggregateUncompressedBytes = 64L * 1024L * 1024L;
        private const long MaxArchiveBytes = 64L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly DateTimeOffset FixedZipTimestamp = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private sealed class ExportRow
        {
            public string Floor { get; set; } = string.Empty;
            public string MaterialName { get; set; } = string.Empty;
            public string UnitHint { get; set; } = string.Empty;
            public string Component { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string FamilyName { get; set; } = string.Empty;
            public int ElementCount { get; set; }
            public int ElementIdCount { get; set; }
            public double PrimaryQuantity { get; set; }
            public double LengthM { get; set; }
            public double AreaM2 { get; set; }
            public double VolumeM3 { get; set; }
            public double MassKg { get; set; }
            public string ProjectId { get; set; } = string.Empty;
            public string DrawingFingerprint { get; set; } = string.Empty;
            public string ElementIds { get; set; } = string.Empty;
            public string SourceHandles { get; set; } = string.Empty;
            public List<string> ElementIdValues { get; set; } = new List<string>();
            public List<string> SourceHandleValues { get; set; } = new List<string>();
        }

        public static void Export(string path, IReadOnlyList<MaterialUsageRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var rowCount = BindRowCount(rows);
            var snapshot = new List<ExportRow>(rowCount);
            var sourceRows = new List<MaterialUsageRow>(rowCount);
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var sourceRow = rows[rowIndex];
                if (sourceRow == null)
                    throw new ArgumentException("Export rows cannot contain null entries. Invalid row index: " + rowIndex + ".", nameof(rows));
                var row = SnapshotRow(sourceRow, rowIndex);
                ValidateCellText(row.Floor, rowIndex, "Floor");
                ValidateCellText(row.MaterialName, rowIndex, "MaterialName");
                ValidateCellText(row.UnitHint, rowIndex, "UnitHint");
                ValidateCellText(row.Component, rowIndex, "Component");
                ValidateCellText(row.Category, rowIndex, "Category");
                ValidateCellText(row.FamilyName, rowIndex, "FamilyName");
                ValidateProvenanceText(row.ProjectId, rowIndex, "ProjectId");
                ValidateProvenanceText(row.DrawingFingerprint, rowIndex, "DrawingFingerprint");
                ValidateProvenanceText(row.ElementIds, rowIndex, "ElementIds");
                ValidateProvenanceText(row.SourceHandles, rowIndex, "SourceHandles");
                ValidateCount(row.ElementCount, rowIndex, "ElementCount");
                ValidateElementCount(row.ElementCount, row.ElementIdCount, rowIndex);
                ValidateNonNegative(row.PrimaryQuantity, rowIndex, "PrimaryQuantity");
                ValidateNonNegative(row.LengthM, rowIndex, "LengthM");
                ValidateNonNegative(row.AreaM2, rowIndex, "AreaM2");
                ValidateNonNegative(row.VolumeM3, rowIndex, "VolumeM3");
                ValidateNonNegative(row.MassKg, rowIndex, "MassKg");
                sourceRows.Add(sourceRow);
                snapshot.Add(row);
            }
            if (BindRowCount(rows) != rowCount)
                throw new InvalidOperationException("Material XLSX source row count changed during snapshot traversal.");
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
                EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);
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
                    WriteWorksheetEntry(archive, snapshot, ref totalUncompressedBytes);
                }
                if (new FileInfo(tempPath).Length > MaxArchiveBytes) throw new InvalidDataException("Material XLSX archive exceeds the bounded output contract.");
                Validate(tempPath);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally { AtomicFileCommit.TryDelete(tempPath); }
        }

        private static int BindRowCount(IReadOnlyList<MaterialUsageRow> rows)
        {
            int? expected = null;
            BindKnownCount(rows.Count, "IReadOnlyCollection<MaterialUsageRow>", ref expected);
            var genericCollection = rows as ICollection<MaterialUsageRow>;
            if (genericCollection != null)
                BindKnownCount(genericCollection.Count, "ICollection<MaterialUsageRow>", ref expected);
            var nonGenericCollection = rows as ICollection;
            if (nonGenericCollection != null)
                BindKnownCount(nonGenericCollection.Count, "ICollection", ref expected);
            return expected.GetValueOrDefault();
        }

        private static void BindKnownCount(int count, string contract, ref int? expected)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("rows", "Material XLSX source " + contract + " Count must be non-negative.");
            if (count > MaxDataRows)
                throw new ArgumentOutOfRangeException("rows", "Material XLSX export supports at most " + MaxDataRows + " data rows.");
            if (expected.HasValue && expected.Value != count)
                throw new ArgumentException("Material XLSX source exposes conflicting deterministic Count contracts.", "rows");
            expected = count;
        }

        private static void WriteWorksheetEntry(ZipArchive archive, IReadOnlyList<ExportRow> rows, ref long totalUncompressedBytes)
        {
            using (var buffer = new MemoryStream())
            {
                using (var boundedEntry = new BoundedEntryWriteStream(buffer, MaxWorksheetEntryBytes))
                using (var writer = new StreamWriter(boundedEntry, StrictUtf8, 4096, true)) WriteSheet(writer, rows);
                ReserveUncompressed(buffer.Length, ref totalUncompressedBytes, "xl/worksheets/sheet1.xml");
                buffer.Position = 0L;
                var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
                entry.LastWriteTime = FixedZipTimestamp;
                using (var target = entry.Open()) buffer.CopyTo(target);
            }
        }

        private static void WriteSheet(TextWriter writer, IReadOnlyList<ExportRow> rows)
        {
            var headers = new[]
            {
                "Tầng", "Vật liệu", "Đơn vị", "Thành phần", "Loại cấu kiện", "Family / Loại",
                "SL cấu kiện", "KL chính", "Dài (m)", "Diện tích (m²)", "Thể tích (m³)", "Khối lượng (kg)",
                "Project ID", "Drawing fingerprint", "Element IDs", "Source Handles"
            };
            var lastRow = Math.Max(1, rows.Count + 1);
            var range = "A1:P" + lastRow.ToString(CultureInfo.InvariantCulture);
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            writer.Write("<dimension ref=\""); writer.Write(range); writer.Write("\"/>");
            writer.Write("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            writer.Write("<cols><col min=\"1\" max=\"6\" width=\"20\" customWidth=\"1\"/><col min=\"7\" max=\"12\" width=\"16\" customWidth=\"1\"/><col min=\"13\" max=\"16\" width=\"28\" customWidth=\"1\"/></cols><sheetData>");
            writer.Write("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) StringCell(writer, CellRef(c, 1), headers[c], 1);
            writer.Write("</row>");
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var r = index + 2;
                writer.Write("<row r=\""); writer.Write(r.ToString(CultureInfo.InvariantCulture)); writer.Write("\">");
                StringCell(writer, CellRef(0, r), row.Floor, 0);
                StringCell(writer, CellRef(1, r), row.MaterialName, 0);
                StringCell(writer, CellRef(2, r), row.UnitHint, 0);
                StringCell(writer, CellRef(3, r), row.Component, 0);
                StringCell(writer, CellRef(4, r), row.Category, 0);
                StringCell(writer, CellRef(5, r), row.FamilyName, 0);
                NumberCell(writer, CellRef(6, r), row.ElementCount);
                NumberCell(writer, CellRef(7, r), row.PrimaryQuantity);
                NumberCell(writer, CellRef(8, r), row.LengthM);
                NumberCell(writer, CellRef(9, r), row.AreaM2);
                NumberCell(writer, CellRef(10, r), row.VolumeM3);
                NumberCell(writer, CellRef(11, r), row.MassKg);
                StringCell(writer, CellRef(12, r), row.ProjectId, 0);
                StringCell(writer, CellRef(13, r), row.DrawingFingerprint, 0);
                StringCell(writer, CellRef(14, r), row.ElementIds, 0);
                StringCell(writer, CellRef(15, r), row.SourceHandles, 0);
                writer.Write("</row>");
            }
            writer.Write("</sheetData><autoFilter ref=\""); writer.Write(range); writer.Write("\"/></worksheet>");
        }

        private static void Validate(string path)
        {
            XlsxPackageValidator.Validate(
                path,
                "[Content_Types].xml",
                "_rels/.rels",
                "xl/workbook.xml",
                "xl/_rels/workbook.xml.rels",
                "xl/styles.xml",
                "xl/worksheets/sheet1.xml");
        }

        private static ExportRow SnapshotRow(MaterialUsageRow row, int rowIndex)
        {
            var elementIdValues = SnapshotProvenance(row.ElementIds, rowIndex, "ElementIds");
            var sourceHandleValues = SnapshotProvenance(row.SourceHandles, rowIndex, "SourceHandles");
            return new ExportRow
            {
                Floor = row.Floor ?? string.Empty,
                MaterialName = row.MaterialName ?? string.Empty,
                UnitHint = row.UnitHint ?? string.Empty,
                Component = row.Component ?? string.Empty,
                Category = row.Category ?? string.Empty,
                FamilyName = row.FamilyName ?? string.Empty,
                ElementCount = row.ElementCount,
                ElementIdCount = elementIdValues.Count,
                PrimaryQuantity = row.PrimaryQuantity,
                LengthM = row.LengthM,
                AreaM2 = row.AreaM2,
                VolumeM3 = row.VolumeM3,
                MassKg = row.MassKg,
                ProjectId = row.ProjectId ?? string.Empty,
                DrawingFingerprint = row.DrawingFingerprint ?? string.Empty,
                ElementIds = string.Join(ProvenanceSeparator, elementIdValues),
                SourceHandles = string.Join(ProvenanceSeparator, sourceHandleValues),
                ElementIdValues = elementIdValues,
                SourceHandleValues = sourceHandleValues
            };
        }

        private static List<string> SnapshotProvenance(IList<string> values, int rowIndex, string fieldName)
        {
            if (values == null)
                throw new ArgumentException("Material XLSX row " + rowIndex + " field " + fieldName + " cannot be null.", "rows");

            var count = values.Count;
            var snapshot = new List<string>(count);
            for (var index = 0; index < count; index++)
            {
                var value = values[index];
                if (value == null)
                    throw new ArgumentException(
                        "Material XLSX row " + rowIndex + " field " + fieldName + " contains a null provenance entry at index " + index + ".",
                        "rows");
                snapshot.Add(value);
            }
            if (values.Count != count)
                throw new InvalidOperationException("Material XLSX row " + rowIndex + " field " + fieldName + " count changed during snapshot traversal.");
            return snapshot;
        }

        private static void EnsureRowStable(MaterialUsageRow source, ExportRow snapshot, int rowIndex)
        {
            if (source == null ||
                !string.Equals(source.Floor ?? string.Empty, snapshot.Floor, StringComparison.Ordinal) ||
                !string.Equals(source.MaterialName ?? string.Empty, snapshot.MaterialName, StringComparison.Ordinal) ||
                !string.Equals(source.UnitHint ?? string.Empty, snapshot.UnitHint, StringComparison.Ordinal) ||
                !string.Equals(source.Component ?? string.Empty, snapshot.Component, StringComparison.Ordinal) ||
                !string.Equals(source.Category ?? string.Empty, snapshot.Category, StringComparison.Ordinal) ||
                !string.Equals(source.FamilyName ?? string.Empty, snapshot.FamilyName, StringComparison.Ordinal) ||
                source.ElementCount != snapshot.ElementCount ||
                source.PrimaryQuantity != snapshot.PrimaryQuantity ||
                source.LengthM != snapshot.LengthM ||
                source.AreaM2 != snapshot.AreaM2 ||
                source.VolumeM3 != snapshot.VolumeM3 ||
                source.MassKg != snapshot.MassKg ||
                !string.Equals(source.ProjectId ?? string.Empty, snapshot.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(source.DrawingFingerprint ?? string.Empty, snapshot.DrawingFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Material XLSX export row values changed during snapshot traversal. Invalid row index: " + rowIndex + ".");

            EnsureProvenanceStable(source.ElementIds, snapshot.ElementIdValues, rowIndex, "ElementIds");
            EnsureProvenanceStable(source.SourceHandles, snapshot.SourceHandleValues, rowIndex, "SourceHandles");
        }

        private static void EnsureProvenanceStable(IList<string> source, IReadOnlyList<string> snapshot, int rowIndex, string fieldName)
        {
            if (source == null || source.Count != snapshot.Count)
                throw new InvalidOperationException("Material XLSX row " + rowIndex + " field " + fieldName + " count changed during snapshot traversal.");
            for (var index = 0; index < snapshot.Count; index++)
            {
                if (!string.Equals(source[index], snapshot[index], StringComparison.Ordinal))
                    throw new InvalidOperationException("Material XLSX row " + rowIndex + " field " + fieldName + " values changed during snapshot traversal.");
            }
        }

        private static void ValidateCellText(string value, int rowIndex, string fieldName)
        {
            var text = value ?? string.Empty;
            if (text.Length > MaxCellTextCharacters)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Material XLSX row " + rowIndex + " field " + fieldName + " exceeds Excel's " + MaxCellTextCharacters + "-character cell text limit.");
        }

        private static void ValidateProvenanceText(string value, int rowIndex, string fieldName)
        {
            ValidateCellText(value, rowIndex, fieldName);
            var text = value ?? string.Empty;
            for (var index = 0; index < text.Length; index++)
            {
                var ch = text[index];
                if (char.IsHighSurrogate(ch))
                {
                    if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                        throw InvalidProvenanceText(rowIndex, fieldName, "contains an unpaired high surrogate.");
                    index++;
                    continue;
                }
                if (char.IsLowSurrogate(ch))
                    throw InvalidProvenanceText(rowIndex, fieldName, "contains an unpaired low surrogate.");
                if (ch == '\t' || ch == '\n' || ch == '\r') continue;
                if (ch < 0x20)
                    throw InvalidProvenanceText(rowIndex, fieldName, "contains an invalid XML control character.");
            }
        }

        private static InvalidDataException InvalidProvenanceText(int rowIndex, string fieldName, string reason)
        {
            return new InvalidDataException(
                "Material XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) +
                " field " + fieldName + " " + reason);
        }

        private static void ValidateCount(int value, int rowIndex, string fieldName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Material XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " field " + fieldName + " must be non-negative.");
        }

        private static void ValidateElementCount(int elementCount, int elementIdCount, int rowIndex)
        {
            if (elementCount != elementIdCount)
                throw new ArgumentException(
                    "Material XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " ElementCount must match ElementIds count.",
                    "rows");
        }

        private static void ValidateNonNegative(double value, int rowIndex, string fieldName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Material XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " field " + fieldName + " must be finite and non-negative.");
        }

        private static void StringCell(TextWriter writer, string cellRef, string value, int style)
        {
            writer.Write("<c r=\""); writer.Write(cellRef); writer.Write("\" t=\"inlineStr\" s=\""); writer.Write(style.ToString(CultureInfo.InvariantCulture)); writer.Write("\"><is><t");
            if (!string.IsNullOrEmpty(value) && (IsXmlWhitespace(value[0]) || IsXmlWhitespace(value[value.Length - 1]))) writer.Write(" xml:space=\"preserve\"");
            writer.Write(">"); writer.Write(XlsxXmlText.Escape(value)); writer.Write("</t></is></c>");
        }

        private static bool IsXmlWhitespace(char value) => value == ' ' || value == '\t' || value == '\n' || value == '\r';

        private static void NumberCell(TextWriter writer, string cellRef, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Material XLSX numeric values must be finite.");
            writer.Write("<c r=\""); writer.Write(cellRef); writer.Write("\" s=\"2\"><v>"); writer.Write(value.ToString("R", CultureInfo.InvariantCulture)); writer.Write("</v></c>");
        }

        private static string CellRef(int columnZeroBased, int row)
        {
            var n = columnZeroBased + 1;
            var name = string.Empty;
            while (n > 0) { n--; name = (char)('A' + (n % 26)) + name; n /= 26; }
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
            if (entryBytes < 0L || entryBytes > MaxWorksheetEntryBytes) throw new InvalidDataException("Material XLSX worksheet exceeds the bounded output contract: " + name + ".");
            var projected = checked(totalUncompressedBytes + entryBytes);
            if (projected > MaxAggregateUncompressedBytes) throw new InvalidDataException("Material XLSX aggregate uncompressed XML exceeds the bounded output contract.");
            totalUncompressedBytes = projected;
        }

        private sealed class BoundedEntryWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;
            internal BoundedEntryWriteStream(Stream inner, long maxLength) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); _maxLength = maxLength; }
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true; public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush(); public override int Read(byte[] b, int o, int c) => throw new NotSupportedException(); public override long Seek(long o, SeekOrigin x) => throw new NotSupportedException(); public override void SetLength(long v) => throw new NotSupportedException();
            public override void Write(byte[] b, int o, int c) { if (checked(_inner.Position + c) > _maxLength) throw new InvalidDataException("Material XLSX worksheet exceeds the bounded output contract."); _inner.Write(b, o, c); }
            public override void WriteByte(byte v) { if (checked(_inner.Position + 1L) > _maxLength) throw new InvalidDataException("Material XLSX worksheet exceeds the bounded output contract."); _inner.WriteByte(v); }
        }

        private sealed class BoundedArchiveWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;
            internal BoundedArchiveWriteStream(Stream inner, long maxLength) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); _maxLength = maxLength; }
            public override bool CanRead => _inner.CanRead; public override bool CanSeek => _inner.CanSeek; public override bool CanWrite => _inner.CanWrite; public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush(); public override int Read(byte[] b, int o, int c) => _inner.Read(b, o, c); public override long Seek(long o, SeekOrigin x) => _inner.Seek(o, x); public override void SetLength(long v) { if (v > _maxLength) throw new InvalidDataException("Material XLSX archive exceeds the bounded output contract."); _inner.SetLength(v); }
            public override void Write(byte[] b, int o, int c) { var projected = checked(_inner.Position + c); if (projected > _maxLength) throw new InvalidDataException("Material XLSX archive exceeds the bounded output contract."); _inner.Write(b, o, c); }
            public override void WriteByte(byte v) { if (checked(_inner.Position + 1L) > _maxLength) throw new InvalidDataException("Material XLSX archive exceeds the bounded output contract."); _inner.WriteByte(v); }
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Vật liệu\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}
