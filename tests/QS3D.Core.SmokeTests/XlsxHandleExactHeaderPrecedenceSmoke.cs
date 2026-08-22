using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleExactHeaderPrecedenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactHandleHeaderWinsOverFuzzyHeader();
            DuplicateExactHandleHeadersRemainAmbiguousForModernSchema();
            FuzzyHandleHeaderRemainsCompatibleWithoutExactHeader();
        }

        private static void ExactHandleHeaderWinsOverFuzzyHeader()
        {
            var path = CreateWorkbook(
                "<row r=\"1\">" +
                Header("A1", "QS3D Element ID") +
                Header("B1", "CAD Handle (hex)") +
                Header("C1", "QS3D Drawing Fingerprint") +
                Header("D1", "Handle Notes") +
                "</row>" +
                "<row r=\"2\">" +
                TextCell("A2", "E1") +
                TextCell("B2", "1A") +
                TextCell("C2", "DRAWING-1") +
                TextCell("D2", "not-a-handle") +
                "</row>");
            try
            {
                var result = XlsxHandleReader.ReadHandleLookup(path, 2);
                if (!result.IsModernSchema) throw new Exception("Exact Handle precedence smoke must remain a modern QS3D worksheet.");
                if (result.Handles.Count != 1 || !string.Equals(result.Handles[0], "1A", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Exact CAD Handle (hex) header must win over unrelated fuzzy Handle headers.");
            }
            finally { Delete(path); }
        }

        private static void DuplicateExactHandleHeadersRemainAmbiguousForModernSchema()
        {
            var path = CreateWorkbook(
                "<row r=\"1\">" +
                Header("A1", "QS3D Element ID") +
                Header("B1", "CAD Handle (hex)") +
                Header("C1", "CAD Handle (hex)") +
                Header("D1", "QS3D Drawing Fingerprint") +
                "</row>" +
                "<row r=\"2\">" +
                TextCell("A2", "E1") +
                TextCell("B2", "1A") +
                TextCell("C2", "2B") +
                TextCell("D2", "DRAWING-1") +
                "</row>");
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandleLookup(path, 2));
            }
            finally { Delete(path); }
        }

        private static void FuzzyHandleHeaderRemainsCompatibleWithoutExactHeader()
        {
            var path = CreateWorkbook(
                "<row r=\"1\">" + Header("A1", "Object Handle") + "</row>" +
                "<row r=\"2\">" + TextCell("A2", "2B") + "</row>");
            try
            {
                var result = XlsxHandleReader.ReadHandleLookup(path, 2);
                if (result.IsModernSchema) throw new Exception("Fuzzy Handle compatibility smoke must remain non-modern.");
                if (result.Handles.Count != 1 || !string.Equals(result.Handles[0], "2B", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Fuzzy Handle header compatibility must remain available when no exact Handle header exists.");
            }
            finally { Delete(path); }
        }

        private static string Header(string reference, string value) => TextCell(reference, value);

        private static string TextCell(string reference, string value) =>
            "<c r=\"" + reference + "\" t=\"inlineStr\"><is><t>" + EscapeXml(value) + "</t></is></c>";

        private static string EscapeXml(string value) =>
            (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static string CreateWorkbook(string rowsXml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-exact-header-" + Guid.NewGuid().ToString("N") + ".xlsx");
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.NoCompression);
                using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" + rowsXml + "</sheetData></worksheet>");
            }
            return path;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
