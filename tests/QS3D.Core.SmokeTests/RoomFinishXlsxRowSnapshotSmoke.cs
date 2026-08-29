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
            CrossRowTextMutationFailsBeforeIo();
            CrossRowProvenanceMutationFailsBeforeIo();
        }

        private static void ExportReadsCallerRowOnceBeforeIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-row-snapshot-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "room-finish.xlsx");
            var row = ValidRow("101", "FINISH-1", "ROOM-101");
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

        private static void CrossRowTextMutationFailsBeforeIo()
        {
            RunCrossRowMutation(
                "text",
                row => row.Floor = "L-MUTATED");
        }

        private static void CrossRowProvenanceMutationFailsBeforeIo()
        {
            RunCrossRowMutation(
                "provenance",
                row => row.ElementIds[0] = "FINISH-MUTATED");
        }

        private static void RunCrossRowMutation(string label, Action<RoomFinishScheduleRow> mutation)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-row-snapshot-" + label + "-" + Guid.NewGuid().ToString("N") + ".xlsx");
            var first = ValidRow("101", "FINISH-1", "ROOM-101");
            var second = ValidRow("102", "FINISH-2", "ROOM-102");
            var rows = new CrossRowMutatingList(first, second, mutation);

            try
            {
                File.WriteAllText(path, "existing-workbook");
                Throws<InvalidOperationException>(() => RoomFinishXlsxExporter.Export(path, rows));
                if (!string.Equals(File.ReadAllText(path), "existing-workbook", StringComparison.Ordinal))
                    throw new Exception("Room-finish XLSX cross-row " + label + " mutation must preserve the existing destination.");
                if (rows.FirstReadCount != 1 || rows.SecondReadCount != 1)
                    throw new Exception("Room-finish XLSX cross-row mutation detection must preserve the single-read outer-row contract.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static RoomFinishScheduleRow ValidRow(string room, string elementId, string roomId)
        {
            var row = new RoomFinishScheduleRow
            {
                ProjectId = "PROJECT-1",
                DrawingFingerprint = "DRAWING-1",
                Floor = "L1",
                Room = room,
                Category = "FloorFinish",
                FamilyName = "Tile-600",
                Material = "Tile",
                UnitHint = "m2",
                Count = 1,
                LengthM = 0d,
                AreaM2 = 12d,
                PrimaryQuantity = 12d
            };
            row.ElementIds.Add(elementId);
            row.RoomIds.Add(roomId);
            row.SourceHandles.Add("AA" + room);
            return row;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
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

        private sealed class CrossRowMutatingList : IReadOnlyList<RoomFinishScheduleRow>
        {
            private readonly RoomFinishScheduleRow _first;
            private readonly RoomFinishScheduleRow _second;
            private readonly Action<RoomFinishScheduleRow> _mutation;

            public CrossRowMutatingList(RoomFinishScheduleRow first, RoomFinishScheduleRow second, Action<RoomFinishScheduleRow> mutation)
            {
                _first = first;
                _second = second;
                _mutation = mutation;
            }

            public int Count => 2;
            public int FirstReadCount { get; private set; }
            public int SecondReadCount { get; private set; }

            public RoomFinishScheduleRow this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        FirstReadCount++;
                        if (FirstReadCount > 1) throw new InvalidOperationException("First caller-owned row index was read more than once.");
                        return _first;
                    }
                    if (index == 1)
                    {
                        SecondReadCount++;
                        if (SecondReadCount > 1) throw new InvalidOperationException("Second caller-owned row index was read more than once.");
                        _mutation(_first);
                        return _second;
                    }
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public IEnumerator<RoomFinishScheduleRow> GetEnumerator() =>
                throw new InvalidOperationException("Room-finish XLSX exporter must not enumerate caller-owned rows during snapshot validation.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
