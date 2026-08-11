using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxStructuralLimitSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsOversizedWorksheetBeforeIndexingOrFilesystemMutation();
            AcceptsExactCellTextLimit();
            RejectsOversizedScalarCellBeforeFilesystemMutation();
            RejectsOversizedJoinedCellBeforeFilesystemMutation();
        }

        private static void RejectsOversizedWorksheetBeforeIndexingOrFilesystemMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-row-limit-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "room-finish.xlsx");
            try
            {
                Throws<ArgumentOutOfRangeException>(() => RoomFinishXlsxExporter.Export(path, new OversizedRows()));
                if (Directory.Exists(root) || File.Exists(path))
                    throw new Exception("Oversized Room Finish worksheet must fail before row indexing or filesystem mutation.");
            }
            finally { Delete(root); }
        }

        private static void AcceptsExactCellTextLimit()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-cell-ok-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "room-finish.xlsx");
            try
            {
                RoomFinishXlsxExporter.Export(path, new[]
                {
                    new RoomFinishScheduleRow
                    {
                        Material = new string('A', 32767),
                        Count = 1
                    }
                });
                if (!File.Exists(path)) throw new Exception("Room Finish XLSX must accept exactly 32,767 text characters.");
            }
            finally { Delete(root); }
        }

        private static void RejectsOversizedScalarCellBeforeFilesystemMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-cell-reject-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "room-finish.xlsx");
            try
            {
                Throws<ArgumentOutOfRangeException>(() => RoomFinishXlsxExporter.Export(path, new[]
                {
                    new RoomFinishScheduleRow
                    {
                        Material = new string('B', 32768),
                        Count = 1
                    }
                }));
                if (Directory.Exists(root) || File.Exists(path))
                    throw new Exception("Oversized Room Finish scalar text must fail before filesystem mutation.");
            }
            finally { Delete(root); }
        }

        private static void RejectsOversizedJoinedCellBeforeFilesystemMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-joined-reject-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "room-finish.xlsx");
            try
            {
                var row = new RoomFinishScheduleRow { Count = 1 };
                row.ElementIds.Add(new string('C', 16384));
                row.ElementIds.Add(new string('D', 16383));
                Throws<ArgumentOutOfRangeException>(() => RoomFinishXlsxExporter.Export(path, new[] { row }));
                if (Directory.Exists(root) || File.Exists(path))
                    throw new Exception("Oversized joined Room Finish IDs must fail before string.Join or filesystem mutation.");
            }
            finally { Delete(root); }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Delete(string root)
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private sealed class OversizedRows : IReadOnlyList<RoomFinishScheduleRow>
        {
            public int Count => 1048576;
            public RoomFinishScheduleRow this[int index] => throw new Exception("Row limit must be checked before indexing oversized input.");
            public IEnumerator<RoomFinishScheduleRow> GetEnumerator() => throw new Exception("Row limit must be checked before enumerating oversized input.");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
