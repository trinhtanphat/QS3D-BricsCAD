using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleWorkbookMetadataSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsWorkbookWithoutRelationships();
            RejectsRelationshipsWithoutWorkbook();
            PreservesMetadataFreeFallback();
            UsesDeclaredWorksheetWhenMetadataIsComplete();
        }

        private static void RejectsWorkbookWithoutRelationships()
        {
            var path = CreateWorkbook(includeWorkbook: true, includeRelationships: false, includeSecondSheet: false);
            try { Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2)); }
            finally { Delete(path); }
        }

        private static void RejectsRelationshipsWithoutWorkbook()
        {
            var path = CreateWorkbook(includeWorkbook: false, includeRelationships: true, includeSecondSheet: false);
            try { Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2)); }
            finally { Delete(path); }
        }

        private static void PreservesMetadataFreeFallback()
        {
            var path = CreateWorkbook(includeWorkbook: false, includeRelationships: false, includeSecondSheet: false);
            try
            {
                RequireSingleHandle(XlsxHandleReader.ReadHandles(path, 2), "1A", "metadata-free fallback");
            }
            finally { Delete(path); }
        }

        private static void UsesDeclaredWorksheetWhenMetadataIsComplete()
        {
            var path = CreateWorkbook(includeWorkbook: true, includeRelationships: true, includeSecondSheet: true);
            try
            {
                RequireSingleHandle(XlsxHandleReader.ReadHandles(path, 2), "2B", "declared worksheet relationship");
            }
            finally { Delete(path); }
        }

        private static string CreateWorkbook(bool includeWorkbook, bool includeRelationships, bool includeSecondSheet)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-metadata-" + Guid.NewGuid().ToString("N") + ".xlsx");
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Write(archive, "xl/worksheets/sheet1.xml", SheetXml("1A"));
                if (includeSecondSheet) Write(archive, "xl/worksheets/sheet2.xml", SheetXml("2B"));
                if (includeWorkbook)
                {
                    Write(
                        archive,
                        "xl/workbook.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Data\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                }
                if (includeRelationships)
                {
                    Write(
                        archive,
                        "xl/_rels/workbook.xml.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/></Relationships>");
                }
            }
            return path;
        }

        private static string SheetXml(string handle) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
            "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>" + handle + "</t></is></c></row>" +
            "</sheetData></worksheet>";

        private static void RequireSingleHandle(System.Collections.Generic.IReadOnlyList<string> handles, string expected, string label)
        {
            if (handles.Count != 1 || !string.Equals(handles[0], expected, StringComparison.OrdinalIgnoreCase))
                throw new Exception("XLSX Handle reader did not preserve " + label + ".");
        }

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
