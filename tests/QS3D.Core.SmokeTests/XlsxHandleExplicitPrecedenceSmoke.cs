using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleExplicitPrecedenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExplicitHandleColumnWinsOverUnrelatedLegacyToken();
            LegacyDecimalFallbackRemainsWithoutHandleHeader();
        }

        private static void ExplicitHandleColumnWinsOverUnrelatedLegacyToken()
        {
            var path = CreateWorkbook(
                "<row r=\"1\">" +
                "<c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c>" +
                "<c r=\"B1\" t=\"inlineStr\"><is><t>Note</t></is></c>" +
                "</row>" +
                "<row r=\"2\">" +
                "<c r=\"A2\" t=\"inlineStr\"><is><t>1A</t></is></c>" +
                "<c r=\"B2\" t=\"inlineStr\"><is><t>$123</t></is></c>" +
                "</row>");
            try
            {
                var result = XlsxHandleReader.ReadHandleLookup(path, 2);
                if (result.Handles.Count != 1 || !string.Equals(result.Handles[0], "1A", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("An explicit CAD Handle column must take precedence over unrelated legacy-looking cells.");
                if (result.UsesLegacyDecimalHandles)
                    throw new Exception("A row resolved through an explicit CAD Handle column must not be marked as legacy-decimal.");
            }
            finally { Delete(path); }
        }

        private static void LegacyDecimalFallbackRemainsWithoutHandleHeader()
        {
            var path = CreateWorkbook(
                "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>Legacy data</t></is></c></row>" +
                "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>$123</t></is></c></row>");
            try
            {
                var result = XlsxHandleReader.ReadHandleLookup(path, 2);
                if (result.Handles.Count != 1 || !string.Equals(result.Handles[0], "7B", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Legacy decimal Handle fallback must remain available when no explicit Handle header exists.");
                if (!result.UsesLegacyDecimalHandles)
                    throw new Exception("Legacy decimal fallback results must retain UsesLegacyDecimalHandles=true.");
            }
            finally { Delete(path); }
        }

        private static string CreateWorkbook(string rowsXml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-explicit-precedence-" + Guid.NewGuid().ToString("N") + ".xlsx");
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.NoCompression);
                using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" + rowsXml + "</sheetData></worksheet>");
            }
            return path;
        }

        private static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
