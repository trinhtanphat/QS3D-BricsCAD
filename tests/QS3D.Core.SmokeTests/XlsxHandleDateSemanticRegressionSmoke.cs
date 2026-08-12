using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleDateSemanticRegressionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DateTypedHandleKeepsDateTagBeforeRejection();
            ErrorTypedHandleRemainsUnsupported();
        }

        private static void DateTypedHandleKeepsDateTagBeforeRejection()
        {
            var path = CreateWorkbook(
                "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
                "<row r=\"2\"><c r=\"A2\" t=\"d\"><v>1A</v></c></row>");
            try
            {
                ThrowsContaining<InvalidDataException>(
                    () => XlsxHandleReader.ReadHandles(path, 2),
                    "invalid CAD Handle token: [Date]");
            }
            finally
            {
                Delete(path);
            }
        }

        private static void ErrorTypedHandleRemainsUnsupported()
        {
            var path = CreateWorkbook(
                "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c></row>" +
                "<row r=\"2\"><c r=\"A2\" t=\"e\"><v>#VALUE!</v></c></row>");
            try
            {
                ThrowsContaining<InvalidDataException>(
                    () => XlsxHandleReader.ReadHandles(path, 2),
                    "unsupported XLSX value type");
            }
            finally
            {
                Delete(path);
            }
        }

        private static string CreateWorkbook(string rowsXml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-handle-date-regression-" + Guid.NewGuid().ToString("N") + ".xlsx");
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.NoCompression);
                using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" + rowsXml + "</sheetData></worksheet>");
            }
            return path;
        }

        private static void ThrowsContaining<T>(Action action, string expectedMessage) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Expected " + typeof(T).Name + " containing '" + expectedMessage + "' but got: " + ex.Message, ex);
            }
            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private static void Delete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
