using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;

namespace QS3D.Core.Export
{
    public static class XlsxRebarScheduleExporter
    {
        private const int MaxWorksheetRows = 1048576;
        private const int HeaderRows = 1;
        private const int MaxDataRows = MaxWorksheetRows - HeaderRows;

        public static void Export(string path, IReadOnlyList<RebarScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var rowCount = ValidateRowCount(rows);
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
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheet(rows, rowCount));
                }
                ValidatePackage(tempPath);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static int ValidateRowCount(IReadOnlyList<RebarScheduleRow> rows)
        {
            var count = rows.Count;
            if (count < 0 || count > MaxDataRows)
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    count,
                    "BBS XLSX data rows must be between 0 and " + MaxDataRows.ToString(CultureInfo.InvariantCulture) + " so the worksheet stays within its row limit.");
            return count;
        }

        private static string BuildSheet(IReadOnlyList<RebarScheduleRow> rows, int rowCount)
        {
            var headers = new[]
            {
                "Element", "Bar Mark", "Shape", "Notation", "Ø (mm)", "SL", "L cắt (m)", "Tổng L (m)",
                "kg/m", "KL net (kg)", "Hao hụt (%)", "KL tổng (kg)",
                "Fabrication Status", "Standard Code", "Detailing Revision"
            };
            var lastRow = Math.Max(1, rowCount + HeaderRows);
            var range = "A1:O" + lastRow.ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"").Append(range).Append("\"/>");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetData>");
            sb.Append("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) AppendText(sb, CellRef(c, 1), headers[c], 1);
            sb.Append("</row>");
            for (var i = 0; i < rowCount; i++)
            {
                var row = rows[i] ?? throw new ArgumentException("BBS row cannot be null.", nameof(rows));
                var r = i + 2;
                sb.Append("<row r=\"").Append(r).Append("\">");
                AppendText(sb, CellRef(0, r), row.ElementId, 0);
                AppendText(sb, CellRef(1, r), row.BarMark, 0);
                AppendText(sb, CellRef(2, r), row.ShapeCode, 0);
                AppendText(sb, CellRef(3, r), row.Notation, 0);
                AppendNumber(sb, CellRef(4, r), row.DiameterMm);
                AppendNumber(sb, CellRef(5, r), row.Quantity);
                AppendNumber(sb, CellRef(6, r), row.CuttingLengthM);
                AppendNumber(sb, CellRef(7, r), row.TotalLengthM);
                AppendNumber(sb, CellRef(8, r), row.UnitWeightKgM);
                AppendNumber(sb, CellRef(9, r), row.NetWeightKg);
                AppendNumber(sb, CellRef(10, r), row.WastePercent);
                AppendNumber(sb, CellRef(11, r), row.TotalWeightKg);
                AppendText(sb, CellRef(12, r), row.FabricationStatus, 0);
                AppendText(sb, CellRef(13, r), row.FabricationStandardCode, 0);
                AppendText(sb, CellRef(14, r), row.FabricationDetailingRevision, 0);
                sb.Append("</row>");
            }
            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>");
            return sb.ToString();
        }

        private static void ValidatePackage(string path)
        {
            XlsxPackageValidator.Validate(path, "[Content_Types].xml", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml");
        }

        private static void AppendText(StringBuilder sb, string cellRef, string value, int style)
        {
            sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is><t>").Append(SecurityElement.Escape(value ?? string.Empty)).Append("</t></is></c>");
        }

        private static void AppendNumber(StringBuilder sb, string cellRef, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "XLSX numeric values must be finite.");
            sb.Append("<c r=\"").Append(cellRef).Append("\" s=\"2\"><v>").Append(value.ToString("0.########", CultureInfo.InvariantCulture)).Append("</v></c>");
        }

        private static string CellRef(int columnZeroBased, int row)
        {
            var n = columnZeroBased + 1; var name = string.Empty;
            while (n > 0) { n--; name = (char)('A' + n % 26) + name; n /= 26; }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"BBS\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}
