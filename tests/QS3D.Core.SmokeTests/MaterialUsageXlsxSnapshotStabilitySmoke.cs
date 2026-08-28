using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageXlsxSnapshotStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CountStableScalarMutationFailsBeforePublication();
            CountStableProvenanceMutationFailsBeforePublication();
        }

        private static void CountStableScalarMutationFailsBeforePublication()
        {
            var row = CreateRow();
            var rows = new CountStableMutatingRows(row, () => row.MaterialName = "Mutated after snapshot");
            AssertMutationRejected(rows, "scalar mutation");
        }

        private static void CountStableProvenanceMutationFailsBeforePublication()
        {
            var row = CreateRow();
            var rows = new CountStableMutatingRows(row, () => row.ElementIds[0] = "E-MUTATED");
            AssertMutationRejected(rows, "provenance mutation");
        }

        private static MaterialUsageRow CreateRow()
        {
            var row = new MaterialUsageRow
            {
                ProjectId = "P-1",
                DrawingFingerprint = "D-1",
                Floor = "L1",
                MaterialName = "Concrete",
                UnitHint = "m3",
                Component = "Body",
                Category = "Wall",
                FamilyName = "W200",
                ElementCount = 1,
                VolumeM3 = 1.25d
            };
            row.ElementIds.Add("E-1");
            row.SourceHandles.Add("H-1");
            return row;
        }

        private static void AssertMutationRejected(IReadOnlyList<MaterialUsageRow> rows, string label)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-material-xlsx-snapshot-stability-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "material.xlsx");
            const string sentinel = "existing-destination-must-survive";
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(path, sentinel);
                try
                {
                    MaterialUsageXlsxExporter.Export(path, rows);
                    throw new Exception("Material XLSX " + label + " must fail closed before publication.");
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.IndexOf("changed during snapshot traversal", StringComparison.Ordinal) < 0)
                        throw;
                }
                if (!File.Exists(path) || File.ReadAllText(path) != sentinel)
                    throw new Exception("Material XLSX " + label + " must preserve the existing destination when snapshot validation fails.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class CountStableMutatingRows : IReadOnlyList<MaterialUsageRow>
        {
            private readonly MaterialUsageRow _row;
            private readonly Action _mutate;
            private int _countReads;
            private int _indexReads;

            internal CountStableMutatingRows(MaterialUsageRow row, Action mutate)
            {
                _row = row;
                _mutate = mutate;
            }

            public int Count
            {
                get
                {
                    _countReads++;
                    if (_countReads == 2) _mutate();
                    return 1;
                }
            }

            public MaterialUsageRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    _indexReads++;
                    if (_indexReads > 1)
                        throw new InvalidOperationException("Material XLSX must preserve the single-read outer-row contract.");
                    return _row;
                }
            }

            public IEnumerator<MaterialUsageRow> GetEnumerator() =>
                throw new InvalidOperationException("Material XLSX must not enumerate caller-owned rows.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
