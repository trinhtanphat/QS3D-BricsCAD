using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleSharedStringIndexSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsValidSharedStringIndex();
            RejectsInvalidSharedStringIndex("1", "out-of-range");
            RejectsInvalidSharedStringIndex("abc", "non-numeric");
            RejectsInvalidSharedStringIndex("-1", "negative");
            RejectsMissingSharedStringIndex();
        }

        private static void AcceptsValidSharedStringIndex()
        {
            var path = CreateWorkbook("0", true);
            try
            {
                var handles = XlsxHandleReader.ReadHandles(path, 2);
                if (handles.Count != 1 || !string.Equals(handles[0], "1A", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("A valid XLSX shared-string handle index must resolve to the referenced handle text.");
            }
            finally { Delete(path); }
        }

        private static void RejectsInvalidSharedStringIndex(string indexValue, string label)
        {
            var path = CreateWorkbook(indexValue, true);
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new Exception("XLSX Handle reader must reject a " + label + " shared-string index.", ex);
            }
            finally { Delete(path); }
        }

        private static void RejectsMissingSharedStringIndex()
        {
            var path = CreateWorkbook(string.Empty, false);
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static string CreateWorkbook(string indexValue, bool includeValueElement)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-shared-index-" + Guid.NewGuid().ToString("N") + ".xlsx");
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Write(
                    archive,
                    "xl/sharedStrings.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><si><t>1A</t></si></sst>");

                var valueXml = includeValueElement ? "<v>" + EscapeXml(indexValue) + "</v>" : string.Empty;
                Write(
                    archive,
                    "xl/worksheets/sheet1.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                    "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
                    "<row r=\"2\"><c r=\"A2\" t=\"s\">" + valueXml + "</c></row>" +
                    "</sheetData></worksheet>");
            }
            return path;
        }

        private static string EscapeXml(string value) =>
            (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
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
