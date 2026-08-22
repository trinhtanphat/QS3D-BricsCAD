using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class Ed2QuantityXlsxRowSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportReadsEachCallerRowOnceBeforeValidationAndIo();
        }

        private static void ExportReadsEachCallerRowOnceBeforeValidationAndIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-ed2-xlsx-row-snapshot-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "quantity-ed2.xlsx");
            var detail = Row();
            detail.ElementIds.Add("E-1");
            detail.SourceHandles.Add("1A");
            var summary = Row();
            summary.ElementIds.Add("E-1");
            summary.SourceHandles.Add("1A");
            var detailRows = new SingleReadRowList(detail);
            var summaryRows = new SingleReadRowList(summary);

            try
            {
                XlsxQuantityExporter.ExportEd2(path, detailRows, summaryRows);
                if (!File.Exists(path))
                    throw new Exception("ED2 XLSX export must succeed from detached validated CHI_TIET/TONG_HOP snapshots.");
                if (detailRows.IndexReadCount != 1 || summaryRows.IndexReadCount != 1)
                    throw new Exception("ED2 XLSX export must read each caller-owned detail/summary row index exactly once.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static QuantityReportRow Row()
        {
            return new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "Wall",
                FamilyId = "F-1",
                FamilyName = "W200",
                ElementName = "Wall 1",
                Material = "Concrete",
                Note = "",
                DrawingFingerprint = "FP-1",
                Count = 1,
                GrossConcreteM3 = 2d,
                DeductionM3 = 0.25d,
                NetConcreteM3 = 1.75d,
                FormworkM2 = 8d,
                LengthM = 4d,
                OuterPerimeterM = 10d,
                InnerPerimeterM = 0d,
                DoorAreaM2 = 0d,
                SideAreaM2 = 8d,
                BottomAreaM2 = 0d,
                TopAreaM2 = 0d,
                OtherAreaM2 = 0d,
                DensityKgM3 = 2400d,
                MassKg = 4200d
            };
        }

        private sealed class SingleReadRowList : IReadOnlyList<QuantityReportRow>
        {
            private readonly QuantityReportRow _row;

            public SingleReadRowList(QuantityReportRow row) => _row = row;

            public int Count => 1;
            public int IndexReadCount { get; private set; }

            public QuantityReportRow this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    IndexReadCount++;
                    if (IndexReadCount > 1)
                        throw new InvalidOperationException("Caller-owned ED2 row index was read again after snapshotting.");
                    return _row;
                }
            }

            public IEnumerator<QuantityReportRow> GetEnumerator() =>
                throw new InvalidOperationException("ED2 XLSX exporter must not enumerate caller-owned row lists.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
