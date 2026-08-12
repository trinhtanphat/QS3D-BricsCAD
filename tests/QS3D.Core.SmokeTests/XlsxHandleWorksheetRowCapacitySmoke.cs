using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleWorksheetRowCapacitySmoke
    {
        private const int MaxRows = 1048576;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsRequestedRowAboveCapacityBeforeFileLookup();
            RejectsWorksheetRowAboveCapacity();
            RejectsCellReferenceAboveCapacity();
            AcceptsUnaddressedRowWithoutIndex();
            AcceptsLastValidWorksheetRow();
        }

        private static void RejectsRequestedRowAboveCapacityBeforeFileLookup()
        {
            var missingPath = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-row-capacity-missing-" + Guid.NewGuid().ToString("N") + ".xlsx");
            Throws<ArgumentOutOfRangeException>(() => XlsxHandleReader.ReadHandles(missingPath, MaxRows + 1));
        }

        private static void RejectsWorksheetRowAboveCapacity()
        {
            var path = CreateWorkbook(
                HeaderRow() +
                "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>1A</t></is></c></row>" +
                "<row r=\"1048577\"><c r=\"A1048577\" t=\"inlineStr\"><is><t>2B</t></is></c></row>");
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void RejectsCellReferenceAboveCapacity()
        {
            var path = CreateWorkbook(
                HeaderRow() +
                "<row r=\"2\"><c r=\"A1048577\" t=\"inlineStr\"><is><t>1A</t></is></c></row>");
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void AcceptsUnaddressedRowWithoutIndex()
        {
            var path = CreateWorkbook(
                HeaderRow() +
                "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>1A</t></is></c></row>" +
                "<row><c t=\"inlineStr\"><is><t>Ignored optional-index row</t></is></c></row>");
            try
            {
                var handles = XlsxHandleReader.ReadHandles(path, 2);
                if (handles.Count != 1 || !string.Equals(handles[0], "1A", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("An unrelated XLSX row without optional r metadata must not invalidate an addressed Handle row.");
            }
            finally { Delete(path); }
        }

        private static void AcceptsLastValidWorksheetRow()
        {
            var path = CreateWorkbook(
                HeaderRow() +
                "<row r=\"1048576\"><c r=\"A1048576\" t=\"inlineStr\"><is><t>1A</t></is></c></row>");
            try
            {
                var handles = XlsxHandleReader.ReadHandles(path, MaxRows);
                if (handles.Count != 1 || !string.Equals(handles[0], "1A", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("The XLSX Handle reader must accept the final valid worksheet row.");
            }
            finally { Delete(path); }
        }

        private static string HeaderRow() =>
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>";

        private static string CreateWorkbook(string rowsXml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-row-capacity-" + Guid.NewGuid().ToString("N") + ".xlsx");
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
