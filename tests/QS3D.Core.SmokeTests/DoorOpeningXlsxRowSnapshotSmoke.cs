using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxRowSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportReadsCallerRowOnceBeforeIo();
        }

        private static void ExportReadsCallerRowOnceBeforeIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-row-snapshot-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "doors.xlsx");
            var row = new DoorOpeningScheduleRow
            {
                Floor = "L1",
                Category = "Door",
                FamilyName = "D900",
                Material = "Timber",
                WidthM = 0.9d,
                HeightM = 2.2d,
                SillHeightM = 0d,
                ThicknessM = 0.1d,
                Count = 1,
                OpeningAreaM2 = 1.98d,
                HostCount = 1
            };
            row.ElementIds.Add("DOOR-1");
            row.HostIds.Add("WALL-1");
            var rows = new SingleReadRowList(row);

            try
            {
                DoorOpeningXlsxExporter.Export(path, rows);
                if (!File.Exists(path))
                    throw new Exception("Door/opening XLSX export must succeed from the detached validated row snapshot.");
                if (rows.IndexReadCount != 1)
                    throw new Exception("Door/opening XLSX export must read each caller-owned row index exactly once before filesystem work.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class SingleReadRowList : IReadOnlyList<DoorOpeningScheduleRow>
        {
            private readonly DoorOpeningScheduleRow _row;

            public SingleReadRowList(DoorOpeningScheduleRow row) => _row = row;

            public int Count => 1;
            public int IndexReadCount { get; private set; }

            public DoorOpeningScheduleRow this[int index]
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

            public IEnumerator<DoorOpeningScheduleRow> GetEnumerator() =>
                throw new InvalidOperationException("Door/opening XLSX exporter must not enumerate the caller-owned row list.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
