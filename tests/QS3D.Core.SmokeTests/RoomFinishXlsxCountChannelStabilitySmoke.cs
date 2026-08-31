using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxCountChannelStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            RejectsIndexerInducedGenericCountDriftBeforeFilesystem();
            AcceptsStableMultiInterfaceCounts();
        }

        private static void RejectsIndexerInducedGenericCountDriftBeforeFilesystem()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-count-channel-drift-" + Guid.NewGuid().ToString("N"));
            var destination = Path.Combine(root, "room-finish.xlsx");
            var rows = new DriftingRows(ValidRow(), driftAfterIndexer: true);
            try
            {
                try
                {
                    RoomFinishXlsxExporter.Export(destination, rows);
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.IndexOf("conflicting known collection counts", StringComparison.OrdinalIgnoreCase) < 0 &&
                        ex.Message.IndexOf("count changed", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Room-finish XLSX count-channel drift rejection did not identify the count contract.", ex);
                    if (rows.IndexerReads != 1)
                        throw new InvalidOperationException("Room-finish XLSX must reject indexer-induced count drift immediately after the first row indexer.");
                    if (Directory.Exists(root))
                        throw new InvalidOperationException("Room-finish XLSX count-channel drift touched the filesystem before rejection.");
                    return;
                }

                throw new InvalidOperationException("Room-finish XLSX accepted indexer-induced alternate Count-channel drift.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static void AcceptsStableMultiInterfaceCounts()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-count-channel-stable-" + Guid.NewGuid().ToString("N"));
            var destination = Path.Combine(root, "room-finish.xlsx");
            var rows = new DriftingRows(ValidRow(), driftAfterIndexer: false);
            try
            {
                RoomFinishXlsxExporter.Export(destination, rows);
                if (!File.Exists(destination))
                    throw new InvalidOperationException("Room-finish XLSX stable multi-interface input did not publish the workbook.");
                if (rows.IndexerReads != 1)
                    throw new InvalidOperationException("Room-finish XLSX stable multi-interface input must traverse exactly one row.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static RoomFinishScheduleRow ValidRow()
        {
            var row = new RoomFinishScheduleRow
            {
                ProjectId = "project",
                DrawingFingerprint = "drawing-fingerprint",
                Floor = "L1",
                Room = "101",
                Category = "WallFinish",
                FamilyName = "Paint",
                Material = "Paint",
                UnitHint = "m²",
                Count = 1,
                PrimaryQuantity = 1d,
                LengthM = 0d,
                AreaM2 = 1d
            };
            row.ElementIds.Add("E1");
            row.RoomIds.Add("R1");
            row.SourceHandles.Add("A1");
            return row;
        }

        private sealed class DriftingRows : IReadOnlyList<RoomFinishScheduleRow>, ICollection<RoomFinishScheduleRow>, ICollection
        {
            private readonly RoomFinishScheduleRow _row;
            private readonly bool _driftAfterIndexer;
            private bool _drifted;

            internal DriftingRows(RoomFinishScheduleRow row, bool driftAfterIndexer)
            {
                _row = row;
                _driftAfterIndexer = driftAfterIndexer;
            }

            public int Count => 1;
            int ICollection<RoomFinishScheduleRow>.Count => _drifted ? 2 : 1;
            int ICollection.Count => 1;
            public int IndexerReads { get; private set; }
            bool ICollection<RoomFinishScheduleRow>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public RoomFinishScheduleRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    if (_driftAfterIndexer) _drifted = true;
                    return _row;
                }
            }

            void ICollection<RoomFinishScheduleRow>.Add(RoomFinishScheduleRow item) => throw new NotSupportedException();
            void ICollection<RoomFinishScheduleRow>.Clear() => throw new NotSupportedException();
            bool ICollection<RoomFinishScheduleRow>.Contains(RoomFinishScheduleRow item) => ReferenceEquals(item, _row);
            void ICollection<RoomFinishScheduleRow>.CopyTo(RoomFinishScheduleRow[] array, int arrayIndex) => array[arrayIndex] = _row;
            bool ICollection<RoomFinishScheduleRow>.Remove(RoomFinishScheduleRow item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_row, index);
            public IEnumerator<RoomFinishScheduleRow> GetEnumerator() { yield return _row; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
