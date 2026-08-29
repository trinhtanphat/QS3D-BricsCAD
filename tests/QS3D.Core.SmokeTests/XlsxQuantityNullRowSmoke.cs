using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityNullRowSmoke
    {
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-quantity-null-row-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "quantity.xlsx");
                const string sentinel = "preserve-existing-quantity-xlsx";
                File.WriteAllText(destination, sentinel);

                AssertNullRow(destination);
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Quantity XLSX null-row validation replaced an existing destination file.");

                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertNullRow(Path.Combine(untouchedDirectory, "invalid.xlsx"));
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Quantity XLSX null-row validation touched the filesystem before failing.");

                AssertStandardRowCountDriftPreservesDestination(destination, sentinel);
                AssertEd2RowCountDriftPreservesDestination(destination, sentinel);
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertStandardRowCountDriftPreservesDestination(string path, string sentinel)
        {
            var row = ValidRow("E1", "AA1");
            ExpectSnapshotDrift(
                () => XlsxQuantityExporter.Export(path, new CountDriftingRows(row)),
                "Quantity XLSX export row count changed during snapshot");
            if (!string.Equals(File.ReadAllText(path), sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity XLSX row-count drift replaced an existing destination file.");
        }

        private static void AssertEd2RowCountDriftPreservesDestination(string path, string sentinel)
        {
            var detail = ValidRow("E1", "AA1");
            detail.FamilyId = "F1";
            detail.Material = "Concrete";
            var summary = ValidRow("E1", "AA1");
            summary.FamilyId = "F1";
            summary.Material = "Concrete";
            ExpectSnapshotDrift(
                () => XlsxQuantityExporter.ExportEd2(path, new CountDriftingRows(detail), new[] { summary }),
                "ED2 CHI_TIET row count changed during snapshot");
            if (!string.Equals(File.ReadAllText(path), sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity XLSX ED2 row-count drift replaced an existing destination file.");
        }

        private static QuantityReportRow ValidRow(string elementId, string sourceHandle)
        {
            var row = new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "Beam",
                FamilyName = "B1",
                DrawingFingerprint = "drawing",
                Count = 1
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(sourceHandle);
            return row;
        }

        private static void ExpectSnapshotDrift(Action action, string expectedMessage)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Quantity XLSX snapshot drift failed for the wrong reason.", ex);
                return;
            }
            throw new InvalidOperationException("Quantity XLSX accepted a source whose Count changed during snapshot traversal.");
        }

        private sealed class CountDriftingRows : IReadOnlyList<QuantityReportRow>
        {
            private readonly QuantityReportRow _row;
            private int _countReads;

            internal CountDriftingRows(QuantityReportRow row)
            {
                _row = row;
            }

            public int Count
            {
                get
                {
                    _countReads++;
                    return _countReads == 1 ? 1 : 2;
                }
            }

            public QuantityReportRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    return _row;
                }
            }

            public IEnumerator<QuantityReportRow> GetEnumerator()
            {
                yield return _row;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void AssertNullRow(string path)
        {
            try
            {
                XlsxQuantityExporter.Export(path, new QuantityReportRow[] { null! });
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                    throw new InvalidOperationException("Quantity XLSX null-row validation must identify the rows argument.", ex);
                if (ex.Message.IndexOf("row index: 0", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Quantity XLSX null-row validation must identify the zero-based row index.", ex);
                return;
            }

            throw new InvalidOperationException("Quantity XLSX exporter accepted a null report row.");
        }
    }
}
