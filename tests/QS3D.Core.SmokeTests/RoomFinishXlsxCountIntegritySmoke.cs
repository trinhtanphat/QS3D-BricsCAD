using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxCountIntegritySmoke
    {
        internal static void Run()
        {
            AssertRowCountDriftFailsBeforeExistingDestinationReplacement();
            AssertRowCountDriftFailsBeforeFilesystemCreation();
        }

        private static void AssertRowCountDriftFailsBeforeExistingDestinationReplacement()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "room-finish.xlsx");
                const string sentinel = "preserve-existing-room-finish-destination";
                File.WriteAllText(destination, sentinel);

                AssertRowCountDrift(destination);

                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Room-finish XLSX row-count drift replaced an existing destination file.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertRowCountDriftFailsBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertRowCountDrift(Path.Combine(untouchedDirectory, "room-finish.xlsx"));
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Room-finish XLSX row-count drift touched the filesystem before failing.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertRowCountDrift(string destination)
        {
            try
            {
                RoomFinishXlsxExporter.Export(destination, new CountDriftingRows(ValidRow()));
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("row count changed during snapshot", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Room-finish XLSX row-count drift must identify snapshot count instability.", ex);
                return;
            }

            throw new InvalidOperationException("Room-finish XLSX exporter accepted a source whose row count changed during snapshot.");
        }

        private static RoomFinishScheduleRow ValidRow()
        {
            var row = new RoomFinishScheduleRow
            {
                Floor = "L1",
                Room = "R1",
                Category = "FloorFinish",
                FamilyName = "Finish",
                Material = "Tile",
                UnitHint = "m²",
                Count = 1,
                PrimaryQuantity = 12.5d,
                LengthM = 3.5d,
                AreaM2 = 12.5d
            };
            row.ElementIds.Add("E1");
            row.RoomIds.Add("R1");
            return row;
        }

        private sealed class CountDriftingRows : IReadOnlyList<RoomFinishScheduleRow>
        {
            private readonly RoomFinishScheduleRow _row;
            private int _countReads;

            internal CountDriftingRows(RoomFinishScheduleRow row)
            {
                _row = row;
            }

            public int Count
            {
                get
                {
                    _countReads++;
                    return _countReads == 1 ? 1 : 2;
                }
            }

            public RoomFinishScheduleRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    return _row;
                }
            }

            public IEnumerator<RoomFinishScheduleRow> GetEnumerator()
            {
                yield return _row;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
