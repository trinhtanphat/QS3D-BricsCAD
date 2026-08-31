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
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
                {
                    WriteEntry(archive, "[Content_Types].xml", ContentTypesXml);
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml);
                    WriteEntry(archive, "xl/workbook.xml", WorkbookXml);
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml);
                    WriteEntry(archive, "xl/styles.xml", StylesXml);
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheet(snapshot));
                }
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

        private static string BuildSheet(IReadOnlyList<CurtainWallScheduleRow> rows)
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
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<dimension ref=\"").Append(range).Append("\"/>");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sb.Append("<cols>");
            sb.Append("<col min=\"1\" max=\"2\" width=\"22\" customWidth=\"1\"/>");
            sb.Append("<col min=\"3\" max=\"16\" width=\"18\" customWidth=\"1\"/>");
            sb.Append("<col min=\"17\" max=\"20\" width=\"36\" customWidth=\"1\"/>");
            sb.Append("</cols><sheetData>");
            sb.Append("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) AppendInlineStringCell(sb, CellRef(c, 1), headers[c], 1);
            sb.Append("</row>");

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var r = i + 2;
                sb.Append("<row r=\"").Append(r).Append("\">");
                AppendInlineStringCell(sb, CellRef(0, r), row.Floor, 0);
                AppendInlineStringCell(sb, CellRef(1, r), row.FamilyName, 0);
                AppendNumberCell(sb, CellRef(2, r), row.WallCount);
                AppendNumberCell(sb, CellRef(3, r), row.TotalWallLengthM);
                AppendNumberCell(sb, CellRef(4, r), row.GrossWallAreaM2);
                AppendNumberCell(sb, CellRef(5, r), row.OpeningAreaM2);
                AppendNumberCell(sb, CellRef(6, r), row.NetGlassAreaM2);
                AppendNumberCell(sb, CellRef(7, r), row.FrameFaceAreaM2);
                AppendNumberCell(sb, CellRef(8, r), row.FrameLengthM);
                AppendNumberCell(sb, CellRef(9, r), row.PanelCount);
                AppendNumberCell(sb, CellRef(10, r), row.VerticalFrameCount);
                AppendNumberCell(sb, CellRef(11, r), row.HorizontalFrameCount);
                AppendNumberCell(sb, CellRef(12, r), row.MinimumClearPanelWidthM);
                AppendNumberCell(sb, CellRef(13, r), row.MaximumClearPanelWidthM);
                AppendNumberCell(sb, CellRef(14, r), row.MinimumClearPanelHeightM);
                AppendNumberCell(sb, CellRef(15, r), row.MaximumClearPanelHeightM);
                AppendInlineStringCell(sb, CellRef(16, r), row.ProjectId, 0);
                AppendInlineStringCell(sb, CellRef(17, r), row.DrawingFingerprint, 0);
                AppendInlineStringCell(sb, CellRef(18, r), string.Join(";", row.ElementIds), 0);
                AppendInlineStringCell(sb, CellRef(19, r), string.Join(";", row.SourceHandles), 0);
                sb.Append("</row>");
            }
            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>");
            return sb.ToString();
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

        private static void AppendInlineStringCell(StringBuilder sb, string cellRef, string value, int style)
        {
            sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is>");
            XlsxXmlText.AppendTextElement(sb, value);
            sb.Append("</is></c>");
        }

        private static void AppendNumberCell(StringBuilder sb, string cellRef, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Curtain XLSX numeric values must be finite.");
            sb.Append("<c r=\"").Append(cellRef).Append("\" s=\"2\"><v>")
                .Append(value.ToString("R", CultureInfo.InvariantCulture)).Append("</v></c>");
        }

        private static string CellRef(int columnZeroBased, int row)
        {
            var n = columnZeroBased + 1;
            var name = string.Empty;
            while (n > 0) { n--; name = (char)('A' + (n % 26)) + name; n /= 26; }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Vách Kính\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}
