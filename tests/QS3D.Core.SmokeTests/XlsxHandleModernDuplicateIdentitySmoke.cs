using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleModernDuplicateIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsDuplicateModernElementIds();
            RejectsDuplicateModernHandleAliases();
            PreservesUniqueModernIdentitySets();
            PreservesLegacyHandleDeduplication();
        }

        private static void RejectsDuplicateModernElementIds()
        {
            RejectModern("E1;E1", "A", "duplicate Element ID");
            RejectModern("E1;e1", "A", "duplicate Element ID");
        }

        private static void RejectsDuplicateModernHandleAliases()
        {
            RejectModern("E1", "A;A", "duplicate CAD Handle");
            RejectModern("E1", "AB;ab", "duplicate CAD Handle");
            RejectModern("E1", "A;0xA", "duplicate CAD Handle");
            RejectModern("E1", "A;00A", "duplicate CAD Handle");
        }

        private static void PreservesUniqueModernIdentitySets()
        {
            var ed2Path = CreateModernWorkbook("E1", "A;B", true);
            var standardPath = CreateModernWorkbook("E1;E2", "A;B", false);
            try
            {
                var ed2 = XlsxHandleReader.ReadHandleLookup(ed2Path, 2);
                if (!ed2.IsModernSchema || !ed2.IsEd2Detail || ed2.ElementIds.Count != 1 || ed2.Handles.Count != 2)
                    throw new Exception("Unique ED2 identity provenance must remain readable.");

                var standard = XlsxHandleReader.ReadHandleLookup(standardPath, 2);
                if (!standard.IsModernSchema || standard.IsEd2Detail || standard.ElementIds.Count != 2 || standard.Handles.Count != 2)
                    throw new Exception("Unique standard BQ aggregate identity provenance must remain readable.");
            }
            finally
            {
                Delete(ed2Path);
                Delete(standardPath);
            }
        }

        private static void PreservesLegacyHandleDeduplication()
        {
            var fuzzyPath = CreateLegacyWorkbook(
                "<row r=\"1\">" + Cell("A1", "Object Handle") + "</row>" +
                "<row r=\"2\">" + Cell("A2", "A;0A") + "</row>");
            var decimalPath = CreateLegacyWorkbook("<row r=\"2\">" + Cell("A2", "$10$10") + "</row>");
            try
            {
                var fuzzy = XlsxHandleReader.ReadHandleLookup(fuzzyPath, 2);
                if (fuzzy.IsModernSchema || fuzzy.UsesLegacyDecimalHandles || fuzzy.Handles.Count != 1 || fuzzy.Handles[0] != "A")
                    throw new Exception("Fuzzy legacy Handle deduplication must remain compatible.");

                var legacy = XlsxHandleReader.ReadHandleLookup(decimalPath, 2);
                if (!legacy.UsesLegacyDecimalHandles || legacy.Handles.Count != 1 || legacy.Handles[0] != "A")
                    throw new Exception("Legacy BLT decimal Handle deduplication must remain compatible.");
            }
            finally
            {
                Delete(fuzzyPath);
                Delete(decimalPath);
            }
        }

        private static void RejectModern(string elementIds, string handles, string expectedMessage)
        {
            var path = CreateModernWorkbook(elementIds, handles, true);
            try
            {
                try { XlsxHandleReader.ReadHandleLookup(path, 2); }
                catch (InvalidDataException ex)
                {
                    if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0) return;
                    throw new Exception("Modern duplicate identity refusal lost its field-level diagnostic.", ex);
                }
                throw new Exception("Modern XLSX accepted duplicate identity provenance: " + elementIds + " / " + handles + ".");
            }
            finally { Delete(path); }
        }

        private static string CreateModernWorkbook(string elementIds, string handles, bool ed2Detail)
        {
            var rows =
                "<row r=\"1\">" +
                Cell("A1", "QS3D Element ID") +
                Cell("B1", "CAD Handle (hex)") +
                Cell("C1", "QS3D Drawing Fingerprint") +
                "</row>" +
                "<row r=\"2\">" +
                Cell("A2", elementIds) +
                Cell("B2", handles) +
                Cell("C2", "DRAWING-1") +
                "</row>";
            if (!ed2Detail) return CreateLegacyWorkbook(rows);

            var path = NewPath();
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Write(archive, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"CHI_TIET\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                Write(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                WriteSheet(archive, rows);
            }
            return path;
        }

        private static string CreateLegacyWorkbook(string rows)
        {
            var path = NewPath();
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create)) WriteSheet(archive, rows);
            return path;
        }

        private static void WriteSheet(ZipArchive archive, string rows) => Write(
            archive,
            "xl/worksheets/sheet1.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" + rows + "</sheetData></worksheet>");

        private static void Write(ZipArchive archive, string name, string value)
        {
            using (var writer = new StreamWriter(archive.CreateEntry(name, CompressionLevel.NoCompression).Open(), new UTF8Encoding(false)))
                writer.Write(value);
        }

        private static string Cell(string reference, string value) =>
            "<c r=\"" + reference + "\" t=\"inlineStr\"><is><t>" + Escape(value) + "</t></is></c>";

        private static string Escape(string value) =>
            (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static string NewPath() => Path.Combine(
            Path.GetTempPath(),
            "qs3d-xlsx-modern-duplicate-identity-" + Guid.NewGuid().ToString("N") + ".xlsx");

        private static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
