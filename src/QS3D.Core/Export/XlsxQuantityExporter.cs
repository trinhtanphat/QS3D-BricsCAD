using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    public static class XlsxQuantityExporter
    {
        public static void Export(string path, IReadOnlyList<QuantityReportRow> rows)
        {
<<<<<<< origin/main
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));

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
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheet(rows));
                }
                ValidatePackage(tempPath);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
=======
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path)); if (rows == null) throw new ArgumentNullException(nameof(rows)); var fullPath = Path.GetFullPath(path); var directory = Path.GetDirectoryName(fullPath); if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory); if (File.Exists(fullPath)) File.Delete(fullPath);
            using (var stream = File.Create(fullPath)) using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8)) { WriteEntry(archive, "[Content_Types].xml", ContentTypesXml); WriteEntry(archive, "_rels/.rels", RootRelationshipsXml); WriteEntry(archive, "xl/workbook.xml", WorkbookXml); WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml); WriteEntry(archive, "xl/styles.xml", StylesXml); WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheet(rows)); }
>>>>>>> origin/ci/full-domain-integration-final-20260810
        }
        private static string BuildSheet(IReadOnlyList<QuantityReportRow> rows)
        {
            var headers = new[] { "Tầng", "Loại", "Tên cấu kiện", "SL", "BT gộp (m³)", "Trừ giao (m³)", "BT còn (m³)", "Cốp pha (m²)", "Dài (m)", "Chu vi ngoài (m)", "Chu vi trong (m)", "DT cửa (m²)", "Thành bên (m²)", "DT đáy (m²)", "DT đỉnh (m²)", "DT khác (m²)", "Thép (kg)" }; var lastRow = Math.Max(1, rows.Count + 1); var range = "A1:Q" + lastRow.ToString(CultureInfo.InvariantCulture); var sb = new StringBuilder(); sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"").Append(range).Append("\"/><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetData><row r=\"1\">"); for (var c = 0; c < headers.Length; c++) AppendInlineStringCell(sb, CellRef(c, 1), headers[c], 1); sb.Append("</row>");
            for (var i = 0; i < rows.Count; i++) { var row = rows[i]; var r = i + 2; sb.Append("<row r=\"").Append(r).Append("\">"); AppendInlineStringCell(sb, CellRef(0,r), row.Floor,0); AppendInlineStringCell(sb, CellRef(1,r), row.Category,0); AppendInlineStringCell(sb, CellRef(2,r), row.FamilyName,0); AppendNumberCell(sb, CellRef(3,r), row.Count); AppendNumberCell(sb, CellRef(4,r), row.GrossConcreteM3); AppendNumberCell(sb, CellRef(5,r), row.DeductionM3); AppendNumberCell(sb, CellRef(6,r), row.NetConcreteM3); AppendNumberCell(sb, CellRef(7,r), row.FormworkM2); AppendNumberCell(sb, CellRef(8,r), row.LengthM); AppendNumberCell(sb, CellRef(9,r), row.OuterPerimeterM); AppendNumberCell(sb, CellRef(10,r), row.InnerPerimeterM); AppendNumberCell(sb, CellRef(11,r), row.DoorAreaM2); AppendNumberCell(sb, CellRef(12,r), row.SideAreaM2); AppendNumberCell(sb, CellRef(13,r), row.BottomAreaM2); AppendNumberCell(sb, CellRef(14,r), row.TopAreaM2); AppendNumberCell(sb, CellRef(15,r), row.OtherAreaM2); AppendNumberCell(sb, CellRef(16,r), row.SteelWeightKg); sb.Append("</row>"); }
            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>"); return sb.ToString();
        }
<<<<<<< origin/main

        private static void ValidatePackage(string path)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                foreach (var name in new[] { "[Content_Types].xml", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml" })
                    if (archive.GetEntry(name) == null) throw new InvalidDataException("Generated XLSX package is missing " + name + ".");
            }
        }

        private static void AppendInlineStringCell(StringBuilder sb, string cellRef, string value, int style)
        {
            sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is><t>")
                .Append(SecurityElement.Escape(value ?? string.Empty)).Append("</t></is></c>");
        }

        private static void AppendNumberCell(StringBuilder sb, string cellRef, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "XLSX numeric values must be finite.");
            sb.Append("<c r=\"").Append(cellRef).Append("\" s=\"2\"><v>")
                .Append(value.ToString("0.########", CultureInfo.InvariantCulture)).Append("</v></c>");
        }

        private static string CellRef(int columnZeroBased, int row)
        {
            var n = columnZeroBased + 1;
            var name = string.Empty;
            while (n > 0)
            {
                n--;
                name = (char)('A' + (n % 26)) + name;
                n /= 26;
            }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

=======
        private static void AppendInlineStringCell(StringBuilder sb, string cellRef, string value, int style) => sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is><t>").Append(SecurityElement.Escape(value ?? string.Empty)).Append("</t></is></c>");
        private static void AppendNumberCell(StringBuilder sb, string cellRef, double value) => sb.Append("<c r=\"").Append(cellRef).Append("\" s=\"2\"><v>").Append(value.ToString("0.########", CultureInfo.InvariantCulture)).Append("</v></c>");
        private static string CellRef(int columnZeroBased, int row) { var n = columnZeroBased + 1; var name = string.Empty; while (n > 0) { n--; name = (char)('A' + (n % 26)) + name; n /= 26; } return name + row.ToString(CultureInfo.InvariantCulture); }
        private static void WriteEntry(ZipArchive archive, string name, string content) { var entry = archive.CreateEntry(name, CompressionLevel.Optimal); using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content); }
>>>>>>> origin/ci/full-domain-integration-final-20260810
        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Khối lượng\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}
