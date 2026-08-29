using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageXlsxSourceCountContractSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsConflictingDeterministicCountsBeforeFilesystemMutation();
            RejectsNegativeKnownCountBeforeFilesystemMutation();
            RejectsOversizedKnownCountBeforeFilesystemMutation();
            RejectsPostSnapshotCountDriftBeforeReplacingDestination();
            RejectsPostSnapshotPrimaryQuantityDriftBeforeReplacingDestination();
            AcceptsHonestMultiInterfaceSource();
        }

        private static void RejectsConflictingDeterministicCountsBeforeFilesystemMutation()
        {
            var directory = NewDirectory();
            var path = Path.Combine(directory, "material-usage.xlsx");
            var rows = new CountContractRows(1, 2, 1, false);
            try
            {
                Throws<ArgumentException>(() => MaterialUsageXlsxExporter.Export(path, rows));
                if (Directory.Exists(directory)) throw new Exception("Conflicting source counts must fail before destination directory creation.");
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RejectsNegativeKnownCountBeforeFilesystemMutation()
        {
            var directory = NewDirectory();
            var path = Path.Combine(directory, "material-usage.xlsx");
            var rows = new CountContractRows(-1, -1, -1, false);
            try
            {
                Throws<ArgumentOutOfRangeException>(() => MaterialUsageXlsxExporter.Export(path, rows));
                if (Directory.Exists(directory)) throw new Exception("Negative source Count must fail before destination directory creation.");
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RejectsOversizedKnownCountBeforeFilesystemMutation()
        {
            var directory = NewDirectory();
            var path = Path.Combine(directory, "material-usage.xlsx");
            var rows = new CountContractRows(1048576, 1048576, 1048576, false);
            try
            {
                Throws<ArgumentOutOfRangeException>(() => MaterialUsageXlsxExporter.Export(path, rows));
                if (Directory.Exists(directory)) throw new Exception("Oversized source Count must fail before destination directory creation.");
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RejectsPostSnapshotCountDriftBeforeReplacingDestination()
        {
            var directory = NewDirectory();
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "material-usage.xlsx");
            File.WriteAllText(path, "existing-destination");
            var rows = new CountContractRows(1, 1, 1, true);
            try
            {
                Throws<InvalidOperationException>(() => MaterialUsageXlsxExporter.Export(path, rows));
                Equal("existing-destination", File.ReadAllText(path));
                var files = Directory.GetFiles(directory);
                if (files.Length != 1 || !string.Equals(files[0], path, StringComparison.Ordinal))
                    throw new Exception("Count-drift refusal left unexpected temp/output residue.");
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RejectsPostSnapshotPrimaryQuantityDriftBeforeReplacingDestination()
        {
            var directory = NewDirectory();
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "material-usage.xlsx");
            File.WriteAllText(path, "existing-destination");
            var rows = new CountContractRows(1, 1, 1, false, true);
            try
            {
                Throws<InvalidOperationException>(() => MaterialUsageXlsxExporter.Export(path, rows));
                Equal("existing-destination", File.ReadAllText(path));
                var files = Directory.GetFiles(directory);
                if (files.Length != 1 || !string.Equals(files[0], path, StringComparison.Ordinal))
                    throw new Exception("PrimaryQuantity-drift refusal left unexpected temp/output residue.");
            }
            finally { DeleteDirectory(directory); }
        }

        private static void AcceptsHonestMultiInterfaceSource()
        {
            var directory = NewDirectory();
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "material-usage.xlsx");
            var rows = new CountContractRows(1, 1, 1, false);
            try
            {
                MaterialUsageXlsxExporter.Export(path, rows);
                if (!File.Exists(path)) throw new Exception("Honest multi-interface source did not produce a workbook.");
            }
            finally { DeleteDirectory(directory); }
        }

        private static MaterialUsageRow ValidRow()
        {
            var row = new MaterialUsageRow
            {
                Floor = "Floor 1",
                MaterialName = "Concrete",
                UnitHint = "m3",
                Component = "Material",
                Category = "Slab",
                FamilyName = "Slab 200",
                ElementCount = 1,
                PrimaryQuantity = 2.5d,
                AreaM2 = 10d,
                VolumeM3 = 2.5d,
                MassKg = 6000d,
                ProjectId = "PROJECT-1",
                DrawingFingerprint = "DRAWING-1"
            };
            row.ElementIds.Add("E-001");
            row.SourceHandles.Add("AA1");
            return row;
        }

        private static string NewDirectory()
        {
            return Path.Combine(Path.GetTempPath(), "qs3d-material-usage-count-contract-" + Guid.NewGuid().ToString("N"));
        }

        private static void DeleteDirectory(string directory)
        {
            try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
            catch { }
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private sealed class CountContractRows : IReadOnlyList<MaterialUsageRow>, ICollection<MaterialUsageRow>, ICollection
        {
            private readonly MaterialUsageRow[] _items = { ValidRow() };
            private readonly int _readOnlyCount;
            private readonly int _genericCount;
            private readonly int _nonGenericCount;
            private readonly bool _driftAfterRead;
            private readonly bool _driftPrimaryQuantityAfterRead;
            private bool _readOccurred;

            public CountContractRows(
                int readOnlyCount,
                int genericCount,
                int nonGenericCount,
                bool driftAfterRead,
                bool driftPrimaryQuantityAfterRead = false)
            {
                _readOnlyCount = readOnlyCount;
                _genericCount = genericCount;
                _nonGenericCount = nonGenericCount;
                _driftAfterRead = driftAfterRead;
                _driftPrimaryQuantityAfterRead = driftPrimaryQuantityAfterRead;
            }

            int IReadOnlyCollection<MaterialUsageRow>.Count => Current(_readOnlyCount);
            int ICollection<MaterialUsageRow>.Count => Current(_genericCount);
            int ICollection.Count => Current(_nonGenericCount);

            public MaterialUsageRow this[int index]
            {
                get
                {
                    var value = _items[index];
                    _readOccurred = true;
                    return value;
                }
            }

            private int Current(int initial)
            {
                if (_driftPrimaryQuantityAfterRead && _readOccurred)
                    _items[0].PrimaryQuantity = 7.5d;
                return _driftAfterRead && _readOccurred ? 0 : initial;
            }

            bool ICollection<MaterialUsageRow>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            void ICollection<MaterialUsageRow>.Add(MaterialUsageRow item) { throw new NotSupportedException(); }
            void ICollection<MaterialUsageRow>.Clear() { throw new NotSupportedException(); }
            bool ICollection<MaterialUsageRow>.Contains(MaterialUsageRow item) { return Array.IndexOf(_items, item) >= 0; }
            void ICollection<MaterialUsageRow>.CopyTo(MaterialUsageRow[] array, int arrayIndex) { _items.CopyTo(array, arrayIndex); }
            bool ICollection<MaterialUsageRow>.Remove(MaterialUsageRow item) { throw new NotSupportedException(); }
            void ICollection.CopyTo(Array array, int index) { _items.CopyTo(array, index); }

            public IEnumerator<MaterialUsageRow> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++) yield return _items[i];
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
    }
}
