using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxWhitespaceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-door-xlsx-whitespace-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "whitespace.xlsx");
            try
            {
                var row = new DoorOpeningScheduleRow
                {
                    Floor = " L1 ",
                    Category = "Door",
                    FamilyName = "Door family",
                    Material = "Glass",
                    WidthM = 0.9d,
                    HeightM = 2.2d
                };
                row.ElementIds.Add(" E1 ");
                row.HostIds.Add("H1");

                DoorOpeningXlsxExporter.Export(path, new[] { row });
                var sheetXml = ReadSheetXml(path);

                Contains(
                    "<c r=\"A2\" t=\"inlineStr\" s=\"0\"><is><t xml:space=\"preserve\"> L1 </t></is></c>",
                    sheetXml,
                    "boundary whitespace in direct text");
                Contains(
                    "<c r=\"L2\" t=\"inlineStr\" s=\"0\"><is><t xml:space=\"preserve\"> E1 </t></is></c>",
                    sheetXml,
                    "boundary whitespace in joined ID text");
                Contains(
                    "<c r=\"B2\" t=\"inlineStr\" s=\"0\"><is><t>Door</t></is></c>",
                    sheetXml,
                    "ordinary text cell");
                DoesNotContain(
                    "<t xml:space=\"preserve\">Door</t>",
                    sheetXml,
                    "ordinary text should not request whitespace preservation");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static string ReadSheetXml(string path)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (entry == null) throw new InvalidOperationException("Door/opening XLSX worksheet entry is missing.");
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8, true))
                    return reader.ReadToEnd();
            }
        }

        private static void Contains(string expected, string actual, string scenario)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Door/opening XLSX did not preserve " + scenario + ".");
        }

        private static void DoesNotContain(string unexpected, string actual, string scenario)
        {
            if (actual != null && actual.IndexOf(unexpected, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Door/opening XLSX emitted an unexpected preservation marker for " + scenario + ".");
        }

        private static void DeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
