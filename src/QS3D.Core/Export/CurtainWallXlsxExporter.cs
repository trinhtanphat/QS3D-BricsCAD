using System;
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
    public static class CurtainWallXlsxExporter
    {
        private const int MaxDataRows = 1048575;
        private const int MaxCellTextCharacters = 32767;
        private const long MaxWorksheetEntryBytes = 32L * 1024L * 1024L;
        private const long MaxAggregateUncompressedBytes = 64L * 1024L * 1024L;
        private const long MaxArchiveBytes = 64L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly DateTimeOffset FixedZipTimestamp = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static void Export(string path, IReadOnlyList<CurtainWallScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var rowCount = rows.Count;
            if (rowCount > MaxDataRows) throw new ArgumentOutOfRangeException(nameof(rows), "Curtain XLSX export supports at most " + MaxDataRows + " data rows.");
            var snapshot = new List<CurtainWallScheduleRow>(rowCount);
            var sourceRows = new List<CurtainWallScheduleRow>(rowCount);
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var sourceRow = rows[rowIndex];
                if (sourceRow == null)
                    throw new ArgumentException("Export rows cannot contain null entries. Invalid row index: " + rowIndex + ".", nameof(rows));
                var row = SnapshotRow(sourceRow, rowIndex);
                ValidateCellText(row.ProjectId, rowIndex, "Project ID");
                ValidateCellText(row.DrawingFingerprint, rowIndex, "Drawing Fingerprint");
                ValidateCellText(row.Floor, rowIndex, "Floor");
                ValidateCellText(row.FamilyName, rowIndex, "FamilyName");
                ValidateJoinedCellText(row.ElementIds, rowIndex, "Element IDs");
                ValidateJoinedCellText(row.SourceHandles, rowIndex, "Source Handles");
                ValidateCount(row.WallCount, rowIndex, "WallCount");
                ValidateCount(row.PanelCount, rowIndex, "PanelCount");
                ValidateCount(row.VerticalFrameCount, rowIndex, "VerticalFrameCount");
                ValidateCount(row.HorizontalFrameCount, rowIndex, "HorizontalFrameCount");
                ValidateWallCardinality(row, rowIndex);
                ValidateXmlProvenance(row.ProjectId, rowIndex, "Project ID");
                ValidateXmlProvenance(row.DrawingFingerprint, rowIndex, "Drawing Fingerprint");
                ValidateXmlProvenance(row.ElementIds, rowIndex, "Element IDs");
                ValidateXmlProvenance(row.SourceHandles, rowIndex, "Source Handles");
                ValidateNonNegative(row.TotalWallLengthM, rowIndex, "TotalWallLengthM");
                ValidateNonNegative(row.GrossWallAreaM2, rowIndex, "GrossWallAreaM2");
                ValidateNonNegative(row.OpeningAreaM2, rowIndex, "OpeningAreaM2");
                ValidateNonNegative(row.NetGlassAreaM2, rowIndex, "NetGlassAreaM2");
                ValidateNonNegative(row.FrameFaceAreaM2, rowIndex, "FrameFaceAreaM2");
                ValidateNonNegative(row.FrameLengthM, rowIndex, "FrameLengthM");
                ValidateNonNegative(row.MinimumClearPanelWidthM, rowIndex, "MinimumClearPanelWidthM");
                ValidateNonNegative(row.MaximumClearPanelWidthM, rowIndex, "MaximumClearPanelWidthM");
                ValidateNonNegative(row.MinimumClearPanelHeightM, rowIndex, "MinimumClearPanelHeightM");
                ValidateNonNegative(row.MaximumClearPanelHeightM, rowIndex, "MaximumClearPanelHeightM");
                ValidateRange(row.MinimumClearPanelWidthM, row.MaximumClearPanelWidthM, rowIndex, "clear-panel width");
                ValidateRange(row.MinimumClearPanelHeightM, row.MaximumClearPanelHeightM, rowIndex, "clear-panel height");
                sourceRows.Add(sourceRow);
                snapshot.Add(row);
            }
            if (rows.Count != rowCount)
                throw new InvalidOperationException("Curtain XLSX export row count changed during snapshot.");
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
                if (new FileInfo(tempPath).Length > MaxArchiveBytes) throw new InvalidDataException("Curtain XLSX archive exceeds the bounded output contract.");
                ValidatePackage(tempPath);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally { AtomicFileCommit.TryDelete(tempPath); }
        }

        private static CurtainWallScheduleRow SnapshotRow(CurtainWallScheduleRow source, int rowIndex)
        {
            var row = new CurtainWallScheduleRow
            {
                ProjectId = source.ProjectId ?? string.Empty,
                DrawingFingerprint = source.DrawingFingerprint ?? string.Empty,
                Floor = source.Floor ?? string.Empty,
                FamilyName = source.FamilyName ?? string.Empty,
                WallCount = source.WallCount,
                TotalWallLengthM = source.TotalWallLengthM,
                GrossWallAreaM2 = source.GrossWallAreaM2,
                OpeningAreaM2 = source.OpeningAreaM2,
                NetGlassAreaM2 = source.NetGlassAreaM2,
                FrameFaceAreaM2 = source.FrameFaceAreaM2,
                FrameLengthM = source.FrameLengthM,
                PanelCount = source.PanelCount,
                VerticalFrameCount = source.VerticalFrameCount,
                HorizontalFrameCount = source.HorizontalFrameCount,
                MinimumClearPanelWidthM = source.MinimumClearPanelWidthM,
                MaximumClearPanelWidthM = source.MaximumClearPanelWidthM,
                MinimumClearPanelHeightM = source.MinimumClearPanelHeightM,
                MaximumClearPanelHeightM = source.MaximumClearPanelHeightM
            };
            var label = "worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " ";
            SnapshotJoinedCellValues(source.ElementIds, row.ElementIds, label + "Element IDs");
            SnapshotJoinedCellValues(source.SourceHandles, row.SourceHandles, label + "Source Handles");
            return row;
        }

        private static void SnapshotJoinedCellValues(IList<string> source, IList<string> target, string label)
        {
            if (source == null)
                throw new ArgumentException("Curtain XLSX " + label + " collection is required.", "rows");

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
                        "Curtain XLSX " + label + " exceeds Excel's " + MaxCellTextCharacters + "-character cell text limit.");
                target.Add(value);
            }
            EnsureJoinedCellValuesStable(source, target, label);
        }

        private static void EnsureRowStable(CurtainWallScheduleRow source, CurtainWallScheduleRow snapshot, int rowIndex)
        {
            if (source == null ||
                !string.Equals(source.ProjectId ?? string.Empty, snapshot.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(source.DrawingFingerprint ?? string.Empty, snapshot.DrawingFingerprint, StringComparison.Ordinal) ||
                !string.Equals(source.Floor ?? string.Empty, snapshot.Floor, StringComparison.Ordinal) ||
                !string.Equals(source.FamilyName ?? string.Empty, snapshot.FamilyName, StringComparison.Ordinal) ||
                source.WallCount != snapshot.WallCount ||
                source.TotalWallLengthM != snapshot.TotalWallLengthM ||
                source.GrossWallAreaM2 != snapshot.GrossWallAreaM2 ||
                source.OpeningAreaM2 != snapshot.OpeningAreaM2 ||
                source.NetGlassAreaM2 != snapshot.NetGlassAreaM2 ||
                source.FrameFaceAreaM2 != snapshot.FrameFaceAreaM2 ||
                source.FrameLengthM != snapshot.FrameLengthM ||
                source.PanelCount != snapshot.PanelCount ||
                source.VerticalFrameCount != snapshot.VerticalFrameCount ||
                source.HorizontalFrameCount != snapshot.HorizontalFrameCount ||
                source.MinimumClearPanelWidthM != snapshot.MinimumClearPanelWidthM ||
                source.MaximumClearPanelWidthM != snapshot.MaximumClearPanelWidthM ||
                source.MinimumClearPanelHeightM != snapshot.MinimumClearPanelHeightM ||
                source.MaximumClearPanelHeightM != snapshot.MaximumClearPanelHeightM)
                throw new InvalidOperationException("Curtain XLSX export row values changed during snapshot. Invalid row index: " + rowIndex + ".");

            var label = "worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " ";
            EnsureJoinedCellValuesStable(source.ElementIds, snapshot.ElementIds, label + "Element IDs");
            EnsureJoinedCellValuesStable(source.SourceHandles, snapshot.SourceHandles, label + "Source Handles");
        }

        private static void EnsureJoinedCellValuesStable(IList<string> source, IList<string> snapshot, string label)
        {
            if (source == null || source.Count != snapshot.Count)
                throw new InvalidOperationException("Curtain XLSX " + label + " count changed during snapshot.");
            for (var index = 0; index < snapshot.Count; index++)
            {
                if (!string.Equals(source[index] ?? string.Empty, snapshot[index] ?? string.Empty, StringComparison.Ordinal))
                    throw new InvalidOperationException("Curtain XLSX " + label + " values changed during snapshot.");
            }
        }

        private static void WriteWorksheetEntry(ZipArchive archive, IReadOnlyList<CurtainWallScheduleRow> rows, ref long totalUncompressedBytes)
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

        private static void WriteSheet(TextWriter writer, IReadOnlyList<CurtainWallScheduleRow> rows)
        {
            var headers = new[]
            {
                "Tầng", "Family / Loại", "SL vách", "Dài vách (m)", "DT vách gộp (m²)", "DT cửa/lỗ (m²)",
                "DT kính net (m²)", "DT mặt khung (m²)", "Dài khung (m)", "SL panel", "SL khung đứng", "SL khung ngang",
                "Panel clear W min (m)", "Panel clear W max (m)", "Panel clear H min (m)", "Panel clear H max (m)",
                "Project ID", "Drawing Fingerprint", "Element IDs", "Source Handles"
            };
            var lastRow = Math.Max(1, rows.Count + 1);
            var range = "A1:T" + lastRow.ToString(CultureInfo.InvariantCulture);
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            writer.Write("<dimension ref=\""); writer.Write(range); writer.Write("\"/>");
            writer.Write("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            writer.Write("<cols>");
            writer.Write("<col min=\"1\" max=\"2\" width=\"22\" customWidth=\"1\"/>");
            writer.Write("<col min=\"3\" max=\"16\" width=\"18\" customWidth=\"1\"/>");
            writer.Write("<col min=\"17\" max=\"20\" width=\"36\" customWidth=\"1\"/>");
            writer.Write("</cols><sheetData>");
            writer.Write("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) AppendInlineStringCell(writer, CellRef(c, 1), headers[c], 1);
            writer.Write("</row>");

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var r = i + 2;
                writer.Write("<row r=\""); writer.Write(r.ToString(CultureInfo.InvariantCulture)); writer.Write("\">");
                AppendInlineStringCell(writer, CellRef(0, r), row.Floor, 0);
                AppendInlineStringCell(writer, CellRef(1, r), row.FamilyName, 0);
                AppendNumberCell(writer, CellRef(2, r), row.WallCount);
                AppendNumberCell(writer, CellRef(3, r), row.TotalWallLengthM);
                AppendNumberCell(writer, CellRef(4, r), row.GrossWallAreaM2);
                AppendNumberCell(writer, CellRef(5, r), row.OpeningAreaM2);
                AppendNumberCell(writer, CellRef(6, r), row.NetGlassAreaM2);
                AppendNumberCell(writer, CellRef(7, r), row.FrameFaceAreaM2);
                AppendNumberCell(writer, CellRef(8, r), row.FrameLengthM);
                AppendNumberCell(writer, CellRef(9, r), row.PanelCount);
                AppendNumberCell(writer, CellRef(10, r), row.VerticalFrameCount);
                AppendNumberCell(writer, CellRef(11, r), row.HorizontalFrameCount);
                AppendNumberCell(writer, CellRef(12, r), row.MinimumClearPanelWidthM);
                AppendNumberCell(writer, CellRef(13, r), row.MaximumClearPanelWidthM);
                AppendNumberCell(writer, CellRef(14, r), row.MinimumClearPanelHeightM);
                AppendNumberCell(writer, CellRef(15, r), row.MaximumClearPanelHeightM);
                AppendInlineStringCell(writer, CellRef(16, r), row.ProjectId, 0);
                AppendInlineStringCell(writer, CellRef(17, r), row.DrawingFingerprint, 0);
                AppendInlineStringCell(writer, CellRef(18, r), string.Join(";", row.ElementIds), 0);
                AppendInlineStringCell(writer, CellRef(19, r), string.Join(";", row.SourceHandles), 0);
                writer.Write("</row>");
            }
            writer.Write("</sheetData><autoFilter ref=\""); writer.Write(range); writer.Write("\"/></worksheet>");
        }

        private static void ValidatePackage(string path)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                foreach (var name in new[] { "[Content_Types].xml", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml" })
                    if (archive.GetEntry(name) == null) throw new InvalidDataException("Generated curtain XLSX package is missing " + name + ".");
            }
        }

        private static void ValidateCellText(string value, int rowIndex, string fieldName)
        {
            var text = value ?? string.Empty;
            if (text.Length > MaxCellTextCharacters)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Curtain XLSX row " + rowIndex + " field " + fieldName + " exceeds Excel's " + MaxCellTextCharacters + "-character cell text limit.");
        }

        private static void ValidateJoinedCellText(IList<string> values, int rowIndex, string fieldName)
        {
            if (values == null)
                throw new ArgumentException("Curtain XLSX row " + rowIndex + " field " + fieldName + " collection is required.", "rows");
            long length = values.Count > 0 ? values.Count - 1L : 0L;
            for (var index = 0; index < values.Count; index++)
                length += (values[index] ?? string.Empty).Length;
            if (length > MaxCellTextCharacters)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Curtain XLSX row " + rowIndex + " field " + fieldName + " exceeds Excel's " + MaxCellTextCharacters + "-character cell text limit.");
        }

        private static void ValidateWallCardinality(CurtainWallScheduleRow row, int rowIndex)
        {
            if (row.WallCount != row.ElementIds.Count)
                throw new ArgumentException(
                    "Curtain XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " WallCount must match Element IDs count.",
                    "rows");
            if (row.SourceHandles.Count != row.ElementIds.Count)
                throw new ArgumentException(
                    "Curtain XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " Source Handles count must match Element IDs count.",
                    "rows");
        }

        private static void ValidateXmlProvenance(string value, int rowIndex, string fieldName)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value ?? string.Empty);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(
                    "Curtain XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " field " + fieldName + " contains characters invalid in XML provenance.",
                    "rows",
                    ex);
            }
        }

        private static void ValidateXmlProvenance(IEnumerable<string> values, int rowIndex, string fieldName)
        {
            var index = 0;
            foreach (var value in values)
            {
                ValidateXmlProvenance(value ?? string.Empty, rowIndex, fieldName + "[" + index.ToString(CultureInfo.InvariantCulture) + "]");
                index++;
            }
        }

        private static void ValidateCount(int value, int rowIndex, string fieldName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Curtain XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " field " + fieldName + " must be non-negative.");
        }

        private static void ValidateNonNegative(double value, int rowIndex, string fieldName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Curtain XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " field " + fieldName + " must be finite and non-negative.");
        }

        private static void ValidateRange(double minimum, double maximum, int rowIndex, string label)
        {
            if (minimum > maximum)
                throw new ArgumentOutOfRangeException(
                    "rows",
                    "Curtain XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " " + label + " minimum cannot exceed maximum.");
        }

        private static void AppendInlineStringCell(TextWriter writer, string cellRef, string value, int style)
        {
            writer.Write("<c r=\"");
            writer.Write(cellRef);
            writer.Write("\" t=\"inlineStr\" s=\"");
            writer.Write(style.ToString(CultureInfo.InvariantCulture));
            writer.Write("\"><is><t");
            if (!string.IsNullOrEmpty(value) && (IsXmlWhitespace(value[0]) || IsXmlWhitespace(value[value.Length - 1]))) writer.Write(" xml:space=\"preserve\"");
            writer.Write(">");
            writer.Write(XlsxXmlText.Escape(value));
            writer.Write("</t></is></c>");
        }

        private static bool IsXmlWhitespace(char value) => value == ' ' || value == '\t' || value == '\n' || value == '\r';

        private static void AppendNumberCell(TextWriter writer, string cellRef, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Curtain XLSX numeric values must be finite.");
            writer.Write("<c r=\"");
            writer.Write(cellRef);
            writer.Write("\" s=\"2\"><v>");
            writer.Write(value.ToString("R", CultureInfo.InvariantCulture));
            writer.Write("</v></c>");
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
            var bytes = StrictUtf8.GetBytes(content); ReserveUncompressed(bytes.LongLength, ref totalUncompressedBytes, name);
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal); entry.LastWriteTime = FixedZipTimestamp;
            using (var target = entry.Open()) target.Write(bytes, 0, bytes.Length);
        }

        private static void ReserveUncompressed(long entryBytes, ref long totalUncompressedBytes, string name)
        {
            if (entryBytes < 0L || entryBytes > MaxWorksheetEntryBytes) throw new InvalidDataException("Curtain XLSX worksheet exceeds the bounded output contract: " + name + ".");
            var projected = checked(totalUncompressedBytes + entryBytes);
            if (projected > MaxAggregateUncompressedBytes) throw new InvalidDataException("Curtain XLSX aggregate uncompressed XML exceeds the bounded output contract.");
            totalUncompressedBytes = projected;
        }

        private sealed class BoundedEntryWriteStream : Stream
        {
            private readonly Stream _inner; private readonly long _maxLength;
            internal BoundedEntryWriteStream(Stream inner, long maxLength) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); _maxLength = maxLength; }
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true; public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush(); public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException(); public override long Seek(long o,SeekOrigin x)=>throw new NotSupportedException(); public override void SetLength(long v)=>throw new NotSupportedException();
            public override void Write(byte[] b,int o,int c) { if (checked(_inner.Position+c)>_maxLength) throw new InvalidDataException("Curtain XLSX worksheet exceeds the bounded output contract."); _inner.Write(b,o,c); }
            public override void WriteByte(byte v) { if (checked(_inner.Position+1L)>_maxLength) throw new InvalidDataException("Curtain XLSX worksheet exceeds the bounded output contract."); _inner.WriteByte(v); }
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
            private static InvalidDataException Exceeded()=>new InvalidDataException("Curtain XLSX archive exceeds the bounded output contract.");
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Vách Kính\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}
