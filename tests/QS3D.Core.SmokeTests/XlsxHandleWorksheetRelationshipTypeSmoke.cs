using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleWorksheetRelationshipTypeSmoke
    {
        private const string WorksheetTypeHttp = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
        private const string WorksheetTypeHttps = "https://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
        private const string StylesType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";

        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsWorksheetRelationshipType(WorksheetTypeHttp, "http worksheet relationship type");
            AcceptsWorksheetRelationshipType(WorksheetTypeHttps, "https worksheet relationship type");
            RejectsNonWorksheetRelationshipType();
        }

        private static void AcceptsWorksheetRelationshipType(string relationshipType, string label)
        {
            var path = CreateWorkbook(relationshipType);
            try
            {
                var handles = XlsxHandleReader.ReadHandles(path, 2);
                if (handles.Count != 1 || !string.Equals(handles[0], "1A", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("XLSX Handle reader must accept the " + label + ".");
            }
            finally { Delete(path); }
        }

        private static void RejectsNonWorksheetRelationshipType()
        {
            var path = CreateWorkbook(StylesType);
            try
            {
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandles(path, 2));
            }
            finally { Delete(path); }
        }

        private static string CreateWorkbook(string relationshipType)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-rel-type-" + Guid.NewGuid().ToString("N") + ".xlsx");
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Write(
                    archive,
                    "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Data\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                Write(
                    archive,
                    "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"" + relationshipType + "\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                Write(
                    archive,
                    "xl/worksheets/sheet1.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                    "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
                    "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>1A</t></is></c></row>" +
                    "</sheetData></worksheet>");
            }
            return path;
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
