using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityXlsxProvenanceUtf16Smoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsMalformedHighSurrogateBeforeReplace();
            RejectsMalformedLowSurrogateBeforeReplace();
            PreservesValidSupplementaryUnicode();
        }

        private static void RejectsMalformedHighSurrogateBeforeReplace()
        {
            WithDestination((directory, path, original) =>
            {
                var row = ValidRow();
                row.ElementIds.Add("ELEMENT-\uD800-BAD");

                ExpectInvalidData(
                    () => XlsxQuantityExporter.Export(path, new[] { row }),
                    "ElementIds",
                    "Quantity XLSX accepted an unpaired high surrogate in ElementIds");

                AssertDestinationAndNoTempResidue(directory, path, original, "unpaired high surrogate");
            });
        }

        private static void RejectsMalformedLowSurrogateBeforeReplace()
        {
            WithDestination((directory, path, original) =>
            {
                var row = ValidRow();
                row.ElementIds.Add("ELEMENT-001");
                row.DrawingFingerprint = "DRAWING-\uDC00-BAD";

                ExpectInvalidData(
                    () => XlsxQuantityExporter.Export(path, new[] { row }),
                    "DrawingFingerprint",
                    "Quantity XLSX accepted an unpaired low surrogate in DrawingFingerprint");

                AssertDestinationAndNoTempResidue(directory, path, original, "unpaired low surrogate");
            });
        }

        private static void PreservesValidSupplementaryUnicode()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-quantity-xlsx-unicode-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "quantity.xlsx");
            try
            {
                var row = ValidRow();
                row.ElementIds.Add("ELEMENT-\U0001F680-001");
                row.DrawingFingerprint = "DRAWING-\U0001F4A1-001";

                XlsxQuantityExporter.Export(path, new[] { row });

                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (entry == null)
                        throw new InvalidOperationException("QuantityXlsxProvenanceUtf16Smoke: generated workbook is missing xl/worksheets/sheet1.xml.");

                    using (var reader = new StreamReader(entry.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        Contains(xml, row.ElementIds[0], "ElementId supplementary Unicode changed");
                        Contains(xml, row.DrawingFingerprint, "DrawingFingerprint supplementary Unicode changed");
                        if (xml.IndexOf('\uFFFD') >= 0)
                            throw new InvalidOperationException("QuantityXlsxProvenanceUtf16Smoke: valid supplementary Unicode was replaced with U+FFFD.");
                    }
                }
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static QuantityReportRow ValidRow()
        {
            var row = new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "Beam",
                FamilyName = "B200x400",
                DrawingFingerprint = "DRAWING-001",
                Count = 1
            };
            row.SourceHandles.Add("1A2B");
            return row;
        }

        private static void ExpectInvalidData(Action action, string expectedField, string message)
        {
            try
            {
                action();
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf(expectedField, StringComparison.Ordinal) < 0 ||
                    ex.Message.IndexOf("well-formed UTF-16", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "QuantityXlsxProvenanceUtf16Smoke: rejection did not identify malformed " + expectedField + " provenance.", ex);
                return;
            }

            throw new InvalidOperationException("QuantityXlsxProvenanceUtf16Smoke: " + message + ".");
        }

        private static void WithDestination(Action<string, string, byte[]> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-quantity-xlsx-provenance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "quantity.xlsx");
            var original = new byte[] { 0x51, 0x53, 0x33, 0x44, 0x00, 0xFF, 0x2A };
            File.WriteAllBytes(path, original);
            try
            {
                action(directory, path, original);
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static void AssertDestinationAndNoTempResidue(string directory, string path, byte[] original, string scenario)
        {
            var actual = File.ReadAllBytes(path);
            if (!BytesEqual(original, actual))
                throw new InvalidOperationException(
                    "QuantityXlsxProvenanceUtf16Smoke: destination changed before rejecting " + scenario + ".");

            var files = Directory.GetFiles(directory);
            if (files.Length != 1 || !string.Equals(Path.GetFullPath(files[0]), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "QuantityXlsxProvenanceUtf16Smoke: temporary output residue remained after rejecting " + scenario + ".");
        }

        private static bool BytesEqual(byte[] expected, byte[] actual)
        {
            if (expected.Length != actual.Length) return false;
            for (var index = 0; index < expected.Length; index++)
                if (expected[index] != actual[index]) return false;
            return true;
        }

        private static void Contains(string actual, string expected, string message)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("QuantityXlsxProvenanceUtf16Smoke: " + message + ".");
        }

        private static void TryDeleteDirectory(string directory)
        {
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
