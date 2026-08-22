using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleReaderCoordinateSmoke
    {
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-coordinate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var valid = Path.Combine(root, "valid.xlsx");
                CreateWorkbook(valid, "A2", "B2", "C2");
                var lookup = XlsxHandleReader.ReadHandleLookup(valid, 2);
                if (!lookup.IsModernSchema || lookup.ElementIds.Count != 1 || lookup.Handles.Count != 1)
                    throw new InvalidOperationException("Valid XLSX coordinate fixture did not resolve the modern schema.");
                if (!string.Equals(lookup.ElementIds[0], "E-1", StringComparison.Ordinal) ||
                    !string.Equals(lookup.Handles[0], "1A", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(lookup.DrawingFingerprint, "FP-1", StringComparison.Ordinal))
                    throw new InvalidOperationException("Valid XLSX coordinate fixture resolved incorrect provenance values.");

                var mismatchedRow = Path.Combine(root, "mismatched-row.xlsx");
                CreateWorkbook(mismatchedRow, "A9", "B9", "C9");
                AssertInvalid(mismatchedRow, "does not match containing row 2");

                var missingRowSuffix = Path.Combine(root, "missing-row-suffix.xlsx");
                CreateWorkbook(missingRowSuffix, "A", "B2", "C2");
                AssertInvalid(missingRowSuffix, "cell reference is invalid");
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
                XlsxHandleReader.ReadHandleLookup(path, 2);
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("XLSX coordinate validation returned an unexpected error.", ex);
                return;
            }

            throw new InvalidOperationException("XLSX Handle reader accepted an invalid cell coordinate.");
        }

        private static void CreateWorkbook(string path, string elementCell, string handleCell, string fingerprintCell)
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
                Write(archive, "xl/worksheets/sheet1.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                    "<row r=\"1\">" +
                    Inline("A1", "QS3D Element ID") + Inline("B1", "CAD Handle (hex)") + Inline("C1", "QS3D Drawing Fingerprint") +
                    "</row><row r=\"2\">" +
                    Inline(elementCell, "E-1") + Inline(handleCell, "1A") + Inline(fingerprintCell, "FP-1") +
                    "</row></sheetData></worksheet>");
            }
        }

        private static string Inline(string reference, string value) =>
            "<c r=\"" + reference + "\" t=\"inlineStr\"><is><t>" + value + "</t></is></c>";

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }
    }
}
