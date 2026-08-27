using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxKnownCountContractSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-known-count-" + Guid.NewGuid().ToString("N"));
            try
            {
                var rows = new ConflictingKnownCountRows(ValidRow());
                try
                {
                    DoorOpeningXlsxExporter.Export(Path.Combine(root, "door-opening.xlsx"), rows);
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.IndexOf("conflicting known collection counts", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Door/opening XLSX conflicting-count rejection must identify the count contract.", ex);
                    if (rows.IndexerReads != 0)
                        throw new InvalidOperationException("Door/opening XLSX traversed rows before rejecting conflicting known counts.");
                    if (Directory.Exists(root))
                        throw new InvalidOperationException("Door/opening XLSX conflicting known counts touched the filesystem before rejection.");
                    return;
                }

                throw new InvalidOperationException("Door/opening XLSX accepted contradictory deterministic collection counts.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static DoorOpeningScheduleRow ValidRow()
        {
            var row = new DoorOpeningScheduleRow
            {
                ProjectId = "project",
                DrawingFingerprint = "drawing-fingerprint",
                Floor = "L1",
                Category = "Door",
                FamilyName = "D1",
                Material = "Timber",
                WidthM = 0.9d,
                HeightM = 2.1d,
                SillHeightM = 0d,
                ThicknessM = 0.05d,
                Count = 1,
                OpeningAreaM2 = 1.89d,
                HostCount = 1
            };
            row.ElementIds.Add("E1");
            row.HostIds.Add("H1");
            return row;
        }

        private sealed class ConflictingKnownCountRows : IReadOnlyList<DoorOpeningScheduleRow>, ICollection<DoorOpeningScheduleRow>, ICollection
        {
            private readonly DoorOpeningScheduleRow _row;

            internal ConflictingKnownCountRows(DoorOpeningScheduleRow row)
            {
                _row = row;
            }

            public int Count => 1;
            int ICollection<DoorOpeningScheduleRow>.Count => 2;
            int ICollection.Count => 3;
            public int IndexerReads { get; private set; }
            bool ICollection<DoorOpeningScheduleRow>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public DoorOpeningScheduleRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    return _row;
                }
            }

            void ICollection<DoorOpeningScheduleRow>.Add(DoorOpeningScheduleRow item) => throw new NotSupportedException();
            void ICollection<DoorOpeningScheduleRow>.Clear() => throw new NotSupportedException();
            bool ICollection<DoorOpeningScheduleRow>.Contains(DoorOpeningScheduleRow item) => ReferenceEquals(item, _row);
            void ICollection<DoorOpeningScheduleRow>.CopyTo(DoorOpeningScheduleRow[] array, int arrayIndex) => array[arrayIndex] = _row;
            bool ICollection<DoorOpeningScheduleRow>.Remove(DoorOpeningScheduleRow item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_row, index);
            public IEnumerator<DoorOpeningScheduleRow> GetEnumerator() { yield return _row; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
