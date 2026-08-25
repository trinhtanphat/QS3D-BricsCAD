using System;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityProvenanceIntegritySmoke
    {
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-quantity-provenance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                AssertValidGroupedAndSingleRows(root);
                AssertInvalidRowsPreserveDestination(root);
                AssertDuplicateAcrossRowsFailsClosed(root);
                AssertValidationPrecedesFilesystemMutation(root);
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertValidGroupedAndSingleRows(string root)
        {
            var grouped = ValidRow(2, "drawing-A", new[] { "element-A", "element-B" }, new[] { "1A", "0x2B" });
            var single = ValidRow(1, "drawing-A", new[] { "element-C" }, new[] { "3C" });
            var path = Path.Combine(root, "valid.xlsx");
            XlsxQuantityExporter.Export(path, new[] { grouped, single });
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidOperationException("Quantity XLSX valid provenance did not produce a workbook.");
        }

        private static void AssertInvalidRowsPreserveDestination(string root)
        {
            AssertRejectedPreservesDestination(root, "missing-fingerprint", row => row.DrawingFingerprint = "   ", "DrawingFingerprint");
            AssertRejectedPreservesDestination(root, "missing-ids", row => row.ElementIds.Clear(), "ElementIds");
            AssertRejectedPreservesDestination(root, "blank-id", row => row.ElementIds[0] = " ", "ElementIds");
            AssertRejectedPreservesDestination(root, "duplicate-id", row => row.ElementIds[1] = row.ElementIds[0], "duplicate semantic Element ID");
            AssertRejectedPreservesDestination(root, "missing-handle", row => row.SourceHandles.Clear(), "SourceHandles");
            AssertRejectedPreservesDestination(root, "blank-handle", row => row.SourceHandles[0] = " ", "SourceHandles");
            AssertRejectedPreservesDestination(root, "malformed-handle", row => row.SourceHandles[0] = "XYZ", "invalid positive hexadecimal CAD Handle");
            AssertRejectedPreservesDestination(root, "zero-handle", row => row.SourceHandles[0] = "0x0", "invalid positive hexadecimal CAD Handle");
            AssertRejectedPreservesDestination(root, "count-mismatch", row => row.Count = 1, "must match semantic Element ID cardinality");
        }

        private static void AssertDuplicateAcrossRowsFailsClosed(string root)
        {
            var path = Path.Combine(root, "duplicate-across-rows.xlsx");
            const string sentinel = "preserve-existing-quantity-xlsx";
            File.WriteAllText(path, sentinel);
            var first = ValidRow(1, "drawing-A", new[] { "shared-element" }, new[] { "10" });
            var second = ValidRow(1, "drawing-A", new[] { "SHARED-ELEMENT" }, new[] { "11" });

            AssertInvalidData(
                () => XlsxQuantityExporter.Export(path, new[] { first, second }),
                "duplicate semantic Element ID");
            AssertPreserved(path, sentinel, root);
        }

        private static void AssertValidationPrecedesFilesystemMutation(string root)
        {
            var untouchedDirectory = Path.Combine(root, "must-not-be-created");
            var invalid = ValidRow(1, " ", new[] { "element-A" }, new[] { "1A" });
            AssertInvalidData(
                () => XlsxQuantityExporter.Export(Path.Combine(untouchedDirectory, "invalid.xlsx"), new[] { invalid }),
                "DrawingFingerprint");
            if (Directory.Exists(untouchedDirectory))
                throw new InvalidOperationException("Quantity XLSX provenance validation touched the filesystem before failing.");
        }

        private static void AssertRejectedPreservesDestination(
            string root,
            string name,
            Action<QuantityReportRow> mutate,
            string expectedMessage)
        {
            var path = Path.Combine(root, name + ".xlsx");
            const string sentinel = "preserve-existing-quantity-xlsx";
            File.WriteAllText(path, sentinel);
            var row = ValidRow(2, "drawing-A", new[] { "element-A", "element-B" }, new[] { "1A", "2B" });
            mutate(row);
            AssertInvalidData(() => XlsxQuantityExporter.Export(path, new[] { row }), expectedMessage);
            AssertPreserved(path, sentinel, root);
        }

        private static void AssertPreserved(string path, string sentinel, string root)
        {
            if (!string.Equals(File.ReadAllText(path), sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity XLSX invalid provenance replaced an existing destination file.");
            foreach (var file in Directory.GetFiles(root))
            {
                if (string.Equals(file, path, StringComparison.OrdinalIgnoreCase)) continue;
                if (Path.GetFileName(file).StartsWith(Path.GetFileName(path) + ".", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Quantity XLSX invalid provenance left a temporary workbook behind.");
            }
        }

        private static QuantityReportRow ValidRow(int count, string fingerprint, string[] ids, string[] handles)
        {
            var row = new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "ArchitecturalWall",
                FamilyName = "Wall 200",
                DrawingFingerprint = fingerprint,
                Count = count
            };
            foreach (var id in ids) row.ElementIds.Add(id);
            foreach (var handle in handles) row.SourceHandles.Add(handle);
            return row;
        }

        private static void AssertInvalidData(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Quantity XLSX provenance validation returned an unexpected error: " + ex.Message, ex);
                return;
            }

            throw new InvalidOperationException("Quantity XLSX exporter accepted invalid provenance: " + expectedMessage + ".");
        }
    }
}
