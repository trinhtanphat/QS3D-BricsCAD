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
            RejectsFormulaBackedModernIdentity();
            PreservesUnrelatedModernFormulaCells();
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

        private static void RejectsFormulaBackedModernIdentity()
        {
            RejectModernFormula("A1", "\"Not an identity header\"", "QS3D Element ID");
            RejectModernFormula("A2", "\"E999\"", "E1");
            RejectModernFormula("B2", "\"B\"", "A");
            RejectModernFormula("C2", "\"DRAWING-OTHER\"", "DRAWING-1");
        }

        private static void PreservesUnrelatedModernFormulaCells()
        {
            var path = CreateModernWorkbook(
                "E1",
                "A",
                true,
                extraRow2Cell: FormulaCell("D2", "1+1", "2"));
            try
            {
                var result = XlsxHandleReader.ReadHandleLookup(path, 2);
                if (!result.IsModernSchema || !result.IsEd2Detail || result.ElementIds.Count != 1 || result.ElementIds[0] != "E1" || result.Handles.Count != 1 || result.Handles[0] != "A" || result.DrawingFingerprint != "DRAWING-1")
                    throw new Exception("Formula cells outside modern identity/provenance columns must not change Handle lookup behavior.");
            }
            finally { Delete(path); }
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
            var formulaPath = CreateLegacyWorkbook("<row r=\"2\">" + FormulaCell("A2", "\"$11$11\"", "$10$10") + "</row>");
            try
            {
                var fuzzy = XlsxHandleReader.ReadHandleLookup(fuzzyPath, 2);
                if (fuzzy.IsModernSchema || fuzzy.UsesLegacyDecimalHandles || fuzzy.Handles.Count != 1 || fuzzy.Handles[0] != "A")
                    throw new Exception("Fuzzy legacy Handle deduplication must remain compatible.");

                var legacy = XlsxHandleReader.ReadHandleLookup(decimalPath, 2);
                if (!legacy.UsesLegacyDecimalHandles || legacy.Handles.Count != 1 || legacy.Handles[0] != "A")
                    throw new Exception("Legacy BLT decimal Handle deduplication must remain compatible.");

                var formulaLegacy = XlsxHandleReader.ReadHandleLookup(formulaPath, 2);
                if (!formulaLegacy.UsesLegacyDecimalHandles || formulaLegacy.Handles.Count != 1 || formulaLegacy.Handles[0] != "A")
                    throw new Exception("Legacy formula-cached decimal Handle compatibility must remain unchanged.");
            }
            finally
            {
                Delete(fuzzyPath);
                Delete(decimalPath);
                Delete(formulaPath);
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

        private static void RejectModernFormula(string formulaCellReference, string formula, string cachedValue)
        {
            var path = CreateModernWorkbook("E1", "A", true, formulaCellReference, formula, cachedValue);
            try
            {
                try { XlsxHandleReader.ReadHandleLookup(path, 2); }
                catch (InvalidDataException ex)
                {
                    if (ex.Message.IndexOf("formula", StringComparison.OrdinalIgnoreCase) >= 0) return;
                    throw new Exception("Modern formula-backed identity refusal lost its formula diagnostic.", ex);
                }
                throw new Exception("Modern XLSX accepted formula-backed identity cell: " + formulaCellReference + ".");
            }
            finally { Delete(path); }
        }

        private static string CreateModernWorkbook(
            string elementIds,
            string handles,
            bool ed2Detail,
            string formulaCellReference = "",
            string formula = "",
            string cachedValue = "",
            string extraRow2Cell = "")
        {
            var rows =
                "<row r=\"1\">" +
                ModernCell("A1", "QS3D Element ID", formulaCellReference, formula, cachedValue) +
                ModernCell("B1", "CAD Handle (hex)", formulaCellReference, formula, cachedValue) +
                ModernCell("C1", "QS3D Drawing Fingerprint", formulaCellReference, formula, cachedValue) +
                "</row>" +
                "<row r=\"2\">" +
                ModernCell("A2", elementIds, formulaCellReference, formula, cachedValue) +
                ModernCell("B2", handles, formulaCellReference, formula, cachedValue) +
                ModernCell("C2", "DRAWING-1", formulaCellReference, formula, cachedValue) +
                extraRow2Cell +
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

        private static string ModernCell(
            string reference,
            string value,
            string formulaCellReference,
            string formula,
            string cachedValue) =>
            string.Equals(reference, formulaCellReference, StringComparison.OrdinalIgnoreCase)
                ? FormulaCell(reference, formula, cachedValue)
                : Cell(reference, value);

        private static string FormulaCell(string reference, string formula, string cachedValue) =>
            "<c r=\"" + reference + "\" t=\"str\"><f>" + Escape(formula) + "</f><v>" + Escape(cachedValue) + "</v></c>";

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