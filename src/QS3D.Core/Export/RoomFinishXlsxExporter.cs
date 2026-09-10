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
    public static class RoomFinishXlsxExporter
    {
        private const int MaxDataRows = 1048575;
        private const int MaxCellTextCharacters = 32767;
        private const long MaxWorksheetEntryBytes = 32L * 1024L * 1024L;
        private const long MaxAggregateUncompressedBytes = 64L * 1024L * 1024L;
        private const long MaxArchiveBytes = 64L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly DateTimeOffset FixedZipTimestamp = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static void Export(string path, IReadOnlyList<RoomFinishScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var rowCount = BindKnownCount(rows, MaxDataRows, "export rows");
            var snapshot = new List<RoomFinishScheduleRow>(rowCount.Value);
            var sourceRows = new List<RoomFinishScheduleRow>(rowCount.Value);
            for (var rowIndex = 0; rowIndex < rowCount.Value; rowIndex++)
            {
                rowCount.Revalidate(rows, "before row indexer");
                var sourceRow = rows[rowIndex];
                rowCount.Revalidate(rows, "after row indexer");
                if (sourceRow == null)
                    throw new ArgumentException("Export rows cannot contain null entries. Invalid row index: " + rowIndex + ".", nameof(rows));
                sourceRows.Add(sourceRow);
                var row = SnapshotRow(sourceRow, rowIndex);
                rowCount.Revalidate(rows, "after row snapshot");
                ValidateCellText(row.Floor, rowIndex, "Floor");
                ValidateCellText(row.Room, rowIndex, "Room");
                ValidateCellText(row.Category, rowIndex, "Category");
                ValidateCellText(row.FamilyName, rowIndex, "FamilyName");
                ValidateCellText(row.Material, rowIndex, "Material");
                ValidateCellText(row.UnitHint, rowIndex, "UnitHint");
                ValidateJoinedCellText(row.ElementIds, rowIndex, "ElementIds");
                ValidateProvenanceValues(row.ElementIds, rowIndex, "ElementIds");
                ValidateJoinedCellText(row.RoomIds, rowIndex, "RoomIds");
                ValidateProvenanceValues(row.RoomIds, rowIndex, "RoomIds");
                ValidateProvenanceCellText(row.ProjectId, rowIndex, "ProjectId");
                ValidateProvenanceCellText(row.DrawingFingerprint, rowIndex, "DrawingFingerprint");
                ValidateJoinedCellText(row.SourceHandles, rowIndex, "SourceHandles");
                ValidateProvenanceValues(row.SourceHandles, rowIndex, "SourceHandles");
                ValidateNonNegativeCount(row.Count, rowIndex);
                ValidateNonNegativeFinite(row.PrimaryQuantity, rowIndex, "PrimaryQuantity");
                ValidateNonNegativeFinite(row.LengthM, rowIndex, "LengthM");
                ValidateNonNegativeFinite(row.AreaM2, rowIndex, "AreaM2");
                snapshot.Add(row);
            }
            rowCount.Revalidate(rows, "after snapshot traversal");
            for (var rowIndex = 0; rowIndex < rowCount.Value; rowIndex++)
                EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);
            rowCount.Revalidate(rows, "after row stability validation");
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
                if (new FileInfo(tempPath).Length > MaxArchiveBytes)
                    throw new InvalidDataException("Room-finish XLSX archive exceeds the bounded output contract.");
                Validate(tempPath);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally { AtomicFileCommit.TryDelete(tempPath); }
        }

        private static KnownCountContract<T> BindKnownCount<T>(IEnumerable<T> source, int maximum, string label)
        {
            var contract = new KnownCountContract<T>(
                source is IReadOnlyCollection<T>,
                source is ICollection<T>,
                source is ICollection,
                maximum,
                label);
            contract.Bind(source);
            return contract;
        }

        private static int RequireConsistentKnownCount<T>(IEnumerable<T> source, int maximum, string label)
        {
            return BindKnownCount(source, maximum, label).Value;
        }

        private sealed class KnownCountContract<T>
        {
            private readonly bool _readOnlyCount;
            private readonly bool _genericCount;
            private readonly bool _nonGenericCount;
            private readonly int _maximum;
            private readonly string _label;
            private bool _bound;

            internal KnownCountContract(bool readOnlyCount, bool genericCount, bool nonGenericCount, int maximum, string label)
            {
                _readOnlyCount = readOnlyCount;
                _genericCount = genericCount;
                _nonGenericCount = nonGenericCount;
                _maximum = maximum;
                _label = label;
            }

            internal int Value { get; private set; }

            internal void Bind(IEnumerable<T> source)
            {
                Value = Observe(source, "at admission");
                _bound = true;
            }

            internal void Revalidate(IEnumerable<T> source, string phase)
            {
                if (!_bound)
                    throw new InvalidOperationException("Room-finish XLSX " + _label + " count contract was not admitted.");
                var observed = Observe(source, phase);
                if (observed != Value)
                    throw new InvalidOperationException("Room-finish XLSX " + _label + " count changed " + phase + ". Expected " + Value + " but observed " + observed + ".");
            }

            private int Observe(IEnumerable<T> source, string phase)
            {
                if ((source is IReadOnlyCollection<T>) != _readOnlyCount ||
                    (source is ICollection<T>) != _genericCount ||
                    (source is ICollection) != _nonGenericCount)
                    throw new InvalidOperationException("Room-finish XLSX " + _label + " known count sources changed " + phase + ".");

                int? expected = null;
                Action<int> observe = count =>
                {
                    if (count < 0)
                        throw new ArgumentOutOfRangeException("rows", "Room-finish XLSX " + _label + " count must be non-negative " + phase + ".");
                    if (count > _maximum)
                        throw new ArgumentOutOfRangeException("rows", "Room-finish XLSX " + _label + " count exceeds the supported maximum of " + _maximum + " " + phase + ".");
                    if (expected.HasValue && expected.Value != count)
                        throw new InvalidOperationException("Room-finish XLSX " + _label + " exposes conflicting known collection counts " + phase + ".");
                    expected = count;
                };

                if (_readOnlyCount) observe(((IReadOnlyCollection<T>)source).Count);
                if (_genericCount) observe(((ICollection<T>)source).Count);
                if (_nonGenericCount) observe(((ICollection)source).Count);
                if (!expected.HasValue)
                    throw new ArgumentException("Room-finish XLSX " + _label + " must expose a deterministic collection count.", "rows");
                return expected.Value;
            }
        }

        private static RoomFinishScheduleRow SnapshotRow(RoomFinishScheduleRow source, int rowIndex)
        {
            var row = new RoomFinishScheduleRow
            {
                ProjectId = source.ProjectId ?? string.Empty,
                DrawingFingerprint = source.DrawingFingerprint ?? string.Empty,
                Floor = source.Floor ?? string.Empty,
                Room = source.Room ?? string.Empty,
                Category = source.Category ?? string.Empty,
                FamilyName = source.FamilyName ?? string.Empty,
                Material = source.Material ?? string.Empty,
                UnitHint = source.UnitHint ?? string.Empty,
                Count = source.Count,
                LengthM = source.LengthM,
                AreaM2 = source.AreaM2,
                PrimaryQuantity = source.PrimaryQuantity
            };
            SnapshotJoinedCellValues(source.ElementIds, row.ElementIds, rowIndex, "ElementIds");
            SnapshotJoinedCellValues(source.RoomIds, row.RoomIds, rowIndex, "RoomIds");
            SnapshotJoinedCellValues(source.SourceHandles, row.SourceHandles, rowIndex, "SourceHandles");
            return row;
        }

        private static void SnapshotJoinedCellValues(IList<string> source, IList<string> target, int rowIndex, string fieldName)
        {
            if (source == null)
                throw new ArgumentException("Room-finish XLSX row " + rowIndex + " field " + fieldName + " collection is required.", "rows");

            var count = source.Count;
            long joinedLength = 0L;
            for (var index = 0; index < count; index++)
            {
                var value = source[index] ?? string.Empty;
                if (index > 0) joinedLength++;
                joinedLength += value.Length;
                if (joinedLength > MaxCellTextCharacters)
                    throw new ArgumentOutOfRangeException(
                        "rows",
                        "Room-finish XLSX row " + rowIndex + " field " + fieldName + " exceeds Excel's " + MaxCellTextCharacters + "-character cell text limit.");
                target.Add(value);
            }
            if (source.Count != count)
                throw new InvalidOperationException("Room-finish XLSX row " + rowIndex + " field " + fieldName + " count changed during snapshot.");
        }

        private static void EnsureRowStable(RoomFinishScheduleRow source, RoomFinishScheduleRow snapshot, int rowIndex)
        {
            if (!string.Equals(source.ProjectId ?? string.Empty, snapshot.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(source.DrawingFingerprint ?? string.Empty, snapshot.DrawingFingerprint, StringComparison.Ordinal) ||
                !string.Equals(source.Floor ?? string.Empty, snapshot.Floor, StringComparison.Ordinal) ||
                !string.Equals(source.Room ?? string.Empty, snapshot.Room, StringComparison.Ordinal) ||
                !string.Equals(source.Category ?? string.Empty, snapshot.Category, StringComparison.Ordinal) ||
                !string.Equals(source.FamilyName ?? string.Empty, snapshot.FamilyName, StringComparison.Ordinal) ||
                !string.Equals(source.Material ?? string.Empty, snapshot.Material, StringComparison.Ordinal) ||
                !string.Equals(source.UnitHint ?? string.Empty, snapshot.UnitHint, StringComparison.Ordinal) ||
                source.Count != snapshot.Count ||
                !source.LengthM.Equals(snapshot.LengthM) ||
                !source.AreaM2.Equals(snapshot.AreaM2) ||
                !source.PrimaryQuantity.Equals(snapshot.PrimaryQuantity))
                throw new InvalidOperationException("Room-finish XLSX row " + rowIndex + " values changed during snapshot.");

            EnsureJoinedCellValuesStable(source.ElementIds, snapshot.ElementIds, rowIndex, "ElementIds");
            EnsureJoinedCellValuesStable(source.RoomIds, snapshot.RoomIds, rowIndex, "RoomIds");
            EnsureJoinedCellValuesStable(source.SourceHandles, snapshot.SourceHandles, rowIndex, "SourceHandles");
        }

        private static void EnsureJoinedCellValuesStable(IList<string> source, IList<string> snapshot, int rowIndex, string fieldName)
        {
            if (source == null || source.Count != snapshot.Count)
                throw new InvalidOperationException("Room-finish XLSX row " + rowIndex + " field " + fieldName + " changed during snapshot.");
            for (var index = 0; index < snapshot.Count; index++)
            {
                if (!string.Equals(source[index] ?? string.Empty, snapshot[index] ?? string.Empty, StringComparison.Ordinal))
                    throw new InvalidOperationException("Room-finish XLSX row " + rowIndex + " field " + fieldName + " changed during snapshot.");
            }
        }

        private static void WriteWorksheetEntry(ZipArchive archive, IReadOnlyList<RoomFinishScheduleRow> rows, ref long totalUncompressedBytes)
        {
            using (var buffer = new MemoryStream())
            {
                using (var boundedEntry = new BoundedEntryWriteStream(buffer, MaxWorksheetEntryBytes))
                using (var writer = new StreamWriter(boundedEntry, StrictUtf8, 4096, true))
                    WriteSheet(writer, rows);
                ReserveUncompressed(buffer.Length, ref totalUncompressedBytes, "xl/worksheets/sheet1.xml");
                buffer.Position = 0L;
                var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
                entry.LastWriteTime = FixedZipTimestamp;
                using (var target = entry.Open()) buffer.CopyTo(target);
            }
        }

        private static void WriteSheet(TextWriter writer, IReadOnlyList<RoomFinishScheduleRow> rows)
        {
            var headers = new[]
            {
                "T?ng", "Ph?ng", "Lo?i ho?n thi?n", "Family / Lo?i", "V?t li?u", "??n v?", "SL", "KL ch?nh", "D?i (m)", "Di?n t?ch (m?)",
                "Element IDs", "Room IDs", "Project ID", "Drawing fingerprint", "Source Handles"
            };
            var lastRow = Math.Max(1, rows.Count + 1);
            var range = "A1:O" + lastRow.ToString(CultureInfo.InvariantCulture);
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\""); writer.Write(range); writer.Write("\"/>");
            writer.Write("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            writer.Write("<cols><col min=\"1\" max=\"6\" width=\"20\" customWidth=\"1\"/><col min=\"7\" max=\"10\" width=\"15\" customWidth=\"1\"/><col min=\"11\" max=\"12\" width=\"36\" customWidth=\"1\"/><col min=\"13\" max=\"15\" width=\"36\" customWidth=\"1\"/></cols><sheetData>");
            writer.Write("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) StringCell(writer, CellRef(c, 1), headers[c], 1);
            writer.Write("</row>");
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var r = index + 2;
                writer.Write("<row r=\""); writer.Write(r.ToString(CultureInfo.InvariantCulture)); writer.Write("\">");
                StringCell(writer, CellRef(0, r), row.Floor, 0);
                StringCell(writer, CellRef(1, r), row.Room, 0);
                StringCell(writer, CellRef(2, r), row.Category, 0);
                StringCell(writer, CellRef(3, r), row.FamilyName, 0);
                StringCell(writer, CellRef(4, r), row.Material, 0);
                StringCell(writer, CellRef(5, r), row.UnitHint, 0);
                NumberCell(writer, CellRef(6, r), row.Count);
                NumberCell(writer, CellRef(7, r), row.PrimaryQuantity);
                NumberCell(writer, CellRef(8, r), row.LengthM);
                NumberCell(writer, CellRef(9, r), row.AreaM2);
                StringCell(writer, CellRef(10, r), string.Join(";", row.ElementIds), 0);
                StringCell(writer, CellRef(11, r), string.Join(";", row.RoomIds), 0);
                StringCell(writer, CellRef(12, r), row.ProjectId, 0);
                StringCell(writer, CellRef(13, r), row.DrawingFingerprint, 0);
                StringCell(writer, CellRef(14, r), string.Join(";", row.SourceHandles), 0);
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

        private static void ValidateCellText(string value, int rowIndex, string fieldName)
        {
            var text = value ?? string.Empty;
            if (text.Length > MaxCellTextCharacters)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Room-finish XLSX row " + rowIndex + " field " + fieldName + " exceeds Excel's " + MaxCellTextCharacters + "-character cell text limit.");
        }
        private static void ValidateProvenanceCellText(string value, int rowIndex, string fieldName)
        {
            ValidateCellText(value, rowIndex, fieldName);
            ValidateXmlControls(value, rowIndex, fieldName);
        }

        private static void ValidateProvenanceValues(IList<string> values, int rowIndex, string fieldName)
        {
            for (var index = 0; index < values.Count; index++)
                ValidateXmlControls(values[index], rowIndex, fieldName + "[" + index.ToString(CultureInfo.InvariantCulture) + "]");
        }

        private static void ValidateXmlControls(string value, int rowIndex, string fieldName)
        {
            var text = value ?? string.Empty;
            for (var index = 0; index < text.Length; index++)
            {
                var current = text[index];
                if (char.IsHighSurrogate(current))
                {
                    if (index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                    {
                        index++;
                        continue;
                    }

                    throw new ArgumentException(
                        "Room-finish XLSX row " + rowIndex + " field " + fieldName + " contains malformed UTF-16 provenance.",
                        "rows");
                }

                if (char.IsLowSurrogate(current))
                    throw new ArgumentException(
                        "Room-finish XLSX row " + rowIndex + " field " + fieldName + " contains malformed UTF-16 provenance.",
                        "rows");

                if (char.IsControl(current))
                    throw new ArgumentException(
                        "Room-finish XLSX row " + rowIndex + " field " + fieldName + " contains a control character in provenance.",
                        "rows");
                if (current >= '\u0020') continue;
                throw new ArgumentException(
                    "Room-finish XLSX row " + rowIndex + " field " + fieldName + " contains an XML control character.",
                    "rows");
            }
        }

        private static void ValidateJoinedCellText(IList<string> values, int rowIndex, string fieldName)
        {
            long length = 0;
            for (var index = 0; index < values.Count; index++)
            {
                if (index > 0) length++;
                length += (values[index] ?? string.Empty).Length;
                if (length > MaxCellTextCharacters)
                    throw new ArgumentOutOfRangeException(
                        "rows",
                        "Room-finish XLSX row " + rowIndex + " field " + fieldName + " exceeds Excel's " + MaxCellTextCharacters + "-character cell text limit.");
            }
        }

        private static void ValidateNonNegativeCount(int value, int rowIndex)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Room-finish XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " field Count must be non-negative.");
        }

        private static void ValidateNonNegativeFinite(double value, int rowIndex, string fieldName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Room-finish XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " field " + fieldName + " must be finite and non-negative.");
        }

        private static void StringCell(TextWriter writer, string cellRef, string value, int style)
        {
            writer.Write("<c r=\""); writer.Write(cellRef); writer.Write("\" t=\"inlineStr\" s=\"");
            writer.Write(style.ToString(CultureInfo.InvariantCulture)); writer.Write("\"><is><t>");
            writer.Write(XlsxXmlText.Escape(value)); writer.Write("</t></is></c>");
        }

        private static void NumberCell(TextWriter writer, string cellRef, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Room-finish XLSX numeric values must be finite.");
            var text = value == 0d ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
            writer.Write("<c r=\""); writer.Write(cellRef); writer.Write("\" s=\"2\"><v>");
            writer.Write(text); writer.Write("</v></c>");
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
            if (entryBytes < 0L || entryBytes > MaxWorksheetEntryBytes) throw new InvalidDataException("Room-finish XLSX worksheet exceeds the bounded output contract: " + name + ".");
            var projected = checked(totalUncompressedBytes + entryBytes);
            if (projected > MaxAggregateUncompressedBytes) throw new InvalidDataException("Room-finish XLSX aggregate uncompressed XML exceeds the bounded output contract.");
            totalUncompressedBytes = projected;
        }

        private sealed class BoundedEntryWriteStream : Stream
        {
            private readonly Stream _inner; private readonly long _maxLength;
            internal BoundedEntryWriteStream(Stream inner, long maxLength) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); _maxLength = maxLength; }
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true; public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush(); public override int Read(byte[] b, int o, int c) => throw new NotSupportedException(); public override long Seek(long o, SeekOrigin x) => throw new NotSupportedException(); public override void SetLength(long v) => throw new NotSupportedException();
            public override void Write(byte[] b, int o, int c) { if (checked(_inner.Position + c) > _maxLength) throw new InvalidDataException("Room-finish XLSX worksheet exceeds the bounded output contract."); _inner.Write(b, o, c); }
            public override void WriteByte(byte v) { if (checked(_inner.Position + 1L) > _maxLength) throw new InvalidDataException("Room-finish XLSX worksheet exceeds the bounded output contract."); _inner.WriteByte(v); }
        }

        private sealed class BoundedArchiveWriteStream : Stream
        {
            private readonly Stream _inner; private readonly long _maxLength;
            internal BoundedArchiveWriteStream(Stream inner, long maxLength) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); _maxLength = maxLength; }
            public override bool CanRead => _inner.CanRead; public override bool CanSeek => _inner.CanSeek; public override bool CanWrite => _inner.CanWrite; public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush(); public override int Read(byte[] b, int o, int c) => _inner.Read(b, o, c); public override long Seek(long o, SeekOrigin x) => _inner.Seek(o, x); public override void SetLength(long v) { if (v > _maxLength) throw new InvalidDataException("Room-finish XLSX archive exceeds the bounded output contract."); _inner.SetLength(v); }
            public override void Write(byte[] b, int o, int c) { var projected = checked(_inner.Position + c); if (projected > _maxLength) throw new InvalidDataException("Room-finish XLSX archive exceeds the bounded output contract."); _inner.Write(b, o, c); }
            public override void WriteByte(byte v) { if (checked(_inner.Position + 1L) > _maxLength) throw new InvalidDataException("Room-finish XLSX archive exceeds the bounded output contract."); _inner.WriteByte(v); }
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"HT Ph??ng\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}
