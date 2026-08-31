using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleReaderRowScanSmoke
    {
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-row-scan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var valid = Path.Combine(root, "valid.xlsx");
                CreateWorkbook(valid, duplicateTarget: false, invalidTrailingRow: false);
                var lookup = XlsxHandleReader.ReadHandleLookup(valid, 20);
                if (!lookup.IsModernSchema || lookup.ElementIds.Count != 1 || lookup.Handles.Count != 1)
                    throw new InvalidOperationException("Bounded XLSX row scan did not resolve the modern target row.");
                if (!string.Equals(lookup.ElementIds[0], "E-20", StringComparison.Ordinal) ||
                    !string.Equals(lookup.Handles[0], "1A", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(lookup.DrawingFingerprint, "FP-20", StringComparison.Ordinal))
                    throw new InvalidOperationException("Bounded XLSX row scan resolved incorrect target provenance.");

                var duplicate = Path.Combine(root, "duplicate.xlsx");
                CreateWorkbook(duplicate, duplicateTarget: true, invalidTrailingRow: false);
                AssertInvalid(duplicate, "duplicate row number 20");

                var invalidTrailing = Path.Combine(root, "invalid-trailing.xlsx");
                CreateWorkbook(invalidTrailing, duplicateTarget: false, invalidTrailingRow: true);
                AssertInvalid(invalidTrailing, "row number is invalid or exceeds");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertInvalid(string path, string expectedMessageFragment)
        {
            try
            {
                XlsxHandleReader.ReadHandleLookup(path, 20);
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("XLSX bounded row scan returned an unexpected validation error.", ex);
                return;
            }

            throw new InvalidOperationException("XLSX bounded row scan accepted an invalid worksheet.");
        }

        private static void CreateWorkbook(string path, bool duplicateTarget, bool invalidTrailingRow)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                Write(archive, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    "<sheets><sheet name=\"CHI_TIET\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                Write(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "</Relationships>");

                var sheet = new StringBuilder();
                sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sheet.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
                sheet.Append("<row r=\"1\">");
                sheet.Append(Inline("A1", "QS3D Element ID"));
                sheet.Append(Inline("B1", "CAD Handle (hex)"));
                sheet.Append(Inline("C1", "QS3D Drawing Fingerprint"));
                sheet.Append("</row>");
                for (var row = 2; row <= 11; row++)
                    sheet.Append("<row r=\"" + row + "\"><c r=\"A" + row + "\" t=\"inlineStr\"><is><t>noise</t></is></c></row>");
                sheet.Append(TargetRow());
                if (duplicateTarget) sheet.Append(TargetRow());
                sheet.Append("<row r=\"100\"><c r=\"A100\" t=\"inlineStr\"><is><t>tail</t></is></c></row>");
                if (invalidTrailingRow) sheet.Append("<row r=\"1048577\"/>");
                sheet.Append("</sheetData></worksheet>");
                Write(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
            }
        }

        private static string TargetRow() =>
            "<row r=\"20\">" +
            Inline("A20", "E-20") + Inline("B20", "1A") + Inline("C20", "FP-20") +
            "</row>";

        private static string Inline(string reference, string value) =>
            "<c r=\"" + reference + "\" t=\"inlineStr\"><is><t>" + value + "</t></is></c>";

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }
    }
}