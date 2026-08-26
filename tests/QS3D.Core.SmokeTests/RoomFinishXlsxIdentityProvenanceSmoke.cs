using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxIdentityProvenanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsInvalidElementIdBeforeReplace();
            RejectsInvalidRoomIdBeforeReplace();
        }

        private static void RejectsInvalidElementIdBeforeReplace()
        {
            WithDestination((path, original) =>
            {
                var row = ValidRow();
                row.ElementIds.Add("ELEMENT-\u0001-BAD");

                ExpectArgument(
                    () => RoomFinishXlsxExporter.Export(path, new[] { row }),
                    "Room Finish XLSX accepted an Element ID containing an invalid XML control character");

                Equal(original, File.ReadAllText(path), "Room Finish XLSX replaced the destination before rejecting the invalid Element ID");
            });
        }

        private static void RejectsInvalidRoomIdBeforeReplace()
        {
            WithDestination((path, original) =>
            {
                var row = ValidRow();
                row.ElementIds.Add("ELEMENT-001");
                row.RoomIds.Add("ROOM-\u0001-BAD");

                ExpectArgument(
                    () => RoomFinishXlsxExporter.Export(path, new[] { row }),
                    "Room Finish XLSX accepted a Room ID containing an invalid XML control character");

                Equal(original, File.ReadAllText(path), "Room Finish XLSX replaced the destination before rejecting the invalid Room ID");
            });
        }

        private static RoomFinishScheduleRow ValidRow()
        {
            var row = new RoomFinishScheduleRow
            {
                ProjectId = "PROJECT-001",
                DrawingFingerprint = "DRAWING-001",
                Floor = "L1",
                Room = "Room 101",
                Category = "FloorFinish",
                FamilyName = "Tile",
                Material = "Ceramic",
                UnitHint = "m²",
                Count = 1,
                PrimaryQuantity = 1d,
                AreaM2 = 1d,
                LengthM = 0d
            };
            row.SourceHandles.Add("1A2B");
            return row;
        }

        private static void ExpectArgument(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("RoomFinishXlsxIdentityProvenanceSmoke: " + message + ".");
        }

        private static void WithDestination(Action<string, string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-identity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "room-finish.xlsx");
            const string original = "existing-destination";
            File.WriteAllText(path, original);
            try
            {
                action(path, original);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "RoomFinishXlsxIdentityProvenanceSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
