using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityXmlSanitizationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SanitizesStandardWorksheetText();
            SanitizesEd2WorksheetText();
        }

        private static void SanitizesStandardWorksheetText()
        {
            var root = Root("standard");
            var path = Path.Combine(root, "quantity.xlsx");
            try
            {
                XlsxQuantityExporter.Export(path, new[]
                {
                    new QuantityReportRow { FamilyName = "A\u0001B\uD800C<&" }
                });

                using (var archive = ZipFile.OpenRead(path))
                {
                    var xml = ReadEntry(archive, "xl/worksheets/sheet1.xml");
                    RequireSanitized(xml, "A\uFFFDB\uFFFDC&lt;&amp;", "standard Quantity XLSX");
                }
            }
            finally { Delete(root); }
        }

        private static void SanitizesEd2WorksheetText()
        {
            var root = Root("ed2");
            var path = Path.Combine(root, "quantity-ed2.xlsx");
            try
            {
                var detail = ValidEd2Row("E1");
                detail.Note = "D\u0002E\uD800F<&";
                var summary = ValidEd2Row("E1");
                summary.Note = "S\u0003T\uD800U<&";

                XlsxQuantityExporter.ExportEd2(path, new[] { detail }, new[] { summary });

                using (var archive = ZipFile.OpenRead(path))
                {
                    RequireSanitized(
                        ReadEntry(archive, "xl/worksheets/sheet1.xml"),
                        "D\uFFFDE\uFFFDF&lt;&amp;",
                        "ED2 CHI_TIET");
                    RequireSanitized(
                        ReadEntry(archive, "xl/worksheets/sheet2.xml"),
                        "S\uFFFDT\uFFFDU&lt;&amp;",
                        "ED2 TONG_HOP");
                }
            }
            finally { Delete(root); }
        }

        private static QuantityReportRow ValidEd2Row(string elementId)
        {
            var row = new QuantityReportRow
            {
                Floor = "F1",
                Zone = "Z1",
                Category = "Wall",
                FamilyId = "FAM-1",
                FamilyName = "Wall 200",
                Material = "Concrete",
                DrawingFingerprint = "DRAWING-1",
                Count = 1
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add("1");
            return row;
        }

        private static string ReadEntry(ZipArchive archive, string name)
        {
            var entry = archive.GetEntry(name) ?? throw new Exception("Quantity XLSX worksheet entry is missing: " + name + ".");
            using (var reader = new StreamReader(entry.Open())) return reader.ReadToEnd();
        }

        private static void RequireSanitized(string xml, string expected, string label)
        {
            if (xml.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception(label + " must replace XML-invalid text while preserving ordinary characters and escaping markup.");
            if (xml.IndexOf('\u0001') >= 0 || xml.IndexOf('\u0002') >= 0 || xml.IndexOf('\u0003') >= 0 || xml.IndexOf('\uD800') >= 0)
                throw new Exception(label + " must not retain XML-invalid controls or unpaired surrogate characters.");
        }

        private static string Root(string suffix) =>
            Path.Combine(Path.GetTempPath(), "qs3d-quantity-xlsx-xml-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void Delete(string root)
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
