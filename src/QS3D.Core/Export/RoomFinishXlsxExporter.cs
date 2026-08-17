using System;
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

        public static void Export(string path, IReadOnlyList<RoomFinishScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var rowCount = rows.Count;
            if (rowCount > MaxDataRows) throw new ArgumentOutOfRangeException(nameof(rows), "Room-finish XLSX export supports at most " + MaxDataRows + " data rows.");
            var snapshot = new List<RoomFinishScheduleRow>(rowCount);
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var sourceRow = rows[rowIndex];
                if (sourceRow == null)
                    throw new ArgumentException("Export rows cannot contain null entries. Invalid row index: " + rowIndex + ".", nameof(rows));
                var row = SnapshotRow(sourceRow, rowIndex);
                ValidateCellText(row.Floor, rowIndex, "Floor");
                ValidateCellText(row.Room, rowIndex, "Room");
                ValidateCellText(row.Category, rowIndex, "Category");
                ValidateCellText(row.FamilyName, rowIndex, "FamilyName");
                ValidateCellText(row.Material, rowIndex, "Material");
                ValidateCellText(row.UnitHint, rowIndex, "UnitHint");
                ValidateJoinedCellText(row.ElementIds, rowIndex, "ElementIds");
                ValidateJoinedCellText(row.RoomIds, rowIndex, "RoomIds");
                ValidateNonNegativeCount(row.Count, rowIndex);
                ValidateNonNegativeFinite(row.PrimaryQuantity, rowIndex, "PrimaryQuantity");
                ValidateNonNegativeFinite(row.LengthM, rowIndex, "LengthM");
                ValidateNonNegativeFinite(row.AreaM2, rowIndex, "AreaM2");
                snapshot.Add(row);
            }
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
                {
                    Write(archive, "[Content_Types].xml", ContentTypesXml);
                    Write(archive, "_rels/.rels", RootRelationshipsXml);
                    Write(archive, "xl/workbook.xml", WorkbookXml);
                    Write(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml);
                    Write(archive, "xl/styles.xml", StylesXml);
                    Write(archive, "xl/worksheets/sheet1.xml", BuildSheet(snapshot));
                }
                Validate(tempPath);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally { AtomicFileCommit.TryDelete(tempPath); }
        }

        private static RoomFinishScheduleRow SnapshotRow(RoomFinishScheduleRow source, int rowIndex)
        {
            var row = new RoomFinishScheduleRow
            {
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
        }

        private static string BuildSheet(IReadOnlyList<RoomFinishScheduleRow> rows)
        {
            var headers = new[] { "Tầng", "Phòng", "Loại hoàn thiện", "Family / Loại", "Vật liệu", "Đơn vị", "SL", "KL chính", "Dài (m)", "Diện tích (m²)", "Element IDs", "Room IDs" };
            var lastRow = Math.Max(1, rows.Count + 1);
            var range = "A1:L" + lastRow.ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"").Append(range).Append("\"/>");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sb.Append("<cols><col min=\"1\" max=\"6\" width=\"20\" customWidth=\"1\"/><col min=\"7\" max=\"10\" width=\"15\" customWidth=\"1\"/><col min=\"11\" max=\"12\" width=\"36\" customWidth=\"1\"/></cols><sheetData>");
            sb.Append("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) StringCell(sb, CellRef(c, 1), headers[c], 1);
            sb.Append("</row>");
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var r = index + 2;
                sb.Append("<row r=\"").Append(r).Append("\">");
                StringCell(sb, CellRef(0, r), row.Floor, 0);
                StringCell(sb, CellRef(1, r), row.Room, 0);
                StringCell(sb, CellRef(2, r), row.Category, 0);
                StringCell(sb, CellRef(3, r), row.FamilyName, 0);
                StringCell(sb, CellRef(4, r), row.Material, 0);
                StringCell(sb, CellRef(5, r), row.UnitHint, 0);
                NumberCell(sb, CellRef(6, r), row.Count);
                NumberCell(sb, CellRef(7, r), row.PrimaryQuantity);
                NumberCell(sb, CellRef(8, r), row.LengthM);
                NumberCell(sb, CellRef(9, r), row.AreaM2);
                StringCell(sb, CellRef(10, r), string.Join(";", row.ElementIds), 0);
                StringCell(sb, CellRef(11, r), string.Join(";", row.RoomIds), 0);
                sb.Append("</row>");
            }
            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>");
            return sb.ToString();
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

        private static void StringCell(StringBuilder sb, string cellRef, string value, int style)
        {
            sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is><t>").Append(XlsxXmlText.Escape(value)).Append("</t></is></c>");
        }

        private static void NumberCell(StringBuilder sb, string cellRef, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Room-finish XLSX numeric values must be finite.");
            var text = value == 0d ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
            sb.Append("<c r=\"").Append(cellRef).Append("\" s=\"2\"><v>").Append(text).Append("</v></c>");
        }

        private static string CellRef(int columnZeroBased, int row)
        {
            var n = columnZeroBased + 1; var name = string.Empty;
            while (n > 0) { n--; name = (char)('A' + n % 26) + name; n /= 26; }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"HT Phòng\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}
