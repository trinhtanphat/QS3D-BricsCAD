using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    public static class DoorOpeningXlsxExporter
    {
        private const int MaxDataRows = 1048575;
        private const int MaxCellTextLength = 32767;
        private const long MaxWorksheetEntryBytes = 32L * 1024L * 1024L;
        private const long MaxAggregateUncompressedBytes = 64L * 1024L * 1024L;
        private const long MaxArchiveBytes = 64L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly DateTimeOffset FixedZipTimestamp = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static void Export(string path, IReadOnlyList<DoorOpeningScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            KnownCountContract<DoorOpeningScheduleRow> rowCount = BindKnownCount(rows, MaxDataRows, "export rows");

            var snapshot = new List<DoorOpeningScheduleRow>(rowCount.Value);
            var sourceRows = new List<DoorOpeningScheduleRow>(rowCount.Value);
            for (var rowIndex = 0; rowIndex < rowCount.Value; rowIndex++)
            {
                rowCount.Revalidate(rows, "before row indexer");
                var sourceRow = rows[rowIndex];
                rowCount.Revalidate(rows, "after row indexer");
                if (sourceRow == null)
                    throw new ArgumentException("Export rows cannot contain null entries. Invalid row index: " + rowIndex + ".", nameof(rows));
                sourceRows.Add(sourceRow);
                snapshot.Add(SnapshotRow(sourceRow, rowIndex));
                rowCount.Revalidate(rows, "after row snapshot");
            }
            rowCount.Revalidate(rows, "after snapshot traversal");
            for (var rowIndex = 0; rowIndex < rowCount.Value; rowIndex++)
                EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);
            ValidateCellText(snapshot);
            ValidateNumericValues(snapshot);
            ValidateProvenanceIntegrity(snapshot);

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
                if (new FileInfo(tempPath).Length > MaxArchiveBytes) throw new InvalidDataException("Door/opening XLSX archive exceeds the bounded output contract.");
                Validate(tempPath);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally { AtomicFileCommit.TryDelete(tempPath); }
        }

        private static DoorOpeningScheduleRow SnapshotRow(DoorOpeningScheduleRow source, int rowIndex)
        {
            var row = new DoorOpeningScheduleRow
            {
                ProjectId = source.ProjectId ?? string.Empty,
                DrawingFingerprint = source.DrawingFingerprint ?? string.Empty,
                Floor = source.Floor ?? string.Empty,
                Category = source.Category ?? string.Empty,
                FamilyName = source.FamilyName ?? string.Empty,
                Material = source.Material ?? string.Empty,
                WidthM = source.WidthM,
                HeightM = source.HeightM,
                SillHeightM = source.SillHeightM,
                ThicknessM = source.ThicknessM,
                Count = source.Count,
                OpeningAreaM2 = source.OpeningAreaM2,
                HostCount = source.HostCount
            };
            var label = "worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " ";
            SnapshotJoinedCellValues(source.ElementIds, row.ElementIds, label + "Element IDs");
            SnapshotJoinedCellValues(source.HostIds, row.HostIds, label + "Host IDs");
            SnapshotJoinedCellValues(source.SourceHandles, row.SourceHandles, label + "Source Handles");
            return row;
        }

        private static void SnapshotJoinedCellValues(IList<string> source, IList<string> target, string label)
        {
            if (source == null)
                throw new ArgumentException("Door/opening XLSX " + label + " collection is required.", "rows");

            var count = RequireConsistentKnownCount(source, MaxCellTextLength + 1, label);
            long joinedLength = 0L;
            for (var index = 0; index < count; index++)
            {
                var value = source[index] ?? string.Empty;
                if (index > 0) joinedLength++;
                joinedLength += value.Length;
                if (joinedLength > MaxCellTextLength)
                    throw new ArgumentOutOfRangeException(
                        "rows",
                        "Door/opening XLSX " + label + " exceeds Excel's " + MaxCellTextLength + "-character cell text limit.");
                target.Add(value);
            }
            if (source.Count != count)
                throw new InvalidOperationException("Door/opening XLSX " + label + " count changed during snapshot.");
        }

        private static void EnsureRowStable(DoorOpeningScheduleRow source, DoorOpeningScheduleRow snapshot, int rowIndex)
        {
            if (source == null ||
                !string.Equals(source.ProjectId ?? string.Empty, snapshot.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(source.DrawingFingerprint ?? string.Empty, snapshot.DrawingFingerprint, StringComparison.Ordinal) ||
                !string.Equals(source.Floor ?? string.Empty, snapshot.Floor, StringComparison.Ordinal) ||
                !string.Equals(source.Category ?? string.Empty, snapshot.Category, StringComparison.Ordinal) ||
                !string.Equals(source.FamilyName ?? string.Empty, snapshot.FamilyName, StringComparison.Ordinal) ||
                !string.Equals(source.Material ?? string.Empty, snapshot.Material, StringComparison.Ordinal) ||
                !source.WidthM.Equals(snapshot.WidthM) ||
                !source.HeightM.Equals(snapshot.HeightM) ||
                !source.SillHeightM.Equals(snapshot.SillHeightM) ||
                !source.ThicknessM.Equals(snapshot.ThicknessM) ||
                source.Count != snapshot.Count ||
                !source.OpeningAreaM2.Equals(snapshot.OpeningAreaM2) ||
                source.HostCount != snapshot.HostCount)
                throw new InvalidOperationException("Door/opening XLSX export row values changed during snapshot traversal. Invalid row index: " + rowIndex + ".");

            EnsureProvenanceStable(source.ElementIds, snapshot.ElementIds, rowIndex, "Element IDs");
            EnsureProvenanceStable(source.HostIds, snapshot.HostIds, rowIndex, "Host IDs");
            EnsureProvenanceStable(source.SourceHandles, snapshot.SourceHandles, rowIndex, "Source Handles");
        }

        private static void EnsureProvenanceStable(IList<string> source, IList<string> snapshot, int rowIndex, string fieldName)
        {
            if (source == null)
                throw new InvalidOperationException("Door/opening XLSX export row " + rowIndex + " field " + fieldName + " became unavailable during snapshot traversal.");
            var sourceCount = source.Count;
            if (sourceCount != snapshot.Count)
                throw new InvalidOperationException("Door/opening XLSX export row " + rowIndex + " field " + fieldName + " count changed during snapshot traversal.");
            for (var index = 0; index < snapshot.Count; index++)
            {
                if (!string.Equals(source[index] ?? string.Empty, snapshot[index] ?? string.Empty, StringComparison.Ordinal))
                    throw new InvalidOperationException("Door/opening XLSX export row " + rowIndex + " field " + fieldName + " values changed during snapshot traversal.");
            }
            if (source.Count != sourceCount)
                throw new InvalidOperationException("Door/opening XLSX export row " + rowIndex + " field " + fieldName + " count changed during snapshot traversal.");
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
                var observed = Observe(source, "at admission");
                Value = observed;
                _bound = true;
            }

            internal void Revalidate(IEnumerable<T> source, string phase)
            {
                if (!_bound) throw new InvalidOperationException("Door/opening XLSX " + _label + " count contract was not admitted.");
                var observed = Observe(source, phase);
                if (observed != Value)
                {
                    if (string.Equals(_label, "export rows", StringComparison.Ordinal))
                        throw new InvalidOperationException("Door/opening XLSX row count changed during snapshot " + phase + ". Expected " + Value + " but observed " + observed + ".");
                    throw new InvalidOperationException("Door/opening XLSX " + _label + " count changed " + phase + ". Expected " + Value + " but observed " + observed + ".");
                }
            }

            private int Observe(IEnumerable<T> source, string phase)
            {
                if ((source is IReadOnlyCollection<T>) != _readOnlyCount ||
                    (source is ICollection<T>) != _genericCount ||
                    (source is ICollection) != _nonGenericCount)
                    throw new InvalidOperationException("Door/opening XLSX " + _label + " known count sources changed " + phase + ".");

                int? expected = null;
                Action<int> observe = count =>
                {
                    if (count < 0)
                        throw new ArgumentOutOfRangeException("rows", "Door/opening XLSX " + _label + " count must be non-negative " + phase + ".");
                    if (count > _maximum)
                        throw new ArgumentOutOfRangeException("rows", "Door/opening XLSX " + _label + " count exceeds the supported maximum of " + _maximum + " " + phase + ".");
                    if (expected.HasValue && expected.Value != count)
                        throw new InvalidOperationException("Door/opening XLSX " + _label + " exposes conflicting known collection counts " + phase + ".");
                    expected = count;
                };

                if (_readOnlyCount) observe(((IReadOnlyCollection<T>)source).Count);
                if (_genericCount) observe(((ICollection<T>)source).Count);
                if (_nonGenericCount) observe(((ICollection)source).Count);
                if (!expected.HasValue)
                    throw new ArgumentException("Door/opening XLSX " + _label + " must expose a deterministic collection count.", "rows");
                return expected.Value;
            }
        }

        private static void ValidateCellText(IReadOnlyList<DoorOpeningScheduleRow> rows)
        {
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var label = "worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " ";
                RequireCellTextLength(row.ProjectId, label + "Project ID");
                RequireCellTextLength(row.DrawingFingerprint, label + "Drawing Fingerprint");
                RequireCellTextLength(row.Floor, label + "Floor");
                RequireCellTextLength(row.Category, label + "Category");
                RequireCellTextLength(row.FamilyName, label + "Family name");
                RequireCellTextLength(row.Material, label + "Material");
                RequireJoinedCellTextLength(row.ElementIds, label + "Element IDs");
                RequireJoinedCellTextLength(row.HostIds, label + "Host IDs");
                RequireJoinedCellTextLength(row.SourceHandles, label + "Source Handles");
            }
        }

        private static void ValidateNumericValues(IReadOnlyList<DoorOpeningScheduleRow> rows)
        {
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var label = "worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " ";
                RequireCount(row.Count, label + "Count");
                RequireCount(row.HostCount, label + "HostCount");
                RequirePositive(row.WidthM, label + "WidthM");
                RequirePositive(row.HeightM, label + "HeightM");
                RequireNonNegative(row.SillHeightM, label + "SillHeightM");
                RequireNonNegative(row.ThicknessM, label + "ThicknessM");
                RequireNonNegative(row.OpeningAreaM2, label + "OpeningAreaM2");
            }
        }

        private static void ValidateProvenanceIntegrity(IReadOnlyList<DoorOpeningScheduleRow> rows)
        {
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var label = "worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " ";
                if (row.Count != row.ElementIds.Count)
                    throw new ArgumentException("Door/opening XLSX " + label + "Count must match Element IDs count.", "rows");
                if (row.HostCount != row.HostIds.Count)
                    throw new ArgumentException("Door/opening XLSX " + label + "HostCount must match Host IDs count.", "rows");

                RequireXmlProvenance(row.ProjectId, label + "Project ID");
                RequireXmlProvenance(row.DrawingFingerprint, label + "Drawing Fingerprint");
                RequireXmlProvenance(row.ElementIds, label + "Element IDs");
                RequireXmlProvenance(row.HostIds, label + "Host IDs");
                RequireXmlProvenance(row.SourceHandles, label + "Source Handles");
            }
        }

        private static void RequireXmlProvenance(string value, string label)
        {
            try { XmlConvert.VerifyXmlChars(value ?? string.Empty); }
            catch (XmlException ex)
            {
                throw new ArgumentException("Door/opening XLSX " + label + " contains characters that are invalid in XML provenance.", "rows", ex);
            }
        }

        private static void RequireXmlProvenance(IEnumerable<string> values, string label)
        {
            var index = 0;
            foreach (var value in values)
            {
                RequireXmlProvenance(value ?? string.Empty, label + "[" + index.ToString(CultureInfo.InvariantCulture) + "]");
                index++;
            }
        }

        private static void RequireCount(int value, string label)
        {
            if (value < 0) throw new ArgumentOutOfRangeException("rows", "Door/opening XLSX " + label + " must be non-negative.");
        }

        private static void RequirePositive(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException("rows", "Door/opening XLSX " + label + " must be finite and greater than zero.");
        }

        private static void RequireNonNegative(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException("rows", "Door/opening XLSX " + label + " must be finite and non-negative.");
        }

        private static void RequireCellTextLength(string value, string label)
        {
            if ((value ?? string.Empty).Length > MaxCellTextLength)
                throw new ArgumentOutOfRangeException("rows", "Door/opening XLSX " + label + " exceeds Excel's " + MaxCellTextLength + "-character cell text limit.");
        }

        private static void RequireJoinedCellTextLength(IEnumerable<string> values, string label)
        {
            if (values == null) throw new ArgumentException("Door/opening XLSX " + label + " collection is required.", "rows");
            long length = 0L;
            var index = 0;
            foreach (var value in values)
            {
                if (index > 0) length++;
                length += (value ?? string.Empty).Length;
                if (length > MaxCellTextLength)
                    throw new ArgumentOutOfRangeException("rows", "Door/opening XLSX " + label + " exceeds Excel's " + MaxCellTextLength + "-character cell text limit.");
                index++;
            }
        }

        private static void WriteWorksheetEntry(ZipArchive archive, IReadOnlyList<DoorOpeningScheduleRow> rows, ref long totalUncompressedBytes)
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

        private static void WriteSheet(TextWriter writer, IReadOnlyList<DoorOpeningScheduleRow> rows)
        {
            var headers = new[] { "Tầng", "Loại", "Family / Loại", "Vật liệu", "Rộng (m)", "Cao (m)", "Cao bậu (m)", "Dày (m)", "SL", "DT mở (m²)", "SL host", "Element IDs", "Host IDs", "Project ID", "Drawing Fingerprint", "Source Handles" };
            var lastRow = Math.Max(1, rows.Count + 1);
            var range = "A1:P" + lastRow.ToString(CultureInfo.InvariantCulture);
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            writer.Write("<dimension ref=\""); writer.Write(range); writer.Write("\"/>");
            writer.Write("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            writer.Write("<cols><col min=\"1\" max=\"4\" width=\"20\" customWidth=\"1\"/><col min=\"5\" max=\"11\" width=\"15\" customWidth=\"1\"/><col min=\"12\" max=\"16\" width=\"36\" customWidth=\"1\"/></cols><sheetData>");
            writer.Write("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) StringCell(writer, CellRef(c, 1), headers[c], 1);
            writer.Write("</row>");
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var r = index + 2;
                writer.Write("<row r=\""); writer.Write(r.ToString(CultureInfo.InvariantCulture)); writer.Write("\">");
                StringCell(writer, CellRef(0, r), row.Floor, 0); StringCell(writer, CellRef(1, r), row.Category, 0); StringCell(writer, CellRef(2, r), row.FamilyName, 0); StringCell(writer, CellRef(3, r), row.Material, 0);
                NumberCell(writer, CellRef(4, r), row.WidthM); NumberCell(writer, CellRef(5, r), row.HeightM); NumberCell(writer, CellRef(6, r), row.SillHeightM); NumberCell(writer, CellRef(7, r), row.ThicknessM); NumberCell(writer, CellRef(8, r), row.Count); NumberCell(writer, CellRef(9, r), row.OpeningAreaM2); NumberCell(writer, CellRef(10, r), row.HostCount);
                StringCell(writer, CellRef(11, r), string.Join(";", row.ElementIds), 0); StringCell(writer, CellRef(12, r), string.Join(";", row.HostIds), 0); StringCell(writer, CellRef(13, r), row.ProjectId, 0); StringCell(writer, CellRef(14, r), row.DrawingFingerprint, 0); StringCell(writer, CellRef(15, r), string.Join(";", row.SourceHandles), 0);
                writer.Write("</row>");
            }
            writer.Write("</sheetData><autoFilter ref=\""); writer.Write(range); writer.Write("\"/></worksheet>");
        }

        private static void Validate(string path)
        {
            XlsxPackageValidator.Validate(path, "[Content_Types].xml", "_rels/.rels", "xl/workbook.xml", "xl/_rels/workbook.xml.rels", "xl/styles.xml", "xl/worksheets/sheet1.xml");
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
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Door/opening XLSX numeric values must be finite.");
            writer.Write("<c r=\""); writer.Write(cellRef); writer.Write("\" s=\"2\"><v>"); writer.Write(value.ToString("R", CultureInfo.InvariantCulture)); writer.Write("</v></c>");
        }

        private static string CellRef(int columnZeroBased, int row)
        {
            var n = columnZeroBased + 1; var name = string.Empty; while (n > 0) { n--; name = (char)('A' + (n % 26)) + name; n /= 26; } return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content, ref long totalUncompressedBytes)
        {
            var bytes = StrictUtf8.GetBytes(content); ReserveUncompressed(bytes.LongLength, ref totalUncompressedBytes, name);
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal); entry.LastWriteTime = FixedZipTimestamp;
            using (var target = entry.Open()) target.Write(bytes, 0, bytes.Length);
        }

        private static void ReserveUncompressed(long entryBytes, ref long totalUncompressedBytes, string name)
        {
            if (entryBytes < 0L || entryBytes > MaxWorksheetEntryBytes) throw new InvalidDataException("Door/opening XLSX worksheet exceeds the bounded output contract: " + name + ".");
            var projected = checked(totalUncompressedBytes + entryBytes);
            if (projected > MaxAggregateUncompressedBytes) throw new InvalidDataException("Door/opening XLSX aggregate uncompressed XML exceeds the bounded output contract.");
            totalUncompressedBytes = projected;
        }

        private sealed class BoundedEntryWriteStream : Stream
        {
            private readonly Stream _inner; private readonly long _maxLength;
            internal BoundedEntryWriteStream(Stream inner, long maxLength) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); _maxLength = maxLength; }
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true; public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush(); public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException(); public override long Seek(long o,SeekOrigin x)=>throw new NotSupportedException(); public override void SetLength(long v)=>throw new NotSupportedException();
            public override void Write(byte[] b,int o,int c) { if (checked(_inner.Position+c)>_maxLength) throw new InvalidDataException("Door/opening XLSX worksheet exceeds the bounded output contract."); _inner.Write(b,o,c); }
            public override void WriteByte(byte v) { if (checked(_inner.Position+1L)>_maxLength) throw new InvalidDataException("Door/opening XLSX worksheet exceeds the bounded output contract."); _inner.WriteByte(v); }
        }

        private sealed class BoundedArchiveWriteStream : Stream
        {
            private readonly Stream _inner; private readonly long _maxLength;
            internal BoundedArchiveWriteStream(Stream inner,long maxLength) { _inner=inner??throw new ArgumentNullException(nameof(inner)); _maxLength=maxLength; }
            public override bool CanRead=>_inner.CanRead; public override bool CanSeek=>_inner.CanSeek; public override bool CanWrite=>_inner.CanWrite; public override long Length=>_inner.Length;
            public override long Position { get=>_inner.Position; set { if(value<0L||value>_maxLength) throw Exceeded(); _inner.Position=value; } }
            public override void Flush()=>_inner.Flush(); public override int Read(byte[] b,int o,int c)=>_inner.Read(b,o,c);
            public override long Seek(long o,SeekOrigin origin) { long t=origin==SeekOrigin.Begin?o:origin==SeekOrigin.Current?checked(_inner.Position+o):checked(_inner.Length+o); if(t<0L||t>_maxLength) throw Exceeded(); return _inner.Seek(o,origin); }
            public override void SetLength(long v) { if(v<0L||v>_maxLength) throw Exceeded(); _inner.SetLength(v); }
            public override void Write(byte[] b,int o,int c) { if(Math.Max(_inner.Length,checked(_inner.Position+c))>_maxLength) throw Exceeded(); _inner.Write(b,o,c); }
            public override void WriteByte(byte v) { if(Math.Max(_inner.Length,checked(_inner.Position+1L))>_maxLength) throw Exceeded(); _inner.WriteByte(v); }
            private static InvalidDataException Exceeded()=>new InvalidDataException("Door/opening XLSX archive exceeds the bounded output contract.");
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Cửa - Lỗ mở\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}