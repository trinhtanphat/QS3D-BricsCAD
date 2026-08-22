using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BbsXlsxRowSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportReadsCallerRowOnceBeforeIo();
        }

        private static void ExportReadsCallerRowOnceBeforeIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-bbs-xlsx-row-snapshot-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "bbs.xlsx");
            var row = new RebarScheduleRow
            {
                ElementId = "BEAM-1",
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "2Ø16",
                DiameterMm = 16d,
                Quantity = 2,
                CuttingLengthM = 3d,
                TotalLengthM = 6d,
                UnitWeightKgM = 1.58d,
                NetWeightKg = 9.48d,
                WastePercent = 5d,
                TotalWeightKg = 9.954d,
                FabricationStatus = "Reviewed",
                FabricationStandardCode = "LOCAL",
                FabricationDetailingRevision = "R1"
            };
            var rows = new SingleReadRowList(row);

            try
            {
                XlsxRebarScheduleExporter.Export(path, rows);
                if (!File.Exists(path))
                    throw new Exception("BBS XLSX export must succeed from the detached validated row snapshot.");
                if (rows.IndexReadCount != 1)
                    throw new Exception("BBS XLSX export must read each caller-owned row index exactly once before filesystem work.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class SingleReadRowList : IReadOnlyList<RebarScheduleRow>
        {
            private readonly RebarScheduleRow _row;

            public SingleReadRowList(RebarScheduleRow row) => _row = row;

            public int Count => 1;
            public int IndexReadCount { get; private set; }

            public RebarScheduleRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    IndexReadCount++;
                    if (IndexReadCount > 1)
                        throw new InvalidOperationException("Caller-owned BBS row index was read again after preflight.");
                    return _row;
                }
            }

            public IEnumerator<RebarScheduleRow> GetEnumerator() =>
                throw new InvalidOperationException("BBS XLSX exporter must not enumerate the caller-owned row list.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
