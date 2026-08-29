using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxScheduleNullRowSmoke
    {
        internal static void Run()
        {
            AssertRejectsNullRow(
                "door-opening",
                path => DoorOpeningXlsxExporter.Export(path, new DoorOpeningScheduleRow[] { null! }));
            AssertRejectsNullRow(
                "material-usage",
                path => MaterialUsageXlsxExporter.Export(path, new MaterialUsageRow[] { null! }));
            AssertRejectsNullRow(
                "curtain-wall",
                path => CurtainWallXlsxExporter.Export(path, new CurtainWallScheduleRow[] { null! }));
            AssertRejectsNullRow(
                "room-finish",
                path => RoomFinishXlsxExporter.Export(path, new RoomFinishScheduleRow[] { null! }));

            AssertRejectsInvalidRoomFinishRow("Count", row => row.Count = -1);
            AssertRejectsInvalidRoomFinishRow("PrimaryQuantity", row => row.PrimaryQuantity = -0.01d);
            AssertRejectsInvalidRoomFinishRow("LengthM", row => row.LengthM = -0.01d);
            AssertRejectsInvalidRoomFinishRow("AreaM2", row => row.AreaM2 = -0.01d);
            AssertAcceptsZeroRoomFinishValues();
            AssertCurtainWallCountDriftFailsBeforeExistingDestinationReplacement();
            AssertCurtainWallCountDriftFailsBeforeFilesystemCreation();
        }

        private static void AssertRejectsNullRow(string exportName, Action<string> export)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-null-row-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, exportName + ".xlsx");
                var sentinel = "preserve-existing-destination-" + exportName;
                File.WriteAllText(destination, sentinel);

                AssertNullRowArgument(exportName, destination, export);
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException(exportName + " null-row validation replaced an existing destination file.");

                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertNullRowArgument(exportName, Path.Combine(untouchedDirectory, "invalid.xlsx"), export);
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException(exportName + " null-row validation touched the filesystem before failing.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertNullRowArgument(string exportName, string path, Action<string> export)
        {
            try
            {
                export(path);
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                    throw new InvalidOperationException(exportName + " null-row validation must identify the rows argument.", ex);
                if (ex.Message.IndexOf("row index: 0", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(exportName + " null-row validation must identify the zero-based row index.", ex);
                return;
            }

            throw new InvalidOperationException(exportName + " exporter accepted a null schedule row.");
        }

        private static void AssertRejectsInvalidRoomFinishRow(string fieldName, Action<RoomFinishScheduleRow> mutate)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-invalid-row-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var row = ValidRoomFinishRow();
                mutate(row);
                var destination = Path.Combine(root, "room-finish.xlsx");
                const string sentinel = "preserve-existing-room-finish-destination";
                File.WriteAllText(destination, sentinel);

                AssertInvalidRoomFinishArgument(destination, row, fieldName);
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Room-finish invalid-row validation replaced an existing destination file for " + fieldName + ".");

                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertInvalidRoomFinishArgument(Path.Combine(untouchedDirectory, "invalid.xlsx"), row, fieldName);
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Room-finish invalid-row validation touched the filesystem before failing for " + fieldName + ".");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertInvalidRoomFinishArgument(string path, RoomFinishScheduleRow row, string fieldName)
        {
            try
            {
                RoomFinishXlsxExporter.Export(path, new[] { row });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                    throw new InvalidOperationException("Room-finish invalid-row validation must identify the rows argument for " + fieldName + ".", ex);
                if (ex.Message.IndexOf("worksheet row 2", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Room-finish invalid-row validation must identify worksheet row 2 for " + fieldName + ".", ex);
                if (ex.Message.IndexOf(fieldName, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Room-finish invalid-row validation must identify field " + fieldName + ".", ex);
                return;
            }

            throw new InvalidOperationException("Room-finish exporter accepted invalid field " + fieldName + ".");
        }

        private static void AssertAcceptsZeroRoomFinishValues()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-zero-row-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "room-finish.xlsx");
                var row = ValidRoomFinishRow();
                row.Count = 0;
                row.PrimaryQuantity = 0d;
                row.LengthM = 0d;
                row.AreaM2 = 0d;

                RoomFinishXlsxExporter.Export(destination, new[] { row });
                if (!File.Exists(destination) || new FileInfo(destination).Length == 0)
                    throw new InvalidOperationException("Room-finish XLSX exporter rejected a valid zero-valued schedule row.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertCurtainWallCountDriftFailsBeforeExistingDestinationReplacement()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-curtain-xlsx-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "curtain-wall.xlsx");
                const string sentinel = "preserve-existing-curtain-wall-destination";
                File.WriteAllText(destination, sentinel);

                AssertCurtainWallCountDrift(destination);
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Curtain XLSX row-count drift replaced an existing destination file.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertCurtainWallCountDriftFailsBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-curtain-xlsx-count-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertCurtainWallCountDrift(Path.Combine(untouchedDirectory, "curtain-wall.xlsx"));
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Curtain XLSX row-count drift touched the filesystem before failing.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertCurtainWallCountDrift(string destination)
        {
            try
            {
                CurtainWallXlsxExporter.Export(destination, new CurtainCountDriftingRows(ValidCurtainWallRow()));
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("row count changed during snapshot", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Curtain XLSX row-count drift must identify snapshot count instability.", ex);
                return;
            }

            throw new InvalidOperationException("Curtain XLSX exporter accepted a source whose row count changed during snapshot.");
        }

        private static CurtainWallScheduleRow ValidCurtainWallRow()
        {
            var row = new CurtainWallScheduleRow
            {
                Floor = "L1",
                FamilyName = "CW1",
                WallCount = 1,
                TotalWallLengthM = 4d,
                GrossWallAreaM2 = 10d,
                OpeningAreaM2 = 0d,
                NetGlassAreaM2 = 10d,
                FrameFaceAreaM2 = 0d,
                FrameLengthM = 0d,
                PanelCount = 1,
                VerticalFrameCount = 0,
                HorizontalFrameCount = 0,
                MinimumClearPanelWidthM = 0d,
                MaximumClearPanelWidthM = 0d,
                MinimumClearPanelHeightM = 0d,
                MaximumClearPanelHeightM = 0d
            };
            row.ElementIds.Add("CW-COUNT-DRIFT-1");
            row.SourceHandles.Add("CW-COUNT-DRIFT-HANDLE-1");
            return row;
        }

        private static RoomFinishScheduleRow ValidRoomFinishRow()
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

        private sealed class CurtainCountDriftingRows : IReadOnlyList<CurtainWallScheduleRow>
        {
            private readonly CurtainWallScheduleRow _row;
            private int _countReads;

            internal CurtainCountDriftingRows(CurtainWallScheduleRow row)
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

            public CurtainWallScheduleRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    return _row;
                }
            }

            public IEnumerator<CurtainWallScheduleRow> GetEnumerator()
            {
                yield return _row;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
