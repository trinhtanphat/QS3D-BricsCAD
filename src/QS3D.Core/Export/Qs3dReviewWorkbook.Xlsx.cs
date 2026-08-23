using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    internal static class Qs3dReviewXlsx
    {
        internal const int HeaderStyle = 1;
        internal const int IntegerStyle = 2;
        internal const int DecimalStyle = 3;
        internal const int WrappedStyle = 4;

        internal static void WritePackage(string path, params string[] sheets)
        {
            if (sheets == null || sheets.Length != 6) throw new InvalidDataException("QS3D Review workbook requires exactly six worksheets.");
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
                {
                    Entry(archive, "[Content_Types].xml", ContentTypes);
                    Entry(archive, "_rels/.rels", RootRels);
                    Entry(archive, "xl/workbook.xml", Workbook);
                    Entry(archive, "xl/_rels/workbook.xml.rels", WorkbookRels);
                    Entry(archive, "xl/styles.xml", Styles);
                    for (var i = 0; i < sheets.Length; i++) Entry(archive, "xl/worksheets/sheet" + (i + 1).ToString(CultureInfo.InvariantCulture) + ".xml", sheets[i]);
                }
                XlsxPackageValidator.Validate(tempPath,
                    "[Content_Types].xml", "xl/workbook.xml", "xl/_rels/workbook.xml.rels", "xl/styles.xml",
                    "xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml", "xl/worksheets/sheet3.xml",
                    "xl/worksheets/sheet4.xml", "xl/worksheets/sheet5.xml", "xl/worksheets/sheet6.xml");
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally { AtomicFileCommit.TryDelete(tempPath); }
        }

        internal static StringBuilder Begin(string dimension, string columnsXml = "")
        {
            var sb = new StringBuilder(4096);
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"")
              .Append(Escape(dimension)).Append("\"/><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetFormatPr defaultRowHeight=\"15\"/>");
            if (!string.IsNullOrEmpty(columnsXml)) sb.Append(columnsXml);
            sb.Append("<sheetData>");
            return sb;
        }

        internal static string End(StringBuilder sb, string filter)
        {
            sb.Append("</sheetData>");
            if (!string.IsNullOrWhiteSpace(filter)) sb.Append("<autoFilter ref=\"").Append(Escape(filter)).Append("\"/>");
            return sb.Append("</worksheet>").ToString();
        }

        internal static void Header(StringBuilder sb, int row, params string[] values) => TextRow(sb, row, true, values);
        internal static void TextRow(StringBuilder sb, int row, bool header, params string[] values)
        {
            StartRow(sb, row);
            for (var i = 0; i < values.Length; i++) Text(sb, Cell(i, row), values[i], header ? HeaderStyle : 0);
            EndRow(sb);
        }
        internal static void StartRow(StringBuilder sb, int row) => sb.Append("<row r=\"").Append(row.ToString(CultureInfo.InvariantCulture)).Append("\">");
        internal static void EndRow(StringBuilder sb) => sb.Append("</row>");
        internal static void Text(StringBuilder sb, string cell, string value, int style = 0)
        {
            var text = value ?? string.Empty;
            Qs3dReviewModelInfo.VerifyXml(text, cell);
            sb.Append("<c r=\"").Append(cell).Append("\" t=\"inlineStr\"");
            if (style > 0) sb.Append(" s=\"").Append(style.ToString(CultureInfo.InvariantCulture)).Append("\"");
            sb.Append("><is><t xml:space=\"preserve\">").Append(Escape(text)).Append("</t></is></c>");
        }
        internal static void Number(StringBuilder sb, string cell, double value, int style = DecimalStyle)
        {
            if (!Finite(value)) throw new InvalidDataException("Cannot write a non-finite XLSX numeric value.");
            sb.Append("<c r=\"").Append(cell).Append("\"");
            if (style > 0) sb.Append(" s=\"").Append(style.ToString(CultureInfo.InvariantCulture)).Append("\"");
            sb.Append("><v>").Append(value.ToString("R", CultureInfo.InvariantCulture)).Append("</v></c>");
        }
        internal static void Integer(StringBuilder sb, string cell, int value) => Number(sb, cell, value, IntegerStyle);
        internal static void OptionalNumber(StringBuilder sb, string cell, double? value) { if (value.HasValue) Number(sb, cell, value.Value); }
        internal static void Evidence(StringBuilder sb, string cell, double value, bool hasEvidence) { if (hasEvidence) Number(sb, cell, value); }
        internal static string Cell(int column, int row) => Column(column) + row.ToString(CultureInfo.InvariantCulture);

        internal static string TraceKey(string kind, params string[] values)
        {
            using (var sha = SHA256.Create())
            {
                var payload = kind + "\u001f" + string.Join("\u001f", values.Select(value => value ?? string.Empty));
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var text = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return kind + ":" + text;
            }
        }

        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        internal static string Escape(string value) => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

        private static string Column(int column)
        {
            if (column < 0) throw new ArgumentOutOfRangeException(nameof(column));
            var value = column + 1;
            var sb = new StringBuilder();
            while (value > 0) { value--; sb.Insert(0, (char)('A' + value % 26)); value /= 26; }
            return sb.ToString();
        }
        private static void Entry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private static readonly string Workbook =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>" +
            "<sheet name=\"01_TONG_HOP\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"02_CHI_TIET_QTO\" sheetId=\"2\" r:id=\"rId2\"/>" +
            "<sheet name=\"03_CLASHES\" sheetId=\"3\" r:id=\"rId3\"/><sheet name=\"04_DUPLICATES\" sheetId=\"4\" r:id=\"rId4\"/>" +
            "<sheet name=\"05_RULES\" sheetId=\"5\" r:id=\"rId5\"/><sheet name=\"06_MODEL_INFO\" sheetId=\"6\" r:id=\"rId6\"/></sheets></workbook>";
        private const string RootRels = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookRels = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet3.xml\"/>" +
            "<Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet4.xml\"/>" +
            "<Relationship Id=\"rId5\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet5.xml\"/>" +
            "<Relationship Id=\"rId6\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet6.xml\"/>" +
            "<Relationship Id=\"rId7\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string ContentTypes = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet3.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet4.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet5.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet6.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>";
        private const string Styles = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
            "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"5\">" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
            "<xf numFmtId=\"1\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/><xf numFmtId=\"4\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment wrapText=\"1\" vertical=\"top\"/></xf></cellXfs>" +
            "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";
    }
}
