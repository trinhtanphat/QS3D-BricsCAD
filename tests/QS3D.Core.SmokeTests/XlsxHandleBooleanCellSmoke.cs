using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleBooleanCellSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsBooleanHandleCell("1");
            RejectsBooleanHandleCell("0");
            RejectsMalformedBooleanCell();
            PreservesDefaultNumericHandleCell();
        }

        private static void RejectsBooleanHandleCell(string booleanValue)
        {
            var path = CreateWorkbook(" t=\"b\"", booleanValue);
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void RejectsMalformedBooleanCell()
        {
            var path = CreateWorkbook(" t=\"b\"", "2");
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void PreservesDefaultNumericHandleCell()
        {
            var path = CreateWorkbook(string.Empty, "26");
            try
            {
                var handles = XlsxHandleReader.ReadHandles(path, 2);
                if (handles.Count != 1 || !string.Equals(handles[0], "26", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Default/numeric XLSX Handle cells must preserve existing hexadecimal lexical parsing.");
            }
            finally { Delete(path); }
        }

        private static string CreateWorkbook(string typeAttribute, string value)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-boolean-" + Guid.NewGuid().ToString("N") + ".xlsx");
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.NoCompression);
                using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                    writer.Write(
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                        "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
                        "<row r=\"2\"><c r=\"A2\"" + typeAttribute + "><v>" + value + "</v></c></row>" +
                        "</sheetData></worksheet>");
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
