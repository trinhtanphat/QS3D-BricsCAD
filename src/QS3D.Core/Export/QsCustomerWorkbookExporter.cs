using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    public static class QsCustomerWorkbookExporter
    {
        public const string DgklSheet = "DGKL";
        public const string FormworkSheet = "COP_PHA";
        public const string DetailSheet = "CHI_TIET";
        public const string TraceSheet = "TRACE_MODEL";
        public const string TraceHeader = "TRACE_KEY";

        private const int HeaderStyle = 1;
        private const int IntegerStyle = 2;
        private const int DecimalStyle = 3;
        private const int WrappedStyle = 4;
        private const int MaxRows = 1048575;

        public static void Export(
            string path,
            IReadOnlyList<QuantityReportRow> detailRows,
            IReadOnlyList<QuantityReportRow> summaryRows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (detailRows == null) throw new ArgumentNullException(nameof(detailRows));
            if (summaryRows == null) throw new ArgumentNullException(nameof(summaryRows));

            var detailCount = BindSourceCount(detailRows, DetailSheet);
            var summaryCount = BindSourceCount(summaryRows, DgklSheet);
            var details = Snapshot(detailRows, detailCount, true, DetailSheet);
            var summaries = Snapshot(summaryRows, summaryCount, false, DgklSheet);
            ValidateScope(details, summaries);

            var formwork = summaries.Where(HasAnyFormworkEvidence).ToList();
            if ((long)details.Count + summaries.Count + formwork.Count > MaxRows)
                throw new InvalidDataException("Customer workbook TRACE_MODEL exceeds the Excel row limit.");

            var traces = new List<TraceProjection>();
            var dgklXml = BuildDgklSheet(summaries, traces);
            var formworkXml = BuildFormworkSheet(formwork, traces);
            var detailXml = BuildDetailSheet(details, traces);
            var traceXml = BuildTraceSheet(traces);

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
                {
                    WriteEntry(archive, "[Content_Types].xml", ContentTypesXml);
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml);
                    WriteEntry(archive, "xl/workbook.xml", WorkbookXml);
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml);
                    WriteEntry(archive, "xl/styles.xml", StylesXml);
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", dgklXml);
                    WriteEntry(archive, "xl/worksheets/sheet2.xml", formworkXml);
                    WriteEntry(archive, "xl/worksheets/sheet3.xml", detailXml);
                    WriteEntry(archive, "xl/worksheets/sheet4.xml", traceXml);
                }

                XlsxPackageValidator.Validate(
                    tempPath,
                    "[Content_Types].xml",
                    "xl/workbook.xml",
                    "xl/styles.xml",
                    "xl/worksheets/sheet1.xml",
                    "xl/worksheets/sheet2.xml",
                    "xl/worksheets/sheet3.xml",
                    "xl/worksheets/sheet4.xml");
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static int BindSourceCount(IReadOnlyList<QuantityReportRow> source, string sheet)
        {
            var count = source.Count;
            if (count <= 0)
                throw new InvalidDataException("Customer workbook " + sheet + " requires at least one row.");
            if (count > MaxRows)
                throw new InvalidDataException("Customer workbook " + sheet + " exceeds the Excel row limit.");
            return count;
        }

        private static bool HasAnyFormworkEvidence(QuantityReportRow row) =>
            row.HasGrossFormworkM2Evidence || row.HasConcreteContactDeductionM2Evidence || row.HasNetFormworkM2Evidence;

        private static List<QuantityReportRow> Snapshot(
            IReadOnlyList<QuantityReportRow> source,
            int admittedCount,
            bool requireSingle,
            string sheet)
        {
            var result = new List<QuantityReportRow>(admittedCount);
            string? fingerprint = null;
            for (var index = 0; index < admittedCount; index++)
            {
                var row = source[index] ?? throw new InvalidDataException("Customer workbook contains a null quantity row.");
                if (row.Count <= 0) throw new InvalidDataException("Customer workbook row Count must be positive.");
                if (requireSingle && (row.Count != 1 || row.ElementIds.Count != 1))
                    throw new InvalidDataException("Customer workbook CHI_TIET must contain exactly one semantic element per row.");

                var hasNetFormworkEvidence = row.HasNetFormworkM2Evidence || row.HasFormworkM2Evidence;
                var netFormwork = row.HasNetFormworkM2Evidence
                    ? Checked(row.NetFormworkM2, true, "NetFormworkM2")
                    : Checked(row.FormworkM2, row.HasFormworkM2Evidence, "FormworkM2");

                var copy = new QuantityReportRow
                {
                    Floor = row.Floor ?? string.Empty,
                    Zone = row.Zone ?? string.Empty,
                    Category = row.Category ?? string.Empty,
                    FamilyId = row.FamilyId ?? string.Empty,
                    FamilyName = row.FamilyName ?? string.Empty,
                    ElementName = row.ElementName ?? string.Empty,
                    Material = row.Material ?? string.Empty,
                    Note = row.Note ?? string.Empty,
                    DrawingFingerprint = Required(row.DrawingFingerprint, "Drawing Fingerprint"),
                    Count = row.Count,
                    GrossConcreteM3 = Checked(row.GrossConcreteM3, row.HasGrossConcreteM3Evidence, "GrossConcreteM3"),
                    DeductionM3 = Checked(row.DeductionM3, row.HasDeductionM3Evidence, "DeductionM3"),
                    NetConcreteM3 = Checked(row.NetConcreteM3, row.HasNetConcreteM3Evidence, "NetConcreteM3"),
                    GrossFormworkM2 = Checked(row.GrossFormworkM2, row.HasGrossFormworkM2Evidence, "GrossFormworkM2"),
                    ConcreteContactDeductionM2 = Checked(row.ConcreteContactDeductionM2, row.HasConcreteContactDeductionM2Evidence, "ConcreteContactDeductionM2"),
                    NetFormworkM2 = netFormwork,
                    FormworkM2 = netFormwork,
                    LengthM = Checked(row.LengthM, row.HasLengthMEvidence, "LengthM"),
                    WidthM = Checked(row.WidthM, row.HasWidthMEvidence, "WidthM"),
                    HeightM = Checked(row.HeightM, row.HasHeightMEvidence, "HeightM"),
                    OuterPerimeterM = Checked(row.OuterPerimeterM, row.HasOuterPerimeterMEvidence, "OuterPerimeterM"),
                    InnerPerimeterM = Checked(row.InnerPerimeterM, row.HasInnerPerimeterMEvidence, "InnerPerimeterM"),
                    DoorAreaM2 = Checked(row.DoorAreaM2, row.HasDoorAreaM2Evidence, "DoorAreaM2"),
                    SideAreaM2 = Checked(row.SideAreaM2, row.HasSideAreaM2Evidence, "SideAreaM2"),
                    BottomAreaM2 = Checked(row.BottomAreaM2, row.HasBottomAreaM2Evidence, "BottomAreaM2"),
                    TopAreaM2 = Checked(row.TopAreaM2, row.HasTopAreaM2Evidence, "TopAreaM2"),
                    OtherAreaM2 = Checked(row.OtherAreaM2, row.HasOtherAreaM2Evidence, "OtherAreaM2"),
                    HasGrossConcreteM3Evidence = row.HasGrossConcreteM3Evidence,
                    HasDeductionM3Evidence = row.HasDeductionM3Evidence,
                    HasNetConcreteM3Evidence = row.HasNetConcreteM3Evidence,
                    HasGrossFormworkM2Evidence = row.HasGrossFormworkM2Evidence,
                    HasConcreteContactDeductionM2Evidence = row.HasConcreteContactDeductionM2Evidence,
                    HasNetFormworkM2Evidence = hasNetFormworkEvidence,
                    HasFormworkM2Evidence = hasNetFormworkEvidence,
                    HasLengthMEvidence = row.HasLengthMEvidence,
                    HasWidthMEvidence = row.HasWidthMEvidence,
                    HasHeightMEvidence = row.HasHeightMEvidence,
                    HasOuterPerimeterMEvidence = row.HasOuterPerimeterMEvidence,
                    HasInnerPerimeterMEvidence = row.HasInnerPerimeterMEvidence,
                    HasDoorAreaM2Evidence = row.HasDoorAreaM2Evidence,
                    HasSideAreaM2Evidence = row.HasSideAreaM2Evidence,
                    HasBottomAreaM2Evidence = row.HasBottomAreaM2Evidence,
                    HasTopAreaM2Evidence = row.HasTopAreaM2Evidence,
                    HasOtherAreaM2Evidence = row.HasOtherAreaM2Evidence,
                    DensityKgM3 = CheckedNullable(row.DensityKgM3, "DensityKgM3", true),
                    MassKg = CheckedNullable(row.MassKg, "MassKg", false)
                };

                foreach (var id in row.ElementIds)
                {
                    var canonical = Required(id, "QS3D Element ID");
                    if (copy.ElementIds.Any(existing => string.Equals(existing, canonical, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException("Customer workbook row contains duplicate Element ID: " + canonical + ".");
                    copy.ElementIds.Add(canonical);
                }
                if (copy.ElementIds.Count == 0) throw new InvalidDataException("Customer workbook row has no Element ID provenance.");
                if (copy.ElementIds.Count != row.Count)
                    throw new InvalidDataException("Customer workbook row Count must equal its QS3D Element ID provenance cardinality.");

                foreach (var handle in row.SourceHandles)
                {
                    var canonical = CanonicalHandle(handle);
                    if (!copy.SourceHandles.Any(existing => string.Equals(existing, canonical, StringComparison.OrdinalIgnoreCase)))
                        copy.SourceHandles.Add(canonical);
                }
                if (copy.SourceHandles.Count == 0) throw new InvalidDataException("Customer workbook row has no CAD Handle provenance.");

                if (fingerprint == null) fingerprint = copy.DrawingFingerprint;
                else if (!string.Equals(fingerprint, copy.DrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Customer workbook rows contain conflicting drawing fingerprints.");
                result.Add(copy);
            }
            if (source.Count != admittedCount)
                throw new InvalidDataException("Customer workbook " + sheet + " row Count changed during snapshot traversal.");
            return result;
        }

        private static void ValidateScope(IReadOnlyList<QuantityReportRow> details, IReadOnlyList<QuantityReportRow> summaries)
        {
            var detailIds = new HashSet<string>(details.SelectMany(row => row.ElementIds), StringComparer.OrdinalIgnoreCase);
            var detailHandles = new HashSet<string>(details.SelectMany(row => row.SourceHandles), StringComparer.OrdinalIgnoreCase);
            var summaryIds = new HashSet<string>(summaries.SelectMany(row => row.ElementIds), StringComparer.OrdinalIgnoreCase);
            var summaryHandles = new HashSet<string>(summaries.SelectMany(row => row.SourceHandles), StringComparer.OrdinalIgnoreCase);
            var count = summaries.Sum(row => row.Count);
            if (detailIds.Count != details.Count || !summaryIds.SetEquals(detailIds) || !summaryHandles.SetEquals(detailHandles) || count != details.Count)
                throw new InvalidDataException("Customer workbook detail and grouped rows do not describe the same semantic scope.");

            var handlesByElementId = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var detail in details)
            {
                if (detail.ElementIds.Count != 1)
                    throw new InvalidDataException("Customer workbook CHI_TIET must contain exactly one semantic element per row.");
                var elementId = detail.ElementIds[0];
                if (handlesByElementId.ContainsKey(elementId))
                    throw new InvalidDataException("Customer workbook CHI_TIET contains duplicate semantic Element ID: " + elementId + ".");
                handlesByElementId.Add(elementId, new HashSet<string>(detail.SourceHandles, StringComparer.OrdinalIgnoreCase));
            }

            foreach (var summary in summaries)
            {
                var expectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var elementId in summary.ElementIds)
                {
                    HashSet<string> detailRowHandles;
                    if (!handlesByElementId.TryGetValue(elementId, out detailRowHandles))
                        throw new InvalidDataException("Customer workbook grouped row references an unknown QS3D Element ID: " + elementId + ".");
                    expectedHandles.UnionWith(detailRowHandles);
                }

                var actualHandles = new HashSet<string>(summary.SourceHandles, StringComparer.OrdinalIgnoreCase);
                if (!actualHandles.SetEquals(expectedHandles))
                    throw new InvalidDataException("Customer workbook grouped row CAD Handle provenance does not match its QS3D Element IDs.");
            }
        }

        private static string BuildDgklSheet(IReadOnlyList<QuantityReportRow> rows, ICollection<TraceProjection> traces)
        {
            var headers = new[] { "STT", "Tầng", "Loại", "Tên cấu kiện", "SL", "Mác BT", "BT gộp (m³)", "Trừ giao (m³)", "BT còn (m³)", "Dài (m)", "Chu vi ngoài (m)", "Chu vi trong (m)", TraceHeader };
            return BuildBusinessSheet(DgklSheet, headers, 12, rows, traces, (sb, row, excelRow, index, traceKey) =>
            {
                Number(sb, Cell(0, excelRow), index + 1, IntegerStyle);
                Text(sb, Cell(1, excelRow), FloorText(row), 0);
                Text(sb, Cell(2, excelRow), row.Category, 0);
                Text(sb, Cell(3, excelRow), DisplayName(row), 0);
                Number(sb, Cell(4, excelRow), row.Count, IntegerStyle);
                Text(sb, Cell(5, excelRow), row.Material, 0);
                Evidence(sb, Cell(6, excelRow), row.GrossConcreteM3, row.HasGrossConcreteM3Evidence);
                Evidence(sb, Cell(7, excelRow), row.DeductionM3, row.HasDeductionM3Evidence);
                Evidence(sb, Cell(8, excelRow), row.NetConcreteM3, row.HasNetConcreteM3Evidence);
                Evidence(sb, Cell(9, excelRow), row.LengthM, row.HasLengthMEvidence);
                Evidence(sb, Cell(10, excelRow), row.OuterPerimeterM, row.HasOuterPerimeterMEvidence);
                Evidence(sb, Cell(11, excelRow), row.InnerPerimeterM, row.HasInnerPerimeterMEvidence);
                Text(sb, Cell(12, excelRow), traceKey, WrappedStyle);
            });
        }

        private static string BuildFormworkSheet(IReadOnlyList<QuantityReportRow> rows, ICollection<TraceProjection> traces)
        {
            var headers = new[] { "STT", "Tầng", "Loại", "Tên cấu kiện", "SL", "CP gộp (m²)", "Trừ giao (m²)", "CP còn (m²)", TraceHeader };
            return BuildBusinessSheet(FormworkSheet, headers, 8, rows, traces, (sb, row, excelRow, index, traceKey) =>
            {
                Number(sb, Cell(0, excelRow), index + 1, IntegerStyle);
                Text(sb, Cell(1, excelRow), FloorText(row), 0);
                Text(sb, Cell(2, excelRow), row.Category, 0);
                Text(sb, Cell(3, excelRow), DisplayName(row), 0);
                Number(sb, Cell(4, excelRow), row.Count, IntegerStyle);
                Evidence(sb, Cell(5, excelRow), row.GrossFormworkM2, row.HasGrossFormworkM2Evidence);
                Evidence(sb, Cell(6, excelRow), row.ConcreteContactDeductionM2, row.HasConcreteContactDeductionM2Evidence);
                Evidence(sb, Cell(7, excelRow), row.NetFormworkM2, row.HasNetFormworkM2Evidence);
                Text(sb, Cell(8, excelRow), traceKey, WrappedStyle);
            });
        }

        private static string BuildDetailSheet(IReadOnlyList<QuantityReportRow> rows, ICollection<TraceProjection> traces)
        {
            var headers = new[] { "STT", "Nhóm", "Cấu kiện", "Tầng", "Dài", "Rộng", "Cao", "BT gộp", "Trừ giao", "BT còn", "VK", TraceHeader };
            return BuildBusinessSheet(DetailSheet, headers, 11, rows, traces, (sb, row, excelRow, index, traceKey) =>
            {
                Number(sb, Cell(0, excelRow), index + 1, IntegerStyle);
                Text(sb, Cell(1, excelRow), GroupName(row), 0);
                Text(sb, Cell(2, excelRow), DisplayName(row), 0);
                Text(sb, Cell(3, excelRow), FloorText(row), 0);
                Evidence(sb, Cell(4, excelRow), row.LengthM, row.HasLengthMEvidence);
                Evidence(sb, Cell(5, excelRow), row.WidthM, row.HasWidthMEvidence);
                Evidence(sb, Cell(6, excelRow), row.HeightM, row.HasHeightMEvidence);
                Evidence(sb, Cell(7, excelRow), row.GrossConcreteM3, row.HasGrossConcreteM3Evidence);
                Evidence(sb, Cell(8, excelRow), row.DeductionM3, row.HasDeductionM3Evidence);
                Evidence(sb, Cell(9, excelRow), row.NetConcreteM3, row.HasNetConcreteM3Evidence);
                Evidence(sb, Cell(10, excelRow), row.NetFormworkM2, row.HasNetFormworkM2Evidence);
                Text(sb, Cell(11, excelRow), traceKey, WrappedStyle);
            });
        }

        private delegate void BusinessRowWriter(StringBuilder sb, QuantityReportRow row, int excelRow, int index, string traceKey);

        private static string BuildBusinessSheet(
            string sheetName,
            string[] headers,
            int visibleColumnCount,
            IReadOnlyList<QuantityReportRow> rows,
            ICollection<TraceProjection> traces,
            BusinessRowWriter writeRow)
        {
            if (headers.Length != visibleColumnCount + 1 || !string.Equals(headers[headers.Length - 1], TraceHeader, StringComparison.Ordinal))
                throw new InvalidDataException("Customer workbook business sheet must end with one hidden TRACE_KEY column.");

            var lastRow = Math.Max(1, rows.Count + 1);
            var lastColumn = ColumnName(headers.Length - 1);
            var range = "A1:" + lastColumn + lastRow.ToString(CultureInfo.InvariantCulture);
            var visibleRange = "A1:" + ColumnName(visibleColumnCount - 1) + lastRow.ToString(CultureInfo.InvariantCulture);
            var sb = BeginSheet(range);
            AppendBusinessColumns(sb, visibleColumnCount);
            sb.Append("<sheetData><row r=\"1\" ht=\"30\" customHeight=\"1\">");
            for (var column = 0; column < headers.Length; column++) Text(sb, Cell(column, 1), headers[column], HeaderStyle);
            sb.Append("</row>");

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = index + 2;
                var traceKey = BuildTraceKey(sheetName, row);
                traces.Add(new TraceProjection(traceKey, sheetName, excelRow, row.ElementIds, row.SourceHandles, row.DrawingFingerprint));
                sb.Append("<row r=\"").Append(excelRow).Append("\">");
                writeRow(sb, row, excelRow, index, traceKey);
                sb.Append("</row>");
            }

            sb.Append("</sheetData><autoFilter ref=\"").Append(visibleRange).Append("\"/></worksheet>");
            return sb.ToString();
        }

        private static void AppendBusinessColumns(StringBuilder sb, int visibleColumnCount)
        {
            sb.Append("<cols><col min=\"1\" max=\"1\" width=\"7\" customWidth=\"1\"/>");
            if (visibleColumnCount > 1)
                sb.Append("<col min=\"2\" max=\"").Append(visibleColumnCount).Append("\" width=\"18\" customWidth=\"1\"/>");
            var traceColumn = visibleColumnCount + 1;
            sb.Append("<col min=\"").Append(traceColumn).Append("\" max=\"").Append(traceColumn)
              .Append("\" width=\"2\" hidden=\"1\" customWidth=\"1\"/></cols>");
        }

        private static string DisplayName(QuantityReportRow row)
        {
            return string.IsNullOrWhiteSpace(row.ElementName) ? row.FamilyName : row.ElementName;
        }

        private static string GroupName(QuantityReportRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.FamilyName)) return row.FamilyName;
            if (!string.IsNullOrWhiteSpace(row.FamilyId)) return row.FamilyId;
            return row.Category;
        }

        private static string FloorText(QuantityReportRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.Floor)) return row.Floor;
            return row.Zone ?? string.Empty;
        }

        private static string BuildTraceSheet(IReadOnlyList<TraceProjection> traces)
        {
            var headers = new[] { TraceHeader, "SHEET", "ROW", "QS3D Element ID", "CAD Handle (hex)", "QS3D Drawing Fingerprint" };
            var range = "A1:F" + Math.Max(1, traces.Count + 1).ToString(CultureInfo.InvariantCulture);
            var sb = BeginSheet(range);
            sb.Append("<cols><col min=\"1\" max=\"1\" width=\"44\" customWidth=\"1\"/><col min=\"2\" max=\"3\" width=\"14\" customWidth=\"1\"/><col min=\"4\" max=\"6\" width=\"42\" customWidth=\"1\"/></cols>");
            sb.Append("<sheetData><row r=\"1\">");
            for (var column = 0; column < headers.Length; column++) Text(sb, Cell(column, 1), headers[column], HeaderStyle);
            sb.Append("</row>");
            for (var index = 0; index < traces.Count; index++)
            {
                var trace = traces[index];
                var row = index + 2;
                sb.Append("<row r=\"").Append(row).Append("\">");
                Text(sb, Cell(0, row), trace.TraceKey, WrappedStyle);
                Text(sb, Cell(1, row), trace.Sheet, 0);
                Number(sb, Cell(2, row), trace.Row, IntegerStyle);
                Text(sb, Cell(3, row), string.Join(";", trace.ElementIds), WrappedStyle);
                Text(sb, Cell(4, row), string.Join(";", trace.Handles), WrappedStyle);
                Text(sb, Cell(5, row), trace.DrawingFingerprint, WrappedStyle);
                sb.Append("</row>");
            }
            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>");
            return sb.ToString();
        }

        private static StringBuilder BeginSheet(string range)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"").Append(range).Append("\"/>");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            return sb;
        }

        private static string BuildTraceKey(string sheet, QuantityReportRow row)
        {
            var ids = row.ElementIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
            var handles = row.SourceHandles.OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
            var raw = sheet + "\u001f" + row.DrawingFingerprint + "\u001f" + string.Join("\u001e", ids) + "\u001f" + string.Join("\u001e", handles);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return sheet + ":" + hex;
            }
        }

        private static string Required(string value, string label)
        {
            var raw = value ?? string.Empty;
            var result = raw.Trim();
            if (result.Length == 0) throw new InvalidDataException("Customer workbook " + label + " is required.");
            if (!string.Equals(raw, result, StringComparison.Ordinal))
                throw new InvalidDataException("Customer workbook " + label + " must be canonical without surrounding whitespace.");
            if (result.Length > 32767) throw new InvalidDataException("Customer workbook " + label + " exceeds the Excel cell text limit.");
            try
            {
                System.Xml.XmlConvert.VerifyXmlChars(result);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new InvalidDataException("Customer workbook " + label + " contains characters that are invalid in XML provenance.", ex);
            }
            return result;
        }

        private static string CanonicalHandle(string value)
        {
            var token = Required(value, "CAD Handle");
            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) token = token.Substring(2);
            ulong number;
            if (!ulong.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number) || number == 0UL)
                throw new InvalidDataException("Customer workbook contains an invalid CAD Handle: " + value + ".");
            return number.ToString("X", CultureInfo.InvariantCulture);
        }

        private static double Checked(double value, bool hasEvidence, string label)
        {
            if (!hasEvidence) return value;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidDataException("Customer workbook " + label + " must be finite and non-negative when evidence exists.");
            return value;
        }

        private static double? CheckedNullable(double? value, string label, bool mustBePositive)
        {
            if (!value.HasValue) return null;
            if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0d || (mustBePositive && value.Value <= 0d))
                throw new InvalidDataException("Customer workbook " + label + " is invalid.");
            return value;
        }

        private static void Evidence(StringBuilder sb, string cell, double value, bool hasEvidence)
        {
            if (hasEvidence) Number(sb, cell, value, DecimalStyle);
        }

        private static void Text(StringBuilder sb, string cell, string value, int style)
        {
            var text = value ?? string.Empty;
            if (text.Length > 32767) throw new InvalidDataException("Customer workbook text cell exceeds the Excel 32,767-character limit: " + cell + ".");
            sb.Append("<c r=\"").Append(cell).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is>");
            XlsxXmlText.AppendTextElement(sb, text);
            sb.Append("</is></c>");
        }

        private static void Number(StringBuilder sb, string cell, double value, int style)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Customer workbook numeric value must be finite.");
            sb.Append("<c r=\"").Append(cell).Append("\" s=\"").Append(style).Append("\"><v>")
              .Append(value.ToString("R", CultureInfo.InvariantCulture)).Append("</v></c>");
        }

        private static string Cell(int column, int row) => ColumnName(column) + row.ToString(CultureInfo.InvariantCulture);

        private static string ColumnName(int column)
        {
            var n = column + 1;
            var result = string.Empty;
            while (n > 0)
            {
                n--;
                result = (char)('A' + n % 26) + result;
                n /= 26;
            }
            return result;
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private sealed class TraceProjection
        {
            public TraceProjection(string traceKey, string sheet, int row, IEnumerable<string> elementIds, IEnumerable<string> handles, string drawingFingerprint)
            {
                TraceKey = traceKey;
                Sheet = sheet;
                Row = row;
                ElementIds = elementIds.ToList().AsReadOnly();
                Handles = handles.ToList().AsReadOnly();
                DrawingFingerprint = drawingFingerprint;
            }

            public string TraceKey { get; }
            public string Sheet { get; }
            public int Row { get; }
            public IReadOnlyList<string> ElementIds { get; }
            public IReadOnlyList<string> Handles { get; }
            public string DrawingFingerprint { get; }
        }

        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet3.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet4.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"DGKL\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"COP_PHA\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"CHI_TIET\" sheetId=\"3\" r:id=\"rId3\"/><sheet name=\"TRACE_MODEL\" sheetId=\"4\" state=\"hidden\" r:id=\"rId4\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet3.xml\"/><Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet4.xml\"/><Relationship Id=\"rId5\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"#,##0.000\"/></numFmts><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFC000\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"2\"><border/><border><left style=\"thin\"/><right style=\"thin\"/><top style=\"thin\"/><bottom style=\"thin\"/></border></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"5\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"3\" applyNumberFormat=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"164\" applyNumberFormat=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf></cellXfs></styleSheet>";
    }
}
