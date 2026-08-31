using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxTransientCountStabilitySmoke
    {
        internal static void Run()
        {
            RejectsTransientGenericCountGrowthBeforeIndexer();
            RejectsTransientGenericCountDriftAfterIndexer();
            StableMultiInterfaceSourceReadsEachRowOnce();
        }

        private static void RejectsTransientGenericCountGrowthBeforeIndexer()
        {
            var path = TempPath("growth");
            File.WriteAllText(path, "sentinel");
            var rows = new AdversarialRows(Row(), 1, 2, 1);
            ExpectCountFailure(path, rows, expectedIndexerReads: 0, "transient growth before row indexer");
        }

        private static void RejectsTransientGenericCountDriftAfterIndexer()
        {
            var path = TempPath("post-index");
            File.WriteAllText(path, "sentinel");
            var rows = new AdversarialRows(Row(), 1, 1, 2, 1);
            ExpectCountFailure(path, rows, expectedIndexerReads: 1, "transient drift after row indexer");
        }

        private static void StableMultiInterfaceSourceReadsEachRowOnce()
        {
            var path = TempPath("stable");
            try
            {
                var rows = new AdversarialRows(Row(), 1, 1, 1, 1, 1, 1);
                DoorOpeningXlsxExporter.Export(path, rows);
                if (!File.Exists(path)) throw new InvalidOperationException("Stable counted Door/opening XLSX source did not export.");
                if (rows.IndexerReads != 1) throw new InvalidOperationException("Stable Door/opening XLSX source row must be indexed exactly once; reads=" + rows.IndexerReads + ".");
            }
            finally { TryDelete(path); }
        }

        private static void ExpectCountFailure(string path, AdversarialRows rows, int expectedIndexerReads, string label)
        {
            try
            {
                var failed = false;
                try { DoorOpeningXlsxExporter.Export(path, rows); }
                catch (InvalidOperationException ex) when (ex.Message.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0) { failed = true; }
                if (!failed) throw new InvalidOperationException("Door/opening XLSX exporter accepted " + label + ".");
                if (rows.IndexerReads != expectedIndexerReads)
                    throw new InvalidOperationException(label + " reached an unexpected number of row indexer reads: " + rows.IndexerReads + ".");
                if (!string.Equals(File.ReadAllText(path), "sentinel", StringComparison.Ordinal))
                    throw new InvalidOperationException(label + " mutated the existing destination before failing closed.");
            }
            finally { TryDelete(path); }
        }

        private static DoorOpeningScheduleRow Row()
        {
            var row = new DoorOpeningScheduleRow
            {
                ProjectId = "P1",
                DrawingFingerprint = "D1",
                Floor = "L1",
                Category = "Door",
                FamilyName = "D01",
                Material = "Wood",
                WidthM = 0.9d,
                HeightM = 2.2d,
                SillHeightM = 0d,
                ThicknessM = 0.1d,
                Count = 1,
                OpeningAreaM2 = 1.98d,
                HostCount = 0
            };
            row.ElementIds.Add("E1");
            row.SourceHandles.Add("1A");
            return row;
        }

        private static string TempPath(string suffix) => Path.Combine(Path.GetTempPath(), "qs3d-door-opening-count-" + suffix + "-" + Guid.NewGuid().ToString("N") + ".xlsx");
        private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

        private sealed class AdversarialRows : IReadOnlyList<DoorOpeningScheduleRow>, ICollection<DoorOpeningScheduleRow>
        {
            private readonly DoorOpeningScheduleRow _row;
            private readonly int[] _genericCounts;
            private int _genericCountReads;

            internal AdversarialRows(DoorOpeningScheduleRow row, params int[] genericCounts)
            {
                _row = row;
                _genericCounts = genericCounts.Length == 0 ? new[] { 1 } : genericCounts;
            }

            public int Count => 1;
            int ICollection<DoorOpeningScheduleRow>.Count
            {
                get
                {
                    var index = Math.Min(_genericCountReads, _genericCounts.Length - 1);
                    _genericCountReads++;
                    return _genericCounts[index];
                }
            }

            public int IndexerReads { get; private set; }
            public DoorOpeningScheduleRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    IndexerReads++;
                    return _row;
                }
            }

            bool ICollection<DoorOpeningScheduleRow>.IsReadOnly => true;
            void ICollection<DoorOpeningScheduleRow>.Add(DoorOpeningScheduleRow item) => throw new NotSupportedException();
            void ICollection<DoorOpeningScheduleRow>.Clear() => throw new NotSupportedException();
            bool ICollection<DoorOpeningScheduleRow>.Contains(DoorOpeningScheduleRow item) => ReferenceEquals(item, _row);
            void ICollection<DoorOpeningScheduleRow>.CopyTo(DoorOpeningScheduleRow[] array, int arrayIndex) => array[arrayIndex] = _row;
            bool ICollection<DoorOpeningScheduleRow>.Remove(DoorOpeningScheduleRow item) => throw new NotSupportedException();
            public IEnumerator<DoorOpeningScheduleRow> GetEnumerator() { yield return _row; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class DoorOpeningXlsxTransientCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DoorOpeningXlsxTransientCountStabilitySmoke.Run();
    }
}
