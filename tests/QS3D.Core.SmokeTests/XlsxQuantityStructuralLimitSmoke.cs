using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityStructuralLimitSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsOversizedStandardWorksheetBeforeIndexingOrFilesystemMutation();
            RejectsOversizedEd2WorksheetBeforeIndexingOrFilesystemMutation();
            RejectsOversizedEd2SummaryWorksheetBeforeIndexingOrFilesystemMutation();
            AcceptsExactCellTextLimit();
            RejectsOversizedStandardScalarCellBeforeFilesystemMutation();
            RejectsOversizedStandardJoinedCellBeforeFilesystemMutation();
            RejectsOversizedEd2DisplayCellBeforeFilesystemMutation();
            RejectsOversizedEd2FloorZoneCellBeforeFilesystemMutation();
        }

        private static void RejectsOversizedStandardWorksheetBeforeIndexingOrFilesystemMutation()
        {
            var root = Root("row-limit");
            var path = Path.Combine(root, "quantity.xlsx");
            try
            {
                Throws<ArgumentOutOfRangeException>(() => XlsxQuantityExporter.Export(path, new OversizedRows()));
                RequireNoDestinationMutation(root, path, "Oversized Quantity XLSX worksheet");
            }
            finally { Delete(root); }
        }

        private static void RejectsOversizedEd2WorksheetBeforeIndexingOrFilesystemMutation()
        {
            var root = Root("ed2-row-limit");
            var path = Path.Combine(root, "quantity-ed2.xlsx");
            try
            {
                Throws<ArgumentOutOfRangeException>(() => XlsxQuantityExporter.ExportEd2(path, new OversizedRows(), new[] { new QuantityReportRow() }));
                RequireNoDestinationMutation(root, path, "Oversized ED2 CHI_TIET worksheet");
            }
            finally { Delete(root); }
        }

        private static void RejectsOversizedEd2SummaryWorksheetBeforeIndexingOrFilesystemMutation()
        {
            var root = Root("ed2-summary-row-limit");
            var path = Path.Combine(root, "quantity-ed2.xlsx");
            try
            {
                Throws<ArgumentOutOfRangeException>(() => XlsxQuantityExporter.ExportEd2(path, new[] { new QuantityReportRow() }, new OversizedRows()));
                RequireNoDestinationMutation(root, path, "Oversized ED2 TONG_HOP worksheet");
            }
            finally { Delete(root); }
        }

        private static void AcceptsExactCellTextLimit()
        {
            var root = Root("cell-ok");
            var path = Path.Combine(root, "quantity.xlsx");
            try
            {
                var row = ValidStandardRow("E1", "1");
                row.FamilyName = new string('A', 32767);
                XlsxQuantityExporter.Export(path, new[] { row });
                if (!File.Exists(path)) throw new Exception("Quantity XLSX must accept exactly 32,767 text characters.");
            }
            finally { Delete(root); }
        }

        private static void RejectsOversizedStandardScalarCellBeforeFilesystemMutation()
        {
            var root = Root("cell-reject");
            var path = Path.Combine(root, "quantity.xlsx");
            try
            {
                var row = ValidStandardRow("E1", "1");
                row.FamilyName = new string('B', 32768);
                Throws<ArgumentOutOfRangeException>(() => XlsxQuantityExporter.Export(path, new[] { row }));
                RequireNoDestinationMutation(root, path, "Oversized Quantity XLSX scalar text");
            }
            finally { Delete(root); }
        }

        private static void RejectsOversizedStandardJoinedCellBeforeFilesystemMutation()
        {
            var root = Root("joined-reject");
            var path = Path.Combine(root, "quantity.xlsx");
            try
            {
                var row = ValidStandardRow(new string('C', 16384), "1");
                row.ElementIds.Add(new string('D', 16383));
                row.SourceHandles.Add("2");
                row.Count = 2;
                Throws<ArgumentOutOfRangeException>(() => XlsxQuantityExporter.Export(path, new[] { row }));
                RequireNoDestinationMutation(root, path, "Oversized Quantity XLSX joined ElementIds text");
            }
            finally { Delete(root); }
        }

        private static void RejectsOversizedEd2DisplayCellBeforeFilesystemMutation()
        {
            var root = Root("ed2-cell-reject");
            var path = Path.Combine(root, "quantity-ed2.xlsx");
            try
            {
                var detail = ValidEd2Row("E1");
                detail.ElementName = new string('E', 32768);
                var summary = ValidEd2Row("E1");
                Throws<ArgumentOutOfRangeException>(() => XlsxQuantityExporter.ExportEd2(path, new[] { detail }, new[] { summary }));
                RequireNoDestinationMutation(root, path, "Oversized ED2 display text");
            }
            finally { Delete(root); }
        }

        private static void RejectsOversizedEd2FloorZoneCellBeforeFilesystemMutation()
        {
            var root = Root("ed2-floor-zone-reject");
            var path = Path.Combine(root, "quantity-ed2.xlsx");
            try
            {
                var detail = ValidEd2Row("E1");
                detail.Floor = new string('F', 16383);
                detail.Zone = new string('Z', 16382);
                var summary = ValidEd2Row("E1");
                Throws<ArgumentOutOfRangeException>(() => XlsxQuantityExporter.ExportEd2(path, new[] { detail }, new[] { summary }));
                RequireNoDestinationMutation(root, path, "Oversized ED2 FloorZone text");
            }
            finally { Delete(root); }
        }

        private static QuantityReportRow ValidStandardRow(string elementId, string handle)
        {
            var row = new QuantityReportRow
            {
                DrawingFingerprint = "DRAWING-1",
                Count = 1
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static QuantityReportRow ValidEd2Row(string elementId)
        {
            var row = new QuantityReportRow
            {
                Floor = "F1",
                Category = "Wall",
                FamilyId = "FAM-1",
                FamilyName = "Wall 200",
                Material = "Concrete",
                DrawingFingerprint = "DRAWING-1",
                Count = 1
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add("1");
            return row;
        }

        private static string Root(string suffix) =>
            Path.Combine(Path.GetTempPath(), "qs3d-quantity-xlsx-structural-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void RequireNoDestinationMutation(string root, string path, string label)
        {
            if (Directory.Exists(root) || File.Exists(path))
                throw new Exception(label + " must fail before destination filesystem mutation.");
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

        private sealed class OversizedRows : IReadOnlyList<QuantityReportRow>
        {
            public int Count => 1048576;
            public QuantityReportRow this[int index] => throw new Exception("Worksheet row limit must be checked before indexing oversized input.");
            public IEnumerator<QuantityReportRow> GetEnumerator() => throw new Exception("Worksheet row limit must be checked before enumerating oversized input.");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
