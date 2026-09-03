using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationWorkbookCellCoordinateIntegritySmoke
    {
        internal static void Run()
        {
            RejectMismatchedCellRowCoordinate();
            RejectTrailingCellCoordinateGarbage();
            Console.WriteLine("PASS coordination workbook cell coordinate integrity");
        }

        private static void RejectMismatchedCellRowCoordinate()
        {
            WithWorkbook(path =>
            {
                RewriteWorksheet(path, "xl/worksheets/sheet1.xml", xml =>
                    ReplaceOnce(xml, "r=\"B2\"", "r=\"B999\""));
                ExpectInvalidData(() => CoordinationWorkbookTraceReader.Read(path, 2), "mismatched cell row coordinate");
            });
        }

        private static void RejectTrailingCellCoordinateGarbage()
        {
            WithWorkbook(path =>
            {
                RewriteWorksheet(path, "xl/worksheets/sheet1.xml", xml =>
                    ReplaceOnce(xml, "r=\"B2\"", "r=\"B2garbage\""));
                ExpectInvalidData(() => CoordinationWorkbookTraceReader.Read(path, 2), "trailing cell coordinate garbage");
            });
        }

        private static void WithWorkbook(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-coordination-cell-coordinate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "coordination.xlsx");
            try
            {
                CoordinationWorkbookExporter.Export(
                    path,
                    new[]
                    {
                        CoordinationClashExportRow.CreateExactHard(
                            "drawing-fingerprint-01",
                            "10A",
                            "20B",
                            leftElementId: "ELEMENT-A",
                            rightElementId: "ELEMENT-B",
                            leftCategory: "Pipe",
                            rightCategory: "Wall",
                            floor: "L01",
                            comment: "coordinate integrity")
                    });
                action(path);
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
                catch { }
            }
        }

        private static void RewriteWorksheet(string path, string entryName, Func<string, string> transform)
        {
            using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
            var entry = archive.Entries.Single(item => string.Equals(item.FullName, entryName, StringComparison.Ordinal));
            string xml;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8, true, 4096, leaveOpen: false))
                xml = reader.ReadToEnd();
            entry.Delete();
            var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            replacement.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false));
            writer.Write(transform(xml));
        }

        private static string ReplaceOnce(string text, string oldValue, string newValue)
        {
            var first = text.IndexOf(oldValue, StringComparison.Ordinal);
            if (first < 0) throw new InvalidOperationException("Smoke fixture could not find expected worksheet coordinate: " + oldValue);
            if (text.IndexOf(oldValue, first + oldValue.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Smoke fixture coordinate is not unique: " + oldValue);
            return text.Substring(0, first) + newValue + text.Substring(first + oldValue.Length);
        }

        private static void ExpectInvalidData(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new InvalidOperationException("Coordination workbook trace reader accepted " + label + ".");
        }
    }
}
