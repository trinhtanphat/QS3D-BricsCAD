using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityXlsxRowSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportReadsCallerRowOnceBeforeIo();
        }

        private static void ExportReadsCallerRowOnceBeforeIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-quantity-xlsx-row-snapshot-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "quantity.xlsx");
            var row = new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "Wall",
                FamilyName = "W200",
                DrawingFingerprint = "FP-1",
                Count = 1,
                GrossConcreteM3 = 2d,
                DeductionM3 = 0.25d,
                NetConcreteM3 = 1.75d,
                FormworkM2 = 8d,
                LengthM = 4d
            };
            row.ElementIds.Add("E-1");
            row.SourceHandles.Add("1A");
            var rows = new SingleReadRowList(row);

            try
            {
                XlsxQuantityExporter.Export(path, rows);
                if (!File.Exists(path))
                    throw new Exception("Quantity XLSX export must succeed from the detached validated row snapshot.");
                if (rows.IndexReadCount != 1)
                    throw new Exception("Quantity XLSX export must read each caller-owned row index exactly once before filesystem work.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class SingleReadRowList : IReadOnlyList<QuantityReportRow>
        {
            private readonly QuantityReportRow _row;

            public SingleReadRowList(QuantityReportRow row) => _row = row;

            public int Count => 1;
            public int IndexReadCount { get; private set; }

            public QuantityReportRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    IndexReadCount++;
                    if (IndexReadCount > 1)
                        throw new InvalidOperationException("Caller-owned quantity row index was read again after preflight.");
                    return _row;
                }
            }

            public IEnumerator<QuantityReportRow> GetEnumerator() =>
                throw new InvalidOperationException("Quantity XLSX exporter must not enumerate the caller-owned row list.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
