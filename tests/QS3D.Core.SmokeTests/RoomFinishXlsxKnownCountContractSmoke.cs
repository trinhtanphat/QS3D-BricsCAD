using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxKnownCountContractSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            RejectsConflictingKnownCountsBeforeTraversalOrFilesystem();
            RejectsNegativeKnownCountBeforeTraversalOrFilesystem();
            RejectsOversizedKnownCountBeforeTraversalOrFilesystem();
            AcceptsHonestMultiInterfaceKnownCounts();
        }

        private static void RejectsConflictingKnownCountsBeforeTraversalOrFilesystem()
        {
            // One public object can expose several deterministic collection Count contracts at once.
            var rows = new KnownCountRows(ValidRow(), 1, 2, 3);
            ExpectRejected<InvalidOperationException>(rows, "conflicting known collection counts");
        }

        private static void RejectsNegativeKnownCountBeforeTraversalOrFilesystem()
        {
            var rows = new KnownCountRows(ValidRow(), -1, -1, -1);
            ExpectRejected<ArgumentOutOfRangeException>(rows, "count must be non-negative");
        }

        private static void RejectsOversizedKnownCountBeforeTraversalOrFilesystem()
        {
            const int tooManyRows = 1048576;
            var rows = new KnownCountRows(ValidRow(), tooManyRows, tooManyRows, tooManyRows);
            ExpectRejected<ArgumentOutOfRangeException>(rows, "count exceeds the supported maximum");
        }

        private static void AcceptsHonestMultiInterfaceKnownCounts()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-known-count-honest-" + Guid.NewGuid().ToString("N"));
            var destination = Path.Combine(root, "room-finish.xlsx");
            var rows = new KnownCountRows(ValidRow(), 1, 1, 1);
            try
            {
                RoomFinishXlsxExporter.Export(destination, rows);
                if (!File.Exists(destination))
                    throw new InvalidOperationException("Room-finish XLSX honest multi-interface input did not publish the workbook.");
                if (rows.IndexerReads != 1)
                    throw new InvalidOperationException("Room-finish XLSX honest multi-interface input must traverse exactly one row.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static void ExpectRejected<TException>(KnownCountRows rows, string messageToken)
            where TException : Exception
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-known-count-reject-" + Guid.NewGuid().ToString("N"));
            var destination = Path.Combine(root, "room-finish.xlsx");
            try
            {
                try
                {
                    RoomFinishXlsxExporter.Export(destination, rows);
                }
                catch (TException ex)
                {
                    if (ex.Message.IndexOf(messageToken, StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Room-finish XLSX known-count rejection did not identify the expected contract.", ex);
                    if (rows.IndexerReads != 0)
                        throw new InvalidOperationException("Room-finish XLSX traversed rows before rejecting invalid deterministic known counts.");
                    if (Directory.Exists(root))
                        throw new InvalidOperationException("Room-finish XLSX invalid deterministic known counts touched the filesystem before rejection.");
                    return;
                }

                throw new InvalidOperationException("Room-finish XLSX accepted an invalid deterministic known-count contract.");
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

        private sealed class KnownCountRows : IReadOnlyList<RoomFinishScheduleRow>, ICollection<RoomFinishScheduleRow>, ICollection
        {
            private readonly RoomFinishScheduleRow _row;
            private readonly int _readOnlyCount;
            private readonly int _genericCount;
            private readonly int _nonGenericCount;

            internal KnownCountRows(RoomFinishScheduleRow row, int readOnlyCount, int genericCount, int nonGenericCount)
            {
                _row = row;
                _readOnlyCount = readOnlyCount;
                _genericCount = genericCount;
                _nonGenericCount = nonGenericCount;
            }

            public int Count => _readOnlyCount;
            int ICollection<RoomFinishScheduleRow>.Count => _genericCount;
            int ICollection.Count => _nonGenericCount;
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
