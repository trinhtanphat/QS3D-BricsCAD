using System;
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

            AssertRejectsInvalidRoomFinishRow("Count", row => row.Count = 0);
            AssertRejectsInvalidRoomFinishRow("Count", row => row.Count = -1);
            AssertRejectsInvalidRoomFinishRow("PrimaryQuantity", row => row.PrimaryQuantity = -0.01d);
            AssertRejectsInvalidRoomFinishRow("LengthM", row => row.LengthM = -0.01d);
            AssertRejectsInvalidRoomFinishRow("AreaM2", row => row.AreaM2 = -0.01d);
            AssertAcceptsZeroRoomFinishQuantities();
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

        private static void AssertAcceptsZeroRoomFinishQuantities()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-zero-row-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "room-finish.xlsx");
                var row = ValidRoomFinishRow();
                row.PrimaryQuantity = 0d;
                row.LengthM = 0d;
                row.AreaM2 = 0d;

                RoomFinishXlsxExporter.Export(destination, new[] { row });
                if (!File.Exists(destination) || new FileInfo(destination).Length == 0)
                    throw new InvalidOperationException("Room-finish XLSX exporter rejected a valid positive-count row with zero quantities.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
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
    }
}
