using System;
using System.IO;
using System.IO.Compression;
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
            RejectsMalformedUtf16AcrossProvenanceBeforeReplace();
            PreservesValidSupplementaryUnicode();
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

        private static void RejectsMalformedUtf16AcrossProvenanceBeforeReplace()
        {
            foreach (var malformed in new[] { "\uD800", "\uDC00" })
            {
                RejectsMalformedUtf16(
                    row => row.ProjectId = "PROJECT-" + malformed,
                    "ProjectId");
                RejectsMalformedUtf16(
                    row => row.DrawingFingerprint = "DRAWING-" + malformed,
                    "DrawingFingerprint");
                RejectsMalformedUtf16(
                    row => row.ElementIds.Add("ELEMENT-" + malformed),
                    "ElementIds");
                RejectsMalformedUtf16(
                    row => row.RoomIds.Add("ROOM-" + malformed),
                    "RoomIds");
                RejectsMalformedUtf16(
                    row =>
                    {
                        row.SourceHandles.Clear();
                        row.SourceHandles.Add("HANDLE-" + malformed);
                    },
                    "SourceHandles");
            }
        }

        private static void RejectsMalformedUtf16(Action<RoomFinishScheduleRow> mutate, string label)
        {
            WithDestination((path, original) =>
            {
                var row = ValidRow();
                mutate(row);

                ExpectArgument(
                    () => RoomFinishXlsxExporter.Export(path, new[] { row }),
                    "Room Finish XLSX accepted malformed UTF-16 in " + label);

                Equal(original, File.ReadAllText(path), "Room Finish XLSX replaced the destination before rejecting malformed UTF-16 in " + label);
            });
        }

        private static void PreservesValidSupplementaryUnicode()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-unicode-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "room-finish.xlsx");
            try
            {
                var row = ValidRow();
                row.ProjectId = "PROJECT-\U0001F680";
                row.DrawingFingerprint = "DRAWING-\U0001F4A1";
                row.ElementIds.Add("ELEMENT-\U0001F680");
                row.RoomIds.Add("ROOM-\U0001F4A1");
                row.SourceHandles.Clear();
                row.SourceHandles.Add("HANDLE-\U0001F680");

                RoomFinishXlsxExporter.Export(path, new[] { row });

                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (entry == null)
                        throw new InvalidOperationException("RoomFinishXlsxIdentityProvenanceSmoke: generated workbook is missing xl/worksheets/sheet1.xml.");

                    using (var reader = new StreamReader(entry.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        Contains(xml, row.ProjectId, "ProjectId supplementary Unicode changed");
                        Contains(xml, row.DrawingFingerprint, "DrawingFingerprint supplementary Unicode changed");
                        Contains(xml, row.ElementIds[0], "ElementId supplementary Unicode changed");
                        Contains(xml, row.RoomIds[0], "RoomId supplementary Unicode changed");
                        Contains(xml, row.SourceHandles[0], "SourceHandle supplementary Unicode changed");
                        if (xml.IndexOf('\uFFFD') >= 0)
                            throw new InvalidOperationException("RoomFinishXlsxIdentityProvenanceSmoke: valid supplementary Unicode was replaced with U+FFFD.");
                    }
                }
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
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

        private static void Contains(string actual, string expected, string message)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("RoomFinishXlsxIdentityProvenanceSmoke: " + message + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "RoomFinishXlsxIdentityProvenanceSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
