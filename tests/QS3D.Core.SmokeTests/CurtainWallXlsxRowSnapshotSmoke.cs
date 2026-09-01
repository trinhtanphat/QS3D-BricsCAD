using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallXlsxRowSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportReadsCallerRowOnceBeforeIo();
        }

        private static void ExportReadsCallerRowOnceBeforeIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-curtain-xlsx-row-snapshot-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "curtain.xlsx");
            var row = new CurtainWallScheduleRow
            {
                Floor = "L1",
                FamilyName = "CW-01",
                WallCount = 1,
                TotalWallLengthM = 4d,
                GrossWallAreaM2 = 12d,
                OpeningAreaM2 = 1d,
                NetGlassAreaM2 = 10d,
                FrameFaceAreaM2 = 1d,
                FrameLengthM = 8d,
                PanelCount = 4,
                VerticalFrameCount = 3,
                HorizontalFrameCount = 2,
                MinimumClearPanelWidthM = 0.9d,
                MaximumClearPanelWidthM = 1.1d,
                MinimumClearPanelHeightM = 2.4d,
                MaximumClearPanelHeightM = 3d,
                ElementIds = { "CW-SNAPSHOT-01" },
                SourceHandles = { "CW-SNAPSHOT-HANDLE-01" }
            };
            var rows = new SingleReadRowList(row);

            try
            {
                CurtainWallXlsxExporter.Export(path, rows);
                if (!File.Exists(path))
                    throw new Exception("Curtain XLSX export must succeed from the detached validated row snapshot.");
                if (rows.IndexReadCount != 1)
                    throw new Exception("Curtain XLSX export must read each caller-owned row index exactly once before filesystem work.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class SingleReadRowList : IReadOnlyList<CurtainWallScheduleRow>
        {
            private readonly CurtainWallScheduleRow _row;

            public SingleReadRowList(CurtainWallScheduleRow row) => _row = row;

            public int Count => 1;
            public int IndexReadCount { get; private set; }

            public CurtainWallScheduleRow this[int index]
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

            public IEnumerator<CurtainWallScheduleRow> GetEnumerator() =>
                throw new InvalidOperationException("Curtain XLSX exporter must not enumerate the caller-owned row list.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
