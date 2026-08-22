using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleDateCellSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsDateTypedHandleValue();
            DateTypedLegacyTokenDoesNotActivateFallback();
            PreservesDefaultNumericHandleCell();
        }

        private static void RejectsDateTypedHandleValue()
        {
            var path = CreateWorkbook(
                "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
                "<row r=\"2\"><c r=\"A2\" t=\"d\"><v>1A</v></c></row>");
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void DateTypedLegacyTokenDoesNotActivateFallback()
        {
            var path = CreateWorkbook(
                "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>Legacy Data</t></is></c></row>" +
                "<row r=\"2\"><c r=\"A2\" t=\"d\"><v>$123</v></c></row>");
            try
            {
                var result = XlsxHandleReader.ReadHandleLookup(path, 2);
                if (result.Handles.Count != 0)
                    throw new Exception("An XLSX Date cell must not be synthesized into a legacy decimal Handle.");
                if (result.UsesLegacyDecimalHandles)
                    throw new Exception("An XLSX Date cell containing a legacy-looking token must not activate legacy decimal mode.");
            }
            finally { Delete(path); }
        }

        private static void PreservesDefaultNumericHandleCell()
        {
            var path = CreateWorkbook(
                "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
                "<row r=\"2\"><c r=\"A2\"><v>26</v></c></row>");
            try
            {
                var handles = XlsxHandleReader.ReadHandles(path, 2);
                if (handles.Count != 1 || !string.Equals(handles[0], "26", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Default/numeric Handle cells must preserve existing hexadecimal lexical parsing.");
            }
            finally { Delete(path); }
        }

        private static string CreateWorkbook(string rowsXml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-date-cell-" + Guid.NewGuid().ToString("N") + ".xlsx");
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
