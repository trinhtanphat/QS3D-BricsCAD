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
            RejectsConflictingAdmittedCountChannelsBeforeIndexer();
            RejectsTransientGenericCountDriftAroundIndexer();
            AcceptsStableMultiInterfaceCountChannels();
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

        private static void RejectsConflictingAdmittedCountChannelsBeforeIndexer()
        {
            var row = Row("E1", "A1");
            var details = new MultiCountRows(row, genericAdmissionCount: 2, driftGenericAfterIndexer: false);
            RefusesDrift(details, new[] { row }, "conflicting admitted channels");
            if (details.IndexerReads != 0)
                throw new Exception("Customer workbook conflicting Count channels must fail before caller row indexer access.");
        }

        private static void RejectsTransientGenericCountDriftAroundIndexer()
        {
            var row = Row("E1", "A1");
            var details = new MultiCountRows(row, genericAdmissionCount: 1, driftGenericAfterIndexer: true);
            RefusesDrift(details, new[] { row }, "transient generic Count drift");
            if (details.IndexerReads != 1)
                throw new Exception("Customer workbook transient Count drift must fail immediately after the first caller row indexer.");
        }

        private static void AcceptsStableMultiInterfaceCountChannels()
        {
            var row = Row("E1", "A1");
            var details = new MultiCountRows(row, genericAdmissionCount: 1, driftGenericAfterIndexer: false);
            var summaries = new MultiCountRows(row, genericAdmissionCount: 1, driftGenericAfterIndexer: false);
            var root = Path.Combine(Path.GetTempPath(), "qs3d-customer-workbook-count-stable-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "customer.xlsx");
            try
            {
                QsCustomerWorkbookExporter.Export(path, details, summaries);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    throw new Exception("Customer workbook stable multi-interface Count control did not publish an XLSX file.");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
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

        private sealed class MultiCountRows : IReadOnlyList<QuantityReportRow>, ICollection<QuantityReportRow>, ICollection
        {
            private readonly QuantityReportRow _row;
            private readonly int _genericAdmissionCount;
            private readonly bool _driftGenericAfterIndexer;
            private bool _genericDriftPending;

            public MultiCountRows(QuantityReportRow row, int genericAdmissionCount, bool driftGenericAfterIndexer)
            {
                _row = row;
                _genericAdmissionCount = genericAdmissionCount;
                _driftGenericAfterIndexer = driftGenericAfterIndexer;
            }

            public int Count => 1;
            int ICollection<QuantityReportRow>.Count
            {
                get
                {
                    if (_genericDriftPending)
                    {
                        _genericDriftPending = false;
                        return 2;
                    }
                    return _genericAdmissionCount;
                }
            }
            int ICollection.Count => 1;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public int IndexerReads { get; private set; }

            public QuantityReportRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    IndexerReads++;
                    if (_driftGenericAfterIndexer) _genericDriftPending = true;
                    return _row;
                }
            }

            public IEnumerator<QuantityReportRow> GetEnumerator()
            {
                yield return _row;
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
            public bool Contains(QuantityReportRow item) => ReferenceEquals(item, _row);
            public void CopyTo(QuantityReportRow[] array, int arrayIndex) => array[arrayIndex] = _row;
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_row, index);
            public void Add(QuantityReportRow item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(QuantityReportRow item) => throw new NotSupportedException();
        }
    }
}