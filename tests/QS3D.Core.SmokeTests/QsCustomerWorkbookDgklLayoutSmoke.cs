using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCustomerWorkbookDgklLayoutSmoke
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        internal static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-dgkl-layout-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var detail1 = CreateRow("E-001", "A1", "Cột C1", 1.2d, 0.1d, 1.1d, 8.5d, 3.0d, 1.2d, 0d);
                var detail2 = CreateRow("E-002", "B2", "Cột C2", 1.4d, 0.2d, 1.2d, 9.5d, 3.2d, 1.2d, 0d);
                var summary = CreateSummary(detail1, detail2);

                QsCustomerWorkbookExporter.Export(path, new[] { detail1, detail2 }, new[] { summary });

                using (var archive = ZipFile.OpenRead(path))
                {
                    AssertTraceSheetHidden(archive);

                    var dgkl = LoadEntry(archive, "xl/worksheets/sheet1.xml");
                    AssertBusinessSheet(
                        dgkl,
                        new[] { "STT", "Tầng", "Loại", "Tên cấu kiện", "SL", "Mác BT", "BT gộp (m³)", "Trừ giao (m³)", "BT còn (m³)", "Dài (m)", "Chu vi ngoài (m)", "Chu vi trong (m)", QsCustomerWorkbookExporter.TraceHeader },
                        13,
                        "A1:L2");
                    Near(2.6d, NumberCell(dgkl, "G2"), "DGKL gross concrete");
                    Near(0.3d, NumberCell(dgkl, "H2"), "DGKL deduction");
                    Near(2.3d, NumberCell(dgkl, "I2"), "DGKL net concrete");
                    Near(6.2d, NumberCell(dgkl, "J2"), "DGKL length");

                    var formwork = LoadEntry(archive, "xl/worksheets/sheet2.xml");
                    AssertBusinessSheet(
                        formwork,
                        new[] { "STT", "Tầng", "Loại", "Tên cấu kiện", "SL", "CP gộp (m²)", "Trừ giao (m²)", "CP còn (m²)", QsCustomerWorkbookExporter.TraceHeader },
                        9,
                        "A1:H2");
                    Near(18d, NumberCell(formwork, "F2"), "COP_PHA gross formwork contract");
                    Near(0d, NumberCell(formwork, "G2"), "COP_PHA deduction contract");
                    Near(18d, NumberCell(formwork, "H2"), "COP_PHA net formwork contract");

                    var detail = LoadEntry(archive, "xl/worksheets/sheet3.xml");
                    AssertBusinessSheet(
                        detail,
                        new[] { "STT", "Nhóm", "Cấu kiện", "Tầng", "Dài", "Rộng", "Cao", "BT gộp", "Trừ giao", "BT còn", "VK", QsCustomerWorkbookExporter.TraceHeader },
                        12,
                        "A1:K3");
                    Near(3d, NumberCell(detail, "E2"), "CHI_TIET length evidence");
                    False(HasCell(detail, "F2"), "CHI_TIET width must stay blank without evidence");
                    False(HasCell(detail, "G2"), "CHI_TIET height must stay blank without evidence");
                    Near(1.2d, NumberCell(detail, "H2"), "CHI_TIET gross concrete");
                    Near(0.1d, NumberCell(detail, "I2"), "CHI_TIET deduction");
                    Near(1.1d, NumberCell(detail, "J2"), "CHI_TIET net concrete");
                    Near(8.5d, NumberCell(detail, "K2"), "CHI_TIET formwork");
                }

                var grouped = QsCustomerWorkbookTraceReader.Read(path, QsCustomerWorkbookExporter.DgklSheet, 2);
                Equal(2, grouped.ElementIds.Count, "DGKL grouped trace cardinality");
                SetEqual(new[] { "E-001", "E-002" }, grouped.ElementIds, "DGKL grouped semantic trace");
                SetEqual(new[] { "A1", "B2" }, grouped.Handles, "DGKL grouped CAD trace");

                var groupedFormwork = QsCustomerWorkbookTraceReader.Read(path, QsCustomerWorkbookExporter.FormworkSheet, 2);
                Equal(2, groupedFormwork.ElementIds.Count, "COP_PHA grouped trace cardinality");
                SetEqual(new[] { "E-001", "E-002" }, groupedFormwork.ElementIds, "COP_PHA grouped semantic trace");

                var exact = QsCustomerWorkbookTraceReader.Read(path, QsCustomerWorkbookExporter.DetailSheet, 2);
                Equal(1, exact.ElementIds.Count, "CHI_TIET trace cardinality");
                Equal("E-001", exact.ElementIds[0], "CHI_TIET exact semantic trace");
                Equal(1, exact.Handles.Count, "CHI_TIET CAD trace cardinality");
                Equal("A1", exact.Handles[0], "CHI_TIET exact CAD trace");
            }
            finally
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch
                {
                    // Cleanup must not hide the actual smoke assertion result.
                }
            }
        }

        private static QuantityReportRow CreateRow(
            string elementId,
            string handle,
            string elementName,
            double gross,
            double deduction,
            double net,
            double formwork,
            double length,
            double outerPerimeter,
            double innerPerimeter)
        {
            var row = new QuantityReportRow
            {
                Floor = "Tầng 1",
                Zone = "Z1",
                Category = "Cột",
                FamilyId = "COL-300",
                FamilyName = "Cột 300x300",
                ElementName = elementName,
                Material = "B30",
                DrawingFingerprint = "DGKL-SMOKE-FINGERPRINT",
                Count = 1,
                GrossConcreteM3 = gross,
                DeductionM3 = deduction,
                NetConcreteM3 = net,
                FormworkM2 = formwork,
                LengthM = length,
                OuterPerimeterM = outerPerimeter,
                InnerPerimeterM = innerPerimeter,
                HasGrossConcreteM3Evidence = true,
                HasDeductionM3Evidence = true,
                HasNetConcreteM3Evidence = true,
                HasFormworkM2Evidence = true,
                HasLengthMEvidence = true,
                HasOuterPerimeterMEvidence = true,
                HasInnerPerimeterMEvidence = true,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static QuantityReportRow CreateSummary(QuantityReportRow first, QuantityReportRow second)
        {
            var row = new QuantityReportRow
            {
                Floor = "Tầng 1",
                Zone = "Z1",
                Category = "Cột",
                FamilyId = "COL-300",
                FamilyName = "Cột 300x300",
                ElementName = "Cột 300x300",
                Material = "B30",
                DrawingFingerprint = first.DrawingFingerprint,
                Count = 2,
                GrossConcreteM3 = first.GrossConcreteM3 + second.GrossConcreteM3,
                DeductionM3 = first.DeductionM3 + second.DeductionM3,
                NetConcreteM3 = first.NetConcreteM3 + second.NetConcreteM3,
                FormworkM2 = first.FormworkM2 + second.FormworkM2,
                LengthM = first.LengthM + second.LengthM,
                OuterPerimeterM = first.OuterPerimeterM + second.OuterPerimeterM,
                InnerPerimeterM = first.InnerPerimeterM + second.InnerPerimeterM,
                HasGrossConcreteM3Evidence = true,
                HasDeductionM3Evidence = true,
                HasNetConcreteM3Evidence = true,
                HasFormworkM2Evidence = true,
                HasLengthMEvidence = true,
                HasOuterPerimeterMEvidence = true,
                HasInnerPerimeterMEvidence = true,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add(first.ElementIds[0]);
            row.ElementIds.Add(second.ElementIds[0]);
            row.SourceHandles.Add(first.SourceHandles[0]);
            row.SourceHandles.Add(second.SourceHandles[0]);
            return row;
        }

        private static void AssertTraceSheetHidden(ZipArchive archive)
        {
            var workbook = LoadEntry(archive, "xl/workbook.xml");
            var sheet = workbook.Descendants(SpreadsheetNs + "sheet")
                .SingleOrDefault(item => string.Equals((string)item.Attribute("name"), QsCustomerWorkbookExporter.TraceSheet, StringComparison.Ordinal));
            True(sheet != null, "TRACE_MODEL worksheet must exist");
            Equal("hidden", (string)sheet!.Attribute("state") ?? string.Empty, "TRACE_MODEL worksheet visibility");
        }

        private static void AssertBusinessSheet(XDocument document, IReadOnlyList<string> expectedHeaders, int hiddenTraceColumn, string expectedFilter)
        {
            var headerRow = document.Descendants(SpreadsheetNs + "row")
                .Single(item => string.Equals((string)item.Attribute("r"), "1", StringComparison.Ordinal));
            var actualHeaders = headerRow.Elements(SpreadsheetNs + "c").Select(ReadCellText).ToArray();
            Equal(expectedHeaders.Count, actualHeaders.Length, "business header count");
            for (var index = 0; index < expectedHeaders.Count; index++)
                Equal(expectedHeaders[index], actualHeaders[index], "business header at column " + (index + 1).ToString(CultureInfo.InvariantCulture));

            var hidden = document.Descendants(SpreadsheetNs + "col").Any(column =>
            {
                int min;
                int max;
                return int.TryParse((string)column.Attribute("min"), NumberStyles.Integer, CultureInfo.InvariantCulture, out min) &&
                       int.TryParse((string)column.Attribute("max"), NumberStyles.Integer, CultureInfo.InvariantCulture, out max) &&
                       min <= hiddenTraceColumn && hiddenTraceColumn <= max &&
                       string.Equals((string)column.Attribute("hidden"), "1", StringComparison.Ordinal);
            });
            True(hidden, "TRACE_KEY technical column must be hidden");

            var filter = document.Descendants(SpreadsheetNs + "autoFilter").SingleOrDefault();
            True(filter != null, "business sheet auto-filter must exist");
            Equal(expectedFilter, (string)filter!.Attribute("ref") ?? string.Empty, "auto-filter must exclude hidden TRACE_KEY");
        }

        private static XDocument LoadEntry(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path) ?? throw new InvalidOperationException("Missing XLSX entry: " + path);
            using (var stream = entry.Open()) return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }

        private static string ReadCellText(XElement cell)
        {
            return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(item => item.Value));
        }

        private static bool HasCell(XDocument document, string reference)
        {
            return document.Descendants(SpreadsheetNs + "c")
                .Any(cell => string.Equals((string)cell.Attribute("r"), reference, StringComparison.Ordinal));
        }

        private static double NumberCell(XDocument document, string reference)
        {
            var cell = document.Descendants(SpreadsheetNs + "c")
                .SingleOrDefault(item => string.Equals((string)item.Attribute("r"), reference, StringComparison.Ordinal));
            if (cell == null) throw new InvalidOperationException("Missing numeric cell " + reference + ".");
            var raw = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
            double value;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("Cell " + reference + " is not numeric: " + raw + ".");
            return value;
        }

        private static void SetEqual(IEnumerable<string> expected, IEnumerable<string> actual, string label)
        {
            var expectedSet = new HashSet<string>(expected, StringComparer.OrdinalIgnoreCase);
            var actualSet = new HashSet<string>(actual, StringComparer.OrdinalIgnoreCase);
            if (!expectedSet.SetEquals(actualSet))
                throw new InvalidOperationException(label + " mismatch. Expected [" + string.Join(",", expectedSet) + "] but got [" + string.Join(",", actualSet) + "].");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-9d)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected.ToString("R", CultureInfo.InvariantCulture) + " but got " + actual.ToString("R", CultureInfo.InvariantCulture) + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + " but got " + actual + ".");
        }

        private static void True(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label + ".");
        }

        private static void False(bool condition, string label)
        {
            if (condition) throw new InvalidOperationException(label + ".");
        }
    }
}
