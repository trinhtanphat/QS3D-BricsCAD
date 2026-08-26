using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageXlsxRowSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportReadsCallerRowOnceBeforeIo();
        }

        private static void ExportReadsCallerRowOnceBeforeIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-material-xlsx-row-snapshot-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "material.xlsx");
            var row = new MaterialUsageRow
            {
                Floor = "L1",
                MaterialName = "Concrete",
                UnitHint = "m3",
                Component = "Body",
                Category = "Wall",
                FamilyName = "W200",
                ElementCount = 1,
                ElementIds = { "E-1" },
                VolumeM3 = 1.25d
            };
            var rows = new SingleReadRowList(row);

            try
            {
                MaterialUsageXlsxExporter.Export(path, rows);
                if (!File.Exists(path))
                    throw new Exception("Material XLSX export must succeed from the detached validated row snapshot.");
                if (rows.IndexReadCount != 1)
                    throw new Exception("Material XLSX export must read each caller-owned row index exactly once before filesystem work.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class SingleReadRowList : IReadOnlyList<MaterialUsageRow>
        {
            private readonly MaterialUsageRow _row;

            public SingleReadRowList(MaterialUsageRow row) => _row = row;

            public int Count => 1;
            public int IndexReadCount { get; private set; }

            public MaterialUsageRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    IndexReadCount++;
                    if (IndexReadCount > 1)
                        throw new InvalidOperationException("Caller-owned row index was read again after preflight.");
                    return _row;
                }
            }

            public IEnumerator<MaterialUsageRow> GetEnumerator() =>
                throw new InvalidOperationException("Material XLSX exporter must not enumerate the caller-owned row list.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
