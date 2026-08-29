using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCustomerWorkbookSnapshotCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsShrinkAfterAdmissionBeforeDestinationReplacement();
            RejectsGrowthAfterAdmissionBeforeDestinationReplacement();
        }

        private static void RejectsShrinkAfterAdmissionBeforeDestinationReplacement()
        {
            var items = new[] { Row("E1", "A1"), Row("E2", "A2") };
            RefusesDrift(
                new DriftingRows(items, 2, 1),
                new DriftingRows(items, 2, 1),
                "shrink");
        }

        private static void RejectsGrowthAfterAdmissionBeforeDestinationReplacement()
        {
            var items = new[] { Row("E1", "A1"), Row("E2", "A2") };
            RefusesDrift(
                new DriftingRows(items, 1, 2),
                new DriftingRows(items, 1, 2),
                "growth");
        }

        private static void RefusesDrift(
            IReadOnlyList<QuantityReportRow> details,
            IReadOnlyList<QuantityReportRow> summaries,
            string label)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-customer-workbook-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "customer.xlsx");
            File.WriteAllText(path, "existing-destination");
            try
            {
                ExpectThrows<InvalidDataException>(() => QsCustomerWorkbookExporter.Export(path, details, summaries));
                Equal("existing-destination", File.ReadAllText(path));
                var files = Directory.GetFiles(root);
                if (files.Length != 1 || !string.Equals(files[0], path, StringComparison.Ordinal))
                    throw new Exception("Customer workbook Count " + label + " refusal left temp/output residue.");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static QuantityReportRow Row(string id, string handle)
        {
            var row = new QuantityReportRow
            {
                Floor = "Floor 1",
                Category = "Column",
                FamilyId = "COL-1",
                FamilyName = "Column",
                ElementName = "Column " + id,
                Material = "Concrete",
                DrawingFingerprint = "DWG-COUNT-STABILITY",
                Count = 1
            };
            row.ElementIds.Add(id);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private sealed class DriftingRows : IReadOnlyList<QuantityReportRow>
        {
            private readonly QuantityReportRow[] _items;
            private readonly int _beforeReadCount;
            private readonly int _afterReadCount;
            private bool _readOccurred;

            public DriftingRows(QuantityReportRow[] items, int beforeReadCount, int afterReadCount)
            {
                _items = items;
                _beforeReadCount = beforeReadCount;
                _afterReadCount = afterReadCount;
            }

            public int Count => _readOccurred ? _afterReadCount : _beforeReadCount;

            public QuantityReportRow this[int index]
            {
                get
                {
                    var item = _items[index];
                    _readOccurred = true;
                    return item;
                }
            }

            public IEnumerator<QuantityReportRow> GetEnumerator()
            {
                for (var index = 0; index < _items.Length; index++) yield return _items[index];
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
    }
}
