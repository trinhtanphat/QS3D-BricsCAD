using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxSnapshotStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CountStableScalarMutationFailsBeforePublication();
            CountStableElementProvenanceMutationFailsBeforePublication();
            CountStableHostProvenanceMutationFailsBeforePublication();
            CountStableSourceHandleMutationFailsBeforePublication();
        }

        private static void CountStableScalarMutationFailsBeforePublication()
        {
            var row = CreateRow();
            var rows = new CountStableMutatingRows(row, () => row.Material = "Mutated after snapshot");
            AssertMutationRejected(rows, "scalar mutation");
        }

        private static void CountStableElementProvenanceMutationFailsBeforePublication()
        {
            var row = CreateRow();
            var rows = new CountStableMutatingRows(row, () => row.ElementIds[0] = "E-MUTATED");
            AssertMutationRejected(rows, "ElementIds mutation");
        }

        private static void CountStableHostProvenanceMutationFailsBeforePublication()
        {
            var row = CreateRow();
            var rows = new CountStableMutatingRows(row, () => row.HostIds[0] = "HOST-MUTATED");
            AssertMutationRejected(rows, "HostIds mutation");
        }

        private static void CountStableSourceHandleMutationFailsBeforePublication()
        {
            var row = CreateRow();
            var rows = new CountStableMutatingRows(row, () => row.SourceHandles[0] = "HANDLE-MUTATED");
            AssertMutationRejected(rows, "SourceHandles mutation");
        }

        private static DoorOpeningScheduleRow CreateRow()
        {
            var row = new DoorOpeningScheduleRow
            {
                ProjectId = "P-1",
                DrawingFingerprint = "D-1",
                Floor = "L1",
                Category = "Door",
                FamilyName = "D900",
                Material = "Timber",
                WidthM = 0.9d,
                HeightM = 2.1d,
                SillHeightM = 0d,
                ThicknessM = 0.1d,
                Count = 1,
                OpeningAreaM2 = 1.89d,
                HostCount = 1
            };
            row.ElementIds.Add("E-1");
            row.HostIds.Add("HOST-1");
            row.SourceHandles.Add("H-1");
            return row;
        }

        private static void AssertMutationRejected(IReadOnlyList<DoorOpeningScheduleRow> rows, string label)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-xlsx-snapshot-stability-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "door-opening.xlsx");
            const string sentinel = "existing-destination-must-survive";
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(path, sentinel);
                try
                {
                    DoorOpeningXlsxExporter.Export(path, rows);
                    throw new Exception("Door/opening XLSX " + label + " must fail closed before publication.");
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.IndexOf("changed during snapshot traversal", StringComparison.Ordinal) < 0)
                        throw;
                }
                if (!File.Exists(path) || File.ReadAllText(path) != sentinel)
                    throw new Exception("Door/opening XLSX " + label + " must preserve the existing destination when snapshot validation fails.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private sealed class CountStableMutatingRows : IReadOnlyList<DoorOpeningScheduleRow>
        {
            private readonly DoorOpeningScheduleRow _row;
            private readonly Action _mutate;
            private int _countReads;
            private int _indexReads;

            internal CountStableMutatingRows(DoorOpeningScheduleRow row, Action mutate)
            {
                _row = row;
                _mutate = mutate;
            }

            public int Count
            {
                get
                {
                    _countReads++;
                    if (_countReads == 4) _mutate();
                    return 1;
                }
            }

            public DoorOpeningScheduleRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    _indexReads++;
                    if (_indexReads > 1)
                        throw new InvalidOperationException("Door/opening XLSX must preserve the single-read outer-row contract.");
                    return _row;
                }
            }

            public IEnumerator<DoorOpeningScheduleRow> GetEnumerator() =>
                throw new InvalidOperationException("Door/opening XLSX must not enumerate caller-owned rows.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}