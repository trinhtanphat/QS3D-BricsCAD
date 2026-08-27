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
            var componentOnlyPath = Path.Combine(Path.GetTempPath(), "qs3d-dgkl-component-only-" + Guid.NewGuid().ToString("N") + ".xlsx");
            var legacyNetOnlyPath = Path.Combine(Path.GetTempPath(), "qs3d-dgkl-legacy-net-formwork-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var detail1 = CreateRow("E-001", "A1", "Cột C1", 1.2d, 0.1d, 1.1d, 8.5d, 3.0d, 1.2d, 0d);
                detail1.GrossFormworkM2 = 9.2d;
                detail1.ConcreteContactDeductionM2 = 0.7d;
                detail1.NetFormworkM2 = 8.5d;
                detail1.HasGrossFormworkM2Evidence = true;
                detail1.HasConcreteContactDeductionM2Evidence = true;
                detail1.HasNetFormworkM2Evidence = true;
                detail1.WidthM = 0.3d;
                detail1.HeightM = 3.5d;
                detail1.HasWidthMEvidence = true;
                detail1.HasHeightMEvidence = true;

                var detail2 = CreateRow("E-002", "B2", "Cột C2", 1.4d, 0.2d, 1.2d, 9.5d, 3.2d, 1.2d, 0d);
                detail2.GrossFormworkM2 = 10.1d;
                detail2.ConcreteContactDeductionM2 = 0.6d;
                detail2.NetFormworkM2 = 9.5d;
                detail2.HasGrossFormworkM2Evidence = true;
                detail2.HasConcreteContactDeductionM2Evidence = true;
                detail2.HasNetFormworkM2Evidence = true;
                detail2.WidthM = 0.3d;
                detail2.HeightM = 3.5d;
                detail2.HasWidthMEvidence = true;
                detail2.HasHeightMEvidence = true;

                var summary = CreateSummary(detail1, detail2);
                QsCustomerWorkbookExporter.Export(path, new[] { detail1, detail2 }, new[] { summary });

                using (var archive = ZipFile.OpenRead(path))
                {
                    AssertTraceSheetHidden(archive);

                    var dgkl = LoadEntry(archive, "xl/worksheets/sheet1.xml");
                    AssertBusinessSheet(dgkl,
                        new[] { "STT", "Tầng", "Loại", "Tên cấu kiện", "SL", "Mác BT", "BT gộp (m³)", "Trừ giao (m³)", "BT còn (m³)", "Dài (m)", "Chu vi ngoài (m)", "Chu vi trong (m)", QsCustomerWorkbookExporter.TraceHeader },
                        13, "A1:L2");
                    Near(2.6d, NumberCell(dgkl, "G2"), "DGKL gross concrete");
                    Near(0.3d, NumberCell(dgkl, "H2"), "DGKL deduction");
                    Near(2.3d, NumberCell(dgkl, "I2"), "DGKL net concrete");
                    Near(6.2d, NumberCell(dgkl, "J2"), "DGKL length");

                    var formwork = LoadEntry(archive, "xl/worksheets/sheet2.xml");
                    AssertBusinessSheet(formwork,
                        new[] { "STT", "Tầng", "Loại", "Tên cấu kiện", "SL", "CP gộp (m²)", "Trừ giao (m²)", "CP còn (m²)", QsCustomerWorkbookExporter.TraceHeader },
                        9, "A1:H2");
                    Near(19.3d, NumberCell(formwork, "F2"), "COP_PHA explicit gross formwork evidence");
                    Near(1.3d, NumberCell(formwork, "G2"), "COP_PHA explicit formwork deduction evidence");
                    Near(18d, NumberCell(formwork, "H2"), "COP_PHA explicit net formwork evidence");

                    var detail = LoadEntry(archive, "xl/worksheets/sheet3.xml");
                    AssertBusinessSheet(detail,
                        new[] { "STT", "Nhóm", "Cấu kiện", "Tầng", "Dài", "Rộng", "Cao", "BT gộp", "Trừ giao", "BT còn", "VK", QsCustomerWorkbookExporter.TraceHeader },
                        12, "A1:K3");
                    Near(3d, NumberCell(detail, "E2"), "CHI_TIET length evidence");
                    Near(0.3d, NumberCell(detail, "F2"), "CHI_TIET width evidence");
                    Near(3.5d, NumberCell(detail, "G2"), "CHI_TIET height evidence");
                    Near(1.2d, NumberCell(detail, "H2"), "CHI_TIET gross concrete");
                    Near(0.1d, NumberCell(detail, "I2"), "CHI_TIET deduction");
                    Near(1.1d, NumberCell(detail, "J2"), "CHI_TIET net concrete");
                    Near(8.5d, NumberCell(detail, "K2"), "CHI_TIET net formwork");
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

                var legacyNetOnly = CreateRow("E-LEGACY", "D4", "Cột legacy", 0.9d, 0d, 0.9d, 7.25d, 2.8d, 1.0d, 0d);
                legacyNetOnly.HasGrossFormworkM2Evidence = false;
                legacyNetOnly.HasConcreteContactDeductionM2Evidence = false;
                legacyNetOnly.HasNetFormworkM2Evidence = false;
                QsCustomerWorkbookExporter.Export(legacyNetOnlyPath, new[] { legacyNetOnly }, new[] { legacyNetOnly });

                using (var archive = ZipFile.OpenRead(legacyNetOnlyPath))
                {
                    var legacyFormwork = LoadEntry(archive, "xl/worksheets/sheet2.xml");
                    False(HasCell(legacyFormwork, "F2"), "legacy net-only formwork must not fabricate CP gross");
                    False(HasCell(legacyFormwork, "G2"), "legacy net-only formwork must not fabricate deduction zero");
                    Near(7.25d, NumberCell(legacyFormwork, "H2"), "legacy FormworkM2 remains net formwork compatibility evidence");

                    var legacyDetail = LoadEntry(archive, "xl/worksheets/sheet3.xml");
                    Near(7.25d, NumberCell(legacyDetail, "K2"), "legacy net-only formwork remains visible in CHI_TIET VK");
                    False(HasCell(legacyDetail, "F2"), "CHI_TIET width stays blank without WidthM evidence");
                    False(HasCell(legacyDetail, "G2"), "CHI_TIET height stays blank without HeightM evidence");
                }

                var componentOnly = CreateRow("E-003", "C3", "Cột C3", 0.8d, 0d, 0.8d, 0d, 2.5d, 1.1d, 0d);
                componentOnly.HasFormworkM2Evidence = false;
                componentOnly.HasGrossFormworkM2Evidence = false;
                componentOnly.HasConcreteContactDeductionM2Evidence = false;
                componentOnly.HasNetFormworkM2Evidence = false;
                componentOnly.SideAreaM2 = 4.25d;
                componentOnly.HasSideAreaM2Evidence = true;
                QsCustomerWorkbookExporter.Export(componentOnlyPath, new[] { componentOnly }, new[] { componentOnly });

                using (var archive = ZipFile.OpenRead(componentOnlyPath))
                {
                    var componentOnlyFormwork = LoadEntry(archive, "xl/worksheets/sheet2.xml");
                    AssertBusinessSheet(componentOnlyFormwork,
                        new[] { "STT", "Tầng", "Loại", "Tên cấu kiện", "SL", "CP gộp (m²)", "Trừ giao (m²)", "CP còn (m²)", QsCustomerWorkbookExporter.TraceHeader },
                        9, "A1:H1");
                    Equal(1, componentOnlyFormwork.Descendants(SpreadsheetNs + "row").Count(), "COP_PHA must omit component-area-only business rows");
                    False(HasCell(componentOnlyFormwork, "F2"), "COP_PHA must not emit CP cells without formwork evidence");
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                    if (File.Exists(componentOnlyPath)) File.Delete(componentOnlyPath);
                    if (File.Exists(legacyNetOnlyPath)) File.Delete(legacyNetOnlyPath);
                }
                catch { }
            }
        }

        private static QuantityReportRow CreateRow(string elementId, string handle, string elementName, double gross, double deduction, double net, double formwork, double length, double outerPerimeter, double innerPerimeter)
        {
            var row = new QuantityReportRow
            {
                Floor = "Tầng 1", Zone = "Z1", Category = "Cột", FamilyId = "COL-300", FamilyName = "Cột 300x300", ElementName = elementName,
                Material = "B30", DrawingFingerprint = "DGKL-SMOKE-FINGERPRINT", Count = 1,
                GrossConcreteM3 = gross, DeductionM3 = deduction, NetConcreteM3 = net, FormworkM2 = formwork, LengthM = length,
                OuterPerimeterM = outerPerimeter, InnerPerimeterM = innerPerimeter,
                HasGrossConcreteM3Evidence = true, HasDeductionM3Evidence = true, HasNetConcreteM3Evidence = true, HasFormworkM2Evidence = true,
                HasGrossFormworkM2Evidence = false, HasConcreteContactDeductionM2Evidence = false, HasNetFormworkM2Evidence = false,
                HasLengthMEvidence = true, HasWidthMEvidence = false, HasHeightMEvidence = false,
                HasOuterPerimeterMEvidence = true, HasInnerPerimeterMEvidence = true,
                HasDoorAreaM2Evidence = false, HasSideAreaM2Evidence = false, HasBottomAreaM2Evidence = false, HasTopAreaM2Evidence = false, HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static QuantityReportRow CreateSummary(QuantityReportRow first, QuantityReportRow second)
        {
            var row = new QuantityReportRow
            {
                Floor = "Tầng 1", Zone = "Z1", Category = "Cột", FamilyId = "COL-300", FamilyName = "Cột 300x300", ElementName = "Cột 300x300",
                Material = "B30", DrawingFingerprint = first.DrawingFingerprint, Count = 2,
                GrossConcreteM3 = first.GrossConcreteM3 + second.GrossConcreteM3,
                DeductionM3 = first.DeductionM3 + second.DeductionM3,
                NetConcreteM3 = first.NetConcreteM3 + second.NetConcreteM3,
                FormworkM2 = first.FormworkM2 + second.FormworkM2,
                GrossFormworkM2 = first.GrossFormworkM2 + second.GrossFormworkM2,
                ConcreteContactDeductionM2 = first.ConcreteContactDeductionM2 + second.ConcreteContactDeductionM2,
                NetFormworkM2 = first.NetFormworkM2 + second.NetFormworkM2,
                LengthM = first.LengthM + second.LengthM,
                OuterPerimeterM = first.OuterPerimeterM + second.OuterPerimeterM,
                InnerPerimeterM = first.InnerPerimeterM + second.InnerPerimeterM,
                HasGrossConcreteM3Evidence = true, HasDeductionM3Evidence = true, HasNetConcreteM3Evidence = true, HasFormworkM2Evidence = true,
                HasGrossFormworkM2Evidence = first.HasGrossFormworkM2Evidence && second.HasGrossFormworkM2Evidence,
                HasConcreteContactDeductionM2Evidence = first.HasConcreteContactDeductionM2Evidence && second.HasConcreteContactDeductionM2Evidence,
                HasNetFormworkM2Evidence = first.HasNetFormworkM2Evidence && second.HasNetFormworkM2Evidence,
                HasLengthMEvidence = true, HasWidthMEvidence = false, HasHeightMEvidence = false,
                HasOuterPerimeterMEvidence = true, HasInnerPerimeterMEvidence = true,
                HasDoorAreaM2Evidence = false, HasSideAreaM2Evidence = false, HasBottomAreaM2Evidence = false, HasTopAreaM2Evidence = false, HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add(first.ElementIds[0]); row.ElementIds.Add(second.ElementIds[0]);
            row.SourceHandles.Add(first.SourceHandles[0]); row.SourceHandles.Add(second.SourceHandles[0]);
            return row;
        }

        private static void AssertTraceSheetHidden(ZipArchive archive)
        {
            var workbook = LoadEntry(archive, "xl/workbook.xml");
            var sheet = workbook.Descendants(SpreadsheetNs + "sheet").SingleOrDefault(item => string.Equals((string?)item.Attribute("name"), QsCustomerWorkbookExporter.TraceSheet, StringComparison.Ordinal));
            True(sheet != null, "TRACE_MODEL worksheet must exist");
            Equal("hidden", (string?)sheet!.Attribute("state") ?? string.Empty, "TRACE_MODEL worksheet visibility");
        }

        private static void AssertBusinessSheet(XDocument document, IReadOnlyList<string> expectedHeaders, int hiddenTraceColumn, string expectedFilter)
        {
            var headerRow = document.Descendants(SpreadsheetNs + "row").Single(item => string.Equals((string?)item.Attribute("r"), "1", StringComparison.Ordinal));
            var actualHeaders = headerRow.Elements(SpreadsheetNs + "c").Select(ReadCellText).ToArray();
            Equal(expectedHeaders.Count, actualHeaders.Length, "business header count");
            for (var index = 0; index < expectedHeaders.Count; index++) Equal(expectedHeaders[index], actualHeaders[index], "business header at column " + (index + 1).ToString(CultureInfo.InvariantCulture));
            var hidden = document.Descendants(SpreadsheetNs + "col").Any(column =>
            {
                int min; int max;
                return int.TryParse((string?)column.Attribute("min"), NumberStyles.Integer, CultureInfo.InvariantCulture, out min) &&
                       int.TryParse((string?)column.Attribute("max"), NumberStyles.Integer, CultureInfo.InvariantCulture, out max) &&
                       min <= hiddenTraceColumn && hiddenTraceColumn <= max && string.Equals((string?)column.Attribute("hidden"), "1", StringComparison.Ordinal);
            });
            True(hidden, "TRACE_KEY technical column must be hidden");
            var filter = document.Descendants(SpreadsheetNs + "autoFilter").SingleOrDefault();
            True(filter != null, "business sheet auto-filter must exist");
            Equal(expectedFilter, (string?)filter!.Attribute("ref") ?? string.Empty, "auto-filter must exclude hidden TRACE_KEY");
        }

        private static XDocument LoadEntry(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path) ?? throw new InvalidOperationException("Missing XLSX entry: " + path);
            using (var stream = entry.Open()) return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }

        private static string ReadCellText(XElement cell) => string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(item => item.Value));
        private static bool HasCell(XDocument document, string reference) => document.Descendants(SpreadsheetNs + "c").Any(cell => string.Equals((string?)cell.Attribute("r"), reference, StringComparison.Ordinal));

        private static double NumberCell(XDocument document, string reference)
        {
            var cell = document.Descendants(SpreadsheetNs + "c").SingleOrDefault(item => string.Equals((string?)item.Attribute("r"), reference, StringComparison.Ordinal));
            if (cell == null) throw new InvalidOperationException("Missing numeric cell " + reference + ".");
            var raw = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
            double value;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) throw new InvalidOperationException("Cell " + reference + " is not numeric: " + raw + ".");
            return value;
        }

        private static void SetEqual(IEnumerable<string> expected, IEnumerable<string> actual, string label)
        {
            var expectedSet = new HashSet<string>(expected, StringComparer.OrdinalIgnoreCase);
            var actualSet = new HashSet<string>(actual, StringComparer.OrdinalIgnoreCase);
            if (!expectedSet.SetEquals(actualSet)) throw new InvalidOperationException(label + " mismatch. Expected [" + string.Join(",", expectedSet) + "] but got [" + string.Join(",", actualSet) + "].");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-9d) throw new InvalidOperationException(label + " mismatch. Expected " + expected.ToString("R", CultureInfo.InvariantCulture) + " but got " + actual.ToString("R", CultureInfo.InvariantCulture) + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + " mismatch. Expected " + expected + " but got " + actual + ".");
        }

        private static void True(bool condition, string label) { if (!condition) throw new InvalidOperationException(label + "."); }
        private static void False(bool condition, string label) { if (condition) throw new InvalidOperationException(label + "."); }
    }
}
