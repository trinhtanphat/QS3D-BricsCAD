using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleDuplicatePartSmoke
    {
        private const string WorksheetType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsDuplicateWorkbookPart();
            RejectsDuplicateSharedStringsPart();
            RejectsDuplicateDeclaredWorksheetPart();
            RejectsDuplicateFallbackSheet1Part();
            PreservesDistinctFallbackWorksheetParts();
        }

        private static void RejectsDuplicateWorkbookPart()
        {
            var path = NewPath("workbook");
            try
            {
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    Write(archive, "xl/workbook.xml", WorkbookXml("rId1"));
                    Write(archive, "xl/workbook.xml", WorkbookXml("rId1"));
                    Write(archive, "xl/_rels/workbook.xml.rels", RelationshipsXml("worksheets/sheet1.xml"));
                    Write(archive, "xl/worksheets/sheet1.xml", SheetXml("1A"));
                }
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void RejectsDuplicateSharedStringsPart()
        {
            var path = NewPath("shared-strings");
            try
            {
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    Write(archive, "xl/sharedStrings.xml", SharedStringsXml("1A"));
                    Write(archive, "xl/sharedStrings.xml", SharedStringsXml("2B"));
                    Write(archive, "xl/worksheets/sheet1.xml", SharedStringSheetXml());
                }
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void RejectsDuplicateDeclaredWorksheetPart()
        {
            var path = NewPath("declared-sheet");
            try
            {
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    Write(archive, "xl/workbook.xml", WorkbookXml("rId1"));
                    Write(archive, "xl/_rels/workbook.xml.rels", RelationshipsXml("worksheets/sheet1.xml"));
                    Write(archive, "xl/worksheets/sheet1.xml", SheetXml("1A"));
                    Write(archive, "xl/worksheets/sheet1.xml", SheetXml("2B"));
                }
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void RejectsDuplicateFallbackSheet1Part()
        {
            var path = NewPath("fallback-sheet1");
            try
            {
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    Write(archive, "xl/worksheets/sheet1.xml", SheetXml("1A"));
                    Write(archive, "xl/worksheets/sheet1.xml", SheetXml("2B"));
                }
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static void PreservesDistinctFallbackWorksheetParts()
        {
            var path = NewPath("distinct-fallback");
            try
            {
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    Write(archive, "xl/worksheets/sheet2.xml", SheetXml("1A"));
                    Write(archive, "xl/worksheets/sheet3.xml", SheetXml("2B"));
                    Write(archive, "notes/readme.txt", "unrelated");
                    Write(archive, "notes/readme.txt", "duplicate unrelated entry remains outside reader scope");
                }
                var handles = XlsxHandleReader.ReadHandles(path, 2);
                if (handles.Count != 1 || !string.Equals(handles[0], "1A", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Distinct metadata-free worksheet parts must preserve the existing first-sheet fallback.");
            }
            finally { Delete(path); }
        }

        private static string WorkbookXml(string relationshipId) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Data\" sheetId=\"1\" r:id=\"" + relationshipId + "\"/></sheets></workbook>";

        private static string RelationshipsXml(string target) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"" + WorksheetType + "\" Target=\"" + target + "\"/></Relationships>";

        private static string SharedStringsXml(string handle) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><si><t>" + handle + "</t></si></sst>";

        private static string SharedStringSheetXml() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
            "<row r=\"2\"><c r=\"A2\" t=\"s\"><v>0</v></c></row>" +
            "</sheetData></worksheet>";

        private static string SheetXml(string handle) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
            "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>" + handle + "</t></is></c></row>" +
            "</sheetData></worksheet>";

        private static string NewPath(string suffix) =>
            Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-duplicate-part-" + suffix + "-" + Guid.NewGuid().ToString("N") + ".xlsx");

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
