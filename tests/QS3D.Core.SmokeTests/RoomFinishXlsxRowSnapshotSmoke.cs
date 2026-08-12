using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxRowSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportReadsCallerRowOnceBeforeIo();
        }

        private static void ExportReadsCallerRowOnceBeforeIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-row-snapshot-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "room-finish.xlsx");
            var row = new RoomFinishScheduleRow
            {
                Floor = "L1",
                Room = "101",
                Category = "FloorFinish",
                FamilyName = "Tile-600",
                Material = "Tile",
                UnitHint = "m2",
                Count = 1,
                LengthM = 0d,
                AreaM2 = 12d,
                PrimaryQuantity = 12d
            };
            row.ElementIds.Add("FINISH-1");
            row.RoomIds.Add("ROOM-101");
            var rows = new SingleReadRowList(row);

            try
            {
                RoomFinishXlsxExporter.Export(path, rows);
                if (!File.Exists(path))
                    throw new Exception("Room-finish XLSX export must succeed from the detached validated row snapshot.");
                if (rows.IndexReadCount != 1)
                    throw new Exception("Room-finish XLSX export must read each caller-owned row index exactly once before filesystem work.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class SingleReadRowList : IReadOnlyList<RoomFinishScheduleRow>
        {
            private readonly RoomFinishScheduleRow _row;

            public SingleReadRowList(RoomFinishScheduleRow row) => _row = row;

            public int Count => 1;
            public int IndexReadCount { get; private set; }

            public RoomFinishScheduleRow this[int index]
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

            public IEnumerator<RoomFinishScheduleRow> GetEnumerator() =>
                throw new InvalidOperationException("Room-finish XLSX exporter must not enumerate the caller-owned row list.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
