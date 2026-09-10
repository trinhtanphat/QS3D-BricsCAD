using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityXlsxBusinessTextFidelitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsMalformedBusinessTextBeforeFilesystemMutation();
            RejectsXmlIllegalBusinessTextBeforeFilesystemMutation();
            PreservesSupplementaryUnicodeBusinessText();
        }

        private static void RejectsMalformedBusinessTextBeforeFilesystemMutation()
        {
            WithAbsentDestination((root, path) =>
            {
                var row = ValidRow();
                row.Category = "Beam-\uD800-bad";
                ExpectInvalidData(() => XlsxQuantityExporter.Export(path, new[] { row }), "Category", "well-formed UTF-16");
                AssertNoFilesystemState(root, "malformed UTF-16 business text");
            });
        }

        private static void RejectsXmlIllegalBusinessTextBeforeFilesystemMutation()
        {
            WithAbsentDestination((root, path) =>
            {
                var row = ValidRow();
                row.FamilyName = "Family-\u0001-bad";
                ExpectInvalidData(() => XlsxQuantityExporter.Export(path, new[] { row }), "FamilyName", "XML 1.0");
                AssertNoFilesystemState(root, "XML-illegal business text");
            });
        }

        private static void PreservesSupplementaryUnicodeBusinessText()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-qxlsx-goodtext-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "quantity.xlsx");
            try
            {
                var row = ValidRow();
                row.Category = "Beam-\U0001F680";
                row.FamilyName = "Family-\U0001F4A1";
                XlsxQuantityExporter.Export(path, new[] { row });
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidOperationException("Quantity XLSX missing sheet1.xml.");
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        if (xml.IndexOf(row.Category, StringComparison.Ordinal) < 0 || xml.IndexOf(row.FamilyName, StringComparison.Ordinal) < 0)
                            throw new InvalidOperationException("QuantityXlsxBusinessTextFidelitySmoke: supplementary Unicode business text changed.");
                        if (xml.IndexOf('\uFFFD') >= 0)
                            throw new InvalidOperationException("QuantityXlsxBusinessTextFidelitySmoke: valid supplementary Unicode was replaced.");
                    }
                }
            }
            finally { TryDeleteDirectory(root); }
        }

        private static QuantityReportRow ValidRow()
        {
            var row = new QuantityReportRow
            {
                Floor = "L1", Zone = "Z1", Category = "Beam", FamilyName = "B200x400",
                DrawingFingerprint = "DRAWING-001", Count = 1
            };
            row.ElementIds.Add("ELEMENT-001");
            row.SourceHandles.Add("1A2B");
            return row;
        }

        private static void ExpectInvalidData(Action action, string field, string contract)
        {
            try { action(); }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf(field, StringComparison.Ordinal) < 0 || ex.Message.IndexOf(contract, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("QuantityXlsxBusinessTextFidelitySmoke: rejection did not identify " + field + " " + contract + " contract.", ex);
                return;
            }
            throw new InvalidOperationException("QuantityXlsxBusinessTextFidelitySmoke: invalid business text was silently accepted.");
        }

        private static void WithAbsentDestination(Action<string, string> action)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-qxlsx-badtext-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "nested", "quantity.xlsx");
            try { action(root, path); }
            finally { TryDeleteDirectory(root); }
        }

        private static void AssertNoFilesystemState(string root, string scenario)
        {
            if (Directory.Exists(root))
                throw new InvalidOperationException("QuantityXlsxBusinessTextFidelitySmoke: " + scenario + " created filesystem state before rejection.");
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
