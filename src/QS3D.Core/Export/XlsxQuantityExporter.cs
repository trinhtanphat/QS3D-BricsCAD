using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            ExportCore(path, rows, null);
        }

        public static void ExportEd2(string path, IReadOnlyList<QuantityReportRow> detailRows, IReadOnlyList<QuantityReportRow> summaryRows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (detailRows == null) throw new ArgumentNullException(nameof(detailRows));
            if (summaryRows == null) throw new ArgumentNullException(nameof(summaryRows));
            var detailIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var detailHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? drawingFingerprint = null;
            foreach (var row in detailRows)
            {
                if (row == null) throw new InvalidDataException("ED2 CHI_TIET contains a null row.");
                if (row.Count != 1 || row.ElementIds.Count != 1)
                    throw new InvalidDataException("ED2 CHI_TIET must contain exactly one semantic element per row.");
                var elementId = Required(row.ElementIds[0], "ED2 CHI_TIET Element ID");
                if (!detailIds.Add(elementId)) throw new InvalidDataException("ED2 CHI_TIET contains duplicate Element ID: " + elementId + ".");
                if (row.SourceHandles.Count == 0) throw new InvalidDataException("ED2 CHI_TIET row " + elementId + " has no CAD Handle provenance.");
                foreach (var handle in row.SourceHandles) detailHandles.Add(ValidHandle(handle, elementId));
                var fingerprint = Required(row.DrawingFingerprint, "ED2 drawing fingerprint");
                if (drawingFingerprint == null) drawingFingerprint = fingerprint;
                else if (!string.Equals(drawingFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("ED2 CHI_TIET contains conflicting drawing fingerprints.");
            }
            if (detailRows.Count == 0) throw new InvalidDataException("ED2 CHI_TIET must contain at least one row.");

            var summaryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var summaryHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var summaryCount = 0;
            foreach (var row in summaryRows)
            {
                if (row == null) throw new InvalidDataException("ED2 TONG_HOP contains a null row.");
                summaryCount = checked(summaryCount + row.Count);
                if (!string.Equals(Required(row.DrawingFingerprint, "ED2 TONG_HOP drawing fingerprint"), drawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("ED2 TONG_HOP drawing fingerprint does not match CHI_TIET.");
                foreach (var id in row.ElementIds)
                {
                    var elementId = Required(id, "ED2 TONG_HOP Element ID");
                    if (!summaryIds.Add(elementId)) throw new InvalidDataException("ED2 TONG_HOP repeats Element ID: " + elementId + ".");
                }
                foreach (var handle in row.SourceHandles) summaryHandles.Add(ValidHandle(handle, "TONG_HOP"));
            }
            if (summaryRows.Count == 0) throw new InvalidDataException("ED2 TONG_HOP must contain at least one row.");
            if (summaryCount != detailRows.Count || !summaryIds.SetEquals(detailIds) || !summaryHandles.SetEquals(detailHandles))
                throw new InvalidDataException("ED2 CHI_TIET and TONG_HOP do not describe the same semantic scope.");
            ExportCore(path, detailRows, summaryRows);
        }

        private static string Required(string? value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidDataException(label + " is required.");
            return normalized;
        }

        private static string ValidHandle(string? value, string owner)
        {
            var handle = Required(value, "ED2 CAD Handle for " + owner);
            var token = handle.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? handle.Substring(2) : handle;
            if (!long.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number) || number <= 0)
                throw new InvalidDataException("ED2 contains an invalid CAD Handle for " + owner + ": " + handle + ".");
            return number.ToString("X", CultureInfo.InvariantCulture);
        }

        private static void ExportCore(string path, IReadOnlyList<QuantityReportRow> rows, IReadOnlyList<QuantityReportRow>? summaryRows)
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
                {
                    var isEd2 = summaryRows != null;
                    WriteEntry(archive, "[Content_Types].xml", isEd2 ? Ed2ContentTypesXml : ContentTypesXml);
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml);
                    WriteEntry(archive, "xl/workbook.xml", isEd2 ? Ed2WorkbookXml : WorkbookXml);
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", isEd2 ? Ed2WorkbookRelationshipsXml : WorkbookRelationshipsXml);
                    WriteEntry(archive, "xl/styles.xml", StylesXml);
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheet(rows));
                    if (summaryRows != null) WriteEntry(archive, "xl/worksheets/sheet2.xml", BuildSheet(summaryRows));
                }
                ValidatePackage(tempPath, summaryRows != null);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static string BuildSheet(IReadOnlyList<QuantityReportRow> rows)
        {
            var headers = new[]
            {
                "Tầng", "Zone", "Loại", "Tên cấu kiện", "SL", "BT gộp (m³)", "Trừ giao (m³)", "BT còn (m³)",
                "Cốp pha (m²)", "Dài (m)", "Chu vi ngoài (m)", "Chu vi trong (m)", "DT cửa (m²)",
                "Thành bên (m²)", "DT đáy (m²)", "DT đỉnh (m²)", "DT khác (m²)",
                "QS3D Element ID", "CAD Handle (hex)", "QS3D Drawing Fingerprint"
            };

            var lastRow = Math.Max(1, rows.Count + 1);
            var range = "A1:T" + lastRow.ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<dimension ref=\"").Append(range).Append("\"/>");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sb.Append("<sheetData>");
            sb.Append("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) AppendInlineStringCell(sb, CellRef(c, 1), headers[c], 1);
            sb.Append("</row>");

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var r = i + 2;
                sb.Append("<row r=\"").Append(r).Append("\">");
                AppendInlineStringCell(sb, CellRef(0, r), row.Floor, 0);
                AppendInlineStringCell(sb, CellRef(1, r), row.Zone, 0);
                AppendInlineStringCell(sb, CellRef(2, r), row.Category, 0);
                AppendInlineStringCell(sb, CellRef(3, r), row.FamilyName, 0);
                AppendNumberCell(sb, CellRef(4, r), row.Count);
                AppendNumberCell(sb, CellRef(5, r), row.GrossConcreteM3);
                AppendNumberCell(sb, CellRef(6, r), row.DeductionM3);
                AppendNumberCell(sb, CellRef(7, r), row.NetConcreteM3);
                AppendNumberCell(sb, CellRef(8, r), row.FormworkM2);
                AppendNumberCell(sb, CellRef(9, r), row.LengthM);
                AppendNumberCell(sb, CellRef(10, r), row.OuterPerimeterM);
                AppendNumberCell(sb, CellRef(11, r), row.InnerPerimeterM);
                AppendNumberCell(sb, CellRef(12, r), row.DoorAreaM2);
                AppendNumberCell(sb, CellRef(13, r), row.SideAreaM2);
                AppendNumberCell(sb, CellRef(14, r), row.BottomAreaM2);
                AppendNumberCell(sb, CellRef(15, r), row.TopAreaM2);
                AppendNumberCell(sb, CellRef(16, r), row.OtherAreaM2);
                AppendInlineStringCell(sb, CellRef(17, r), row.ElementIdText, 0);
                AppendInlineStringCell(sb, CellRef(18, r), row.SourceHandleText, 0);
                AppendInlineStringCell(sb, CellRef(19, r), row.DrawingFingerprint, 0);
                sb.Append("</row>");
            }

            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>");
            return sb.ToString();
        }

        private static void ValidatePackage(string path, bool isEd2)
        {
            if (isEd2)
                XlsxPackageValidator.Validate(path, "[Content_Types].xml", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml");
            else
                XlsxPackageValidator.Validate(path, "[Content_Types].xml", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml");
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

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string Ed2ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Khối lượng\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string Ed2WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"CHI_TIET\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"TONG_HOP\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string Ed2WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}
