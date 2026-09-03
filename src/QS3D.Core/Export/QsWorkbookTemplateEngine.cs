using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    public enum QsWorkbookTemplateField
    {
        Index,
        Floor,
        Zone,
        FloorZone,
        Category,
        FamilyId,
        FamilyName,
        ElementName,
        Material,
        Note,
        Count,
        GrossConcreteM3,
        DeductionM3,
        NetConcreteM3,
        FormworkM2,
        LengthM,
        OuterPerimeterM,
        InnerPerimeterM,
        DoorAreaM2,
        SideAreaM2,
        BottomAreaM2,
        TopAreaM2,
        OtherAreaM2,
        DensityKgM3,
        MassKg,
        ElementIds,
        SourceHandles,
        DrawingFingerprint,
        TraceKey
    }

    public sealed class QsWorkbookTemplateMapping
    {
        public QsWorkbookTemplateMapping(QsWorkbookTemplateField field, string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Excel column is required.", nameof(column));
            if (column.Any(char.IsControl))
                throw new ArgumentException("Excel column cannot contain control characters.", nameof(column));

            Field = field;
            Column = column.Trim().ToUpperInvariant();
        }

        public QsWorkbookTemplateField Field { get; }
        public string Column { get; }
    }

    public sealed class QsWorkbookTemplateDefinition
    {
        public QsWorkbookTemplateDefinition(
            string worksheetName,
            int firstDataRow,
            IEnumerable<QsWorkbookTemplateMapping> mappings,
            int reservedDataRows = 1)
        {
            if (string.IsNullOrWhiteSpace(worksheetName))
                throw new ArgumentException("Worksheet name is required.", nameof(worksheetName));
            if (worksheetName.Any(char.IsControl))
                throw new ArgumentException("Worksheet name cannot contain control characters.", nameof(worksheetName));
            if (firstDataRow < 1 || firstDataRow > QsWorkbookTemplateExporter.MaxExcelRows)
                throw new ArgumentOutOfRangeException(nameof(firstDataRow));
            if (reservedDataRows < 1 || reservedDataRows > QsWorkbookTemplateExporter.MaxExcelRows - firstDataRow + 1)
                throw new ArgumentOutOfRangeException(nameof(reservedDataRows));
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));

            var snapshot = mappings.ToList();
            if (snapshot.Count == 0) throw new ArgumentException("At least one template mapping is required.", nameof(mappings));
            if (snapshot.Any(mapping => mapping == null)) throw new ArgumentException("Template mappings cannot contain null entries.", nameof(mappings));

            var fields = new HashSet<QsWorkbookTemplateField>();
            var columns = new HashSet<int>();
            foreach (var mapping in snapshot)
            {
                var column = QsWorkbookTemplateExporter.ParseColumn(mapping.Column);
                if (!fields.Add(mapping.Field))
                    throw new ArgumentException("A template field can only be mapped once: " + mapping.Field + ".", nameof(mappings));
                if (!columns.Add(column))
                    throw new ArgumentException("An Excel column can only be mapped once: " + mapping.Column + ".", nameof(mappings));
            }

            WorksheetName = worksheetName.Trim();
            FirstDataRow = firstDataRow;
            ReservedDataRows = reservedDataRows;
            Mappings = snapshot.AsReadOnly();
        }

        public string WorksheetName { get; }
        public int FirstDataRow { get; }
        public int ReservedDataRows { get; }
        public IReadOnlyList<QsWorkbookTemplateMapping> Mappings { get; }
    }

    public sealed class QsWorkbookTemplateTrace
    {
        internal QsWorkbookTemplateTrace(string drawingFingerprint, IReadOnlyList<string> elementIds, IReadOnlyList<string> handles, string traceKey)
        {
            DrawingFingerprint = drawingFingerprint;
            ElementIds = elementIds;
            Handles = handles;
            TraceKey = traceKey;
        }

        public string DrawingFingerprint { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public IReadOnlyList<string> Handles { get; }
        public string TraceKey { get; }
    }

    public static class QsWorkbookTemplateExporter
    {
        internal const int MaxExcelRows = 1048576;
        private const int MaxExcelColumns = 16384;
        internal const long MaxTemplateWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxTemplateMetadataXmlCharacters = 4L * 1024L * 1024L;
        private const long MaxTemplateXmlCharacters = 64L * 1024L * 1024L;
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace OfficeRelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static void Export(
            string templatePath,
            string destinationPath,
            IReadOnlyList<QuantityReportRow> rows,
            QsWorkbookTemplateDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(templatePath)) throw new ArgumentException("Template path is required.", nameof(templatePath));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path is required.", nameof(destinationPath));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rows.Count == 0) throw new InvalidDataException("Template export requires at least one quantity row.");
            if (definition.FirstDataRow + rows.Count - 1 > MaxExcelRows)
                throw new InvalidDataException("Template export exceeds the Excel row limit.");

            var source = Path.GetFullPath(templatePath);
            var destination = Path.GetFullPath(destinationPath);
            if (!File.Exists(source)) throw new FileNotFoundException("XLSX template was not found.", source);
            ValidateTemplatePackageLength(new FileInfo(source).Length);
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Template export must not overwrite the template file in place.");

            var snapshot = SnapshotRows(rows);
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temp = AtomicFileCommit.CreateTempPath(destination);
            try
            {
                File.Copy(source, temp, false);
                string worksheetPart;
                using (var stream = new FileStream(temp, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, false, Encoding.UTF8))
                {
                    worksheetPart = ResolveWorksheetPart(archive, definition.WorksheetName);
                    var worksheetEntry = UniqueEntry(archive, worksheetPart);
                    var worksheet = ReadXml(worksheetEntry, MaxTemplateXmlCharacters);
                    ApplyRows(worksheet, snapshot, definition);
                    ReplaceXmlEntry(archive, worksheetEntry, worksheetPart, worksheet);
                }

                XlsxPackageValidator.Validate(temp, "[Content_Types].xml", "xl/workbook.xml", "xl/_rels/workbook.xml.rels", worksheetPart);
                AtomicFileCommit.ReplaceWithoutBackup(temp, destination);
            }
            finally
            {
                AtomicFileCommit.TryDelete(temp);
            }
        }

        internal static void ValidateTemplatePackageLength(long length)
        {
            if (length < 0 || length > MaxTemplateWorkbookBytes)
                throw new InvalidDataException("XLSX template workbook is too large for bounded processing.");
        }

        internal static int ParseColumn(string column)
        {
            if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Excel column is required.", nameof(column));
            if (column.Any(char.IsControl)) throw new ArgumentException("Excel column cannot contain control characters.", nameof(column));
            var text = column.Trim().ToUpperInvariant();
            var value = 0;
            foreach (var c in text)
            {
                if (c < 'A' || c > 'Z') throw new ArgumentException("Excel column must use A-Z letters only: " + column + ".", nameof(column));
                checked { value = value * 26 + (c - 'A' + 1); }
                if (value > MaxExcelColumns) throw new ArgumentOutOfRangeException(nameof(column), "Excel column exceeds XFD.");
            }
            return value;
        }

        internal static string ColumnName(int column)
        {
            if (column < 1 || column > MaxExcelColumns) throw new ArgumentOutOfRangeException(nameof(column));
            var sb = new StringBuilder();
            var current = column;
            while (current > 0)
            {
                current--;
                sb.Insert(0, (char)('A' + current % 26));
                current /= 26;
            }
            return sb.ToString();
        }

        internal static string TraceKey(string fingerprint, IEnumerable<string> elementIds, IEnumerable<string> handles)
        {
            var canonicalFingerprint = Required(fingerprint, "Drawing Fingerprint");
            var ids = CanonicalTokens(elementIds, "QS3D Element ID", false).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
            var canonicalHandles = CanonicalTokens(handles, "CAD Handle", true).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
            var payload = "QTPL1\n" + canonicalFingerprint + "\n" + string.Join(";", ids) + "\n" + string.Join(";", canonicalHandles);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) hex.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                return "QTPL1:" + hex;
            }
        }

        private static List<QuantityReportRow> SnapshotRows(IReadOnlyList<QuantityReportRow> rows)
        {
            var result = new List<QuantityReportRow>(rows.Count);
            string? fingerprint = null;
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index] ?? throw new InvalidDataException("Template export contains a null quantity row.");
                if (row.Count <= 0) throw new InvalidDataException("Template export row Count must be positive.");
                var ids = CanonicalTokens(row.ElementIds, "QS3D Element ID", false);
                var handles = CanonicalTokens(row.SourceHandles, "CAD Handle", true);
                if (ids.Count != row.Count)
                    throw new InvalidDataException("Template export row Count must equal its QS3D Element ID provenance cardinality.");
                var rowFingerprint = Required(row.DrawingFingerprint, "Drawing Fingerprint");
                if (fingerprint == null) fingerprint = rowFingerprint;
                else if (!string.Equals(fingerprint, rowFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Template export rows contain conflicting drawing fingerprints.");

                ValidateEvidence(row.GrossConcreteM3, row.HasGrossConcreteM3Evidence, "GrossConcreteM3");
                ValidateEvidence(row.DeductionM3, row.HasDeductionM3Evidence, "DeductionM3");
                ValidateEvidence(row.NetConcreteM3, row.HasNetConcreteM3Evidence, "NetConcreteM3");
                ValidateEvidence(row.FormworkM2, row.HasFormworkM2Evidence, "FormworkM2");
                ValidateEvidence(row.LengthM, row.HasLengthMEvidence, "LengthM");
                ValidateEvidence(row.OuterPerimeterM, row.HasOuterPerimeterMEvidence, "OuterPerimeterM");
                ValidateEvidence(row.InnerPerimeterM, row.HasInnerPerimeterMEvidence, "InnerPerimeterM");
                ValidateEvidence(row.DoorAreaM2, row.HasDoorAreaM2Evidence, "DoorAreaM2");
                ValidateEvidence(row.SideAreaM2, row.HasSideAreaM2Evidence, "SideAreaM2");
                ValidateEvidence(row.BottomAreaM2, row.HasBottomAreaM2Evidence, "BottomAreaM2");
                ValidateEvidence(row.TopAreaM2, row.HasTopAreaM2Evidence, "TopAreaM2");
                ValidateEvidence(row.OtherAreaM2, row.HasOtherAreaM2Evidence, "OtherAreaM2");
                ValidateNullable(row.DensityKgM3, "DensityKgM3");
                ValidateNullable(row.MassKg, "MassKg");

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
                    DrawingFingerprint = rowFingerprint,
                    Count = row.Count,
                    GrossConcreteM3 = row.GrossConcreteM3,
                    DeductionM3 = row.DeductionM3,
                    NetConcreteM3 = row.NetConcreteM3,
                    FormworkM2 = row.FormworkM2,
                    LengthM = row.LengthM,
                    OuterPerimeterM = row.OuterPerimeterM,
                    InnerPerimeterM = row.InnerPerimeterM,
                    DoorAreaM2 = row.DoorAreaM2,
                    SideAreaM2 = row.SideAreaM2,
                    BottomAreaM2 = row.BottomAreaM2,
                    TopAreaM2 = row.TopAreaM2,
                    OtherAreaM2 = row.OtherAreaM2,
                    HasGrossConcreteM3Evidence = row.HasGrossConcreteM3Evidence,
                    HasDeductionM3Evidence = row.HasDeductionM3Evidence,
                    HasNetConcreteM3Evidence = row.HasNetConcreteM3Evidence,
                    HasFormworkM2Evidence = row.HasFormworkM2Evidence,
                    HasLengthMEvidence = row.HasLengthMEvidence,
                    HasOuterPerimeterMEvidence = row.HasOuterPerimeterMEvidence,
                    HasInnerPerimeterMEvidence = row.HasInnerPerimeterMEvidence,
                    HasDoorAreaM2Evidence = row.HasDoorAreaM2Evidence,
                    HasSideAreaM2Evidence = row.HasSideAreaM2Evidence,
                    HasBottomAreaM2Evidence = row.HasBottomAreaM2Evidence,
                    HasTopAreaM2Evidence = row.HasTopAreaM2Evidence,
                    HasOtherAreaM2Evidence = row.HasOtherAreaM2Evidence,
                    DensityKgM3 = row.DensityKgM3,
                    MassKg = row.MassKg
                };
                foreach (var id in ids) copy.ElementIds.Add(id);
                foreach (var handle in handles) copy.SourceHandles.Add(handle);
                result.Add(copy);
            }
            return result;
        }

        private static void ApplyRows(XDocument worksheet, IReadOnlyList<QuantityReportRow> rows, QsWorkbookTemplateDefinition definition)
        {
            var root = worksheet.Root ?? throw new InvalidDataException("Worksheet XML has no root element.");
            var sheetData = root.Element(SpreadsheetNs + "sheetData") ?? throw new InvalidDataException("Worksheet has no sheetData element.");
            var allRows = sheetData.Elements(SpreadsheetNs + "row").ToList();
            if (allRows.Any(row => row.Attribute("r") == null)) throw new InvalidDataException("Template worksheet contains a row without an r index.");
            var indexedRows = allRows.ToDictionary(ParseRowIndex);
            XElement templateRow;
            if (!indexedRows.TryGetValue(definition.FirstDataRow, out templateRow))
                throw new InvalidDataException("Template worksheet is missing the configured first data row " + definition.FirstDataRow + ".");

            var reservedEnd = definition.FirstDataRow + definition.ReservedDataRows - 1;
            var generatedEnd = definition.FirstDataRow + rows.Count - 1;
            var lastPhysicalRow = indexedRows.Count == 0 ? 0 : indexedRows.Keys.Max();
            if (rows.Count > definition.ReservedDataRows && lastPhysicalRow > reservedEnd)
                throw new InvalidDataException("Template has worksheet rows below the reserved data block; increase ReservedDataRows before exporting more rows.");

            var mappedColumns = definition.Mappings.ToDictionary(mapping => ParseColumn(mapping.Column), mapping => mapping.Field);
            RejectMergedDataCells(root, definition.FirstDataRow, Math.Max(reservedEnd, generatedEnd));
            RejectMappedTemplateFormulas(templateRow, mappedColumns.Keys);
            if (rows.Count > definition.ReservedDataRows && HasFormula(templateRow))
                throw new InvalidDataException("An expandable template data row cannot contain formulas; reserve preformatted rows or move formulas outside the data row.");

            var outputRows = new Dictionary<int, XElement>(indexedRows);
            for (var index = 0; index < rows.Count; index++)
            {
                var excelRow = definition.FirstDataRow + index;
                XElement rowElement;
                if (excelRow == definition.FirstDataRow)
                {
                    rowElement = new XElement(templateRow);
                }
                else if (excelRow <= reservedEnd && indexedRows.ContainsKey(excelRow))
                {
                    rowElement = new XElement(indexedRows[excelRow]);
                }
                else
                {
                    rowElement = CloneRow(templateRow, excelRow);
                }
                SetRowIndex(rowElement, excelRow);
                WriteMappedCells(rowElement, rows[index], index + 1, excelRow, mappedColumns);
                outputRows[excelRow] = rowElement;
            }

            for (var excelRow = definition.FirstDataRow + rows.Count; excelRow <= reservedEnd; excelRow++)
            {
                XElement existing;
                if (!outputRows.TryGetValue(excelRow, out existing)) continue;
                var cleared = new XElement(existing);
                SetRowIndex(cleared, excelRow);
                ClearMappedCells(cleared, excelRow, mappedColumns.Keys);
                outputRows[excelRow] = cleared;
            }

            sheetData.RemoveNodes();
            foreach (var row in outputRows.OrderBy(pair => pair.Key).Select(pair => pair.Value)) sheetData.Add(row);
            ExpandDimension(root, Math.Max(generatedEnd, lastPhysicalRow));
            ExpandAutoFilter(root, definition.FirstDataRow, reservedEnd, generatedEnd);
        }

        private static void RejectMappedTemplateFormulas(XElement templateRow, IEnumerable<int> mappedColumns)
        {
            var mapped = new HashSet<int>(mappedColumns);
            foreach (var cell in templateRow.Elements(SpreadsheetNs + "c"))
            {
                var reference = (string)cell.Attribute("r");
                int column;
                int row;
                if (!TryParseCell(reference, out column, out row)) throw new InvalidDataException("Template data row contains an invalid cell reference.");
                if (mapped.Contains(column) && cell.Element(SpreadsheetNs + "f") != null)
                    throw new InvalidDataException("Mapped template cell " + reference + " contains a formula and cannot be overwritten safely.");
            }
        }

        private static bool HasFormula(XElement row)
        {
            return row.Elements(SpreadsheetNs + "c").Any(cell => cell.Element(SpreadsheetNs + "f") != null);
        }

        private static void RejectMergedDataCells(XElement root, int firstRow, int lastRow)
        {
            var merges = root.Element(SpreadsheetNs + "mergeCells");
            if (merges == null) return;
            foreach (var merge in merges.Elements(SpreadsheetNs + "mergeCell"))
            {
                var reference = (string)merge.Attribute("ref");
                int startRow;
                int endRow;
                if (!TryParseRangeRows(reference, out startRow, out endRow))
                    throw new InvalidDataException("Template contains an invalid merged-cell reference.");
                if (startRow <= lastRow && endRow >= firstRow)
                    throw new InvalidDataException("Merged cells cannot intersect the configured template data block: " + reference + ".");
            }
        }

        private static XElement CloneRow(XElement templateRow, int targetRow)
        {
            var clone = new XElement(templateRow);
            SetRowIndex(clone, targetRow);
            return clone;
        }

        private static void SetRowIndex(XElement row, int targetRow)
        {
            row.SetAttributeValue("r", targetRow.ToString(CultureInfo.InvariantCulture));
            foreach (var cell in row.Elements(SpreadsheetNs + "c"))
            {
                var reference = (string)cell.Attribute("r");
                int column;
                int ignored;
                if (!TryParseCell(reference, out column, out ignored)) throw new InvalidDataException("Template data row contains an invalid cell reference.");
                cell.SetAttributeValue("r", ColumnName(column) + targetRow.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void WriteMappedCells(XElement row, QuantityReportRow data, int index, int excelRow, IReadOnlyDictionary<int, QsWorkbookTemplateField> mappings)
        {
            foreach (var pair in mappings.OrderBy(item => item.Key))
            {
                var cell = GetOrCreateCell(row, pair.Key, excelRow);
                var value = ProjectValue(data, index, pair.Value);
                WriteValue(cell, value);
            }
        }

        private static void ClearMappedCells(XElement row, int excelRow, IEnumerable<int> mappedColumns)
        {
            foreach (var column in mappedColumns)
            {
                var cell = GetOrCreateCell(row, column, excelRow);
                WriteValue(cell, CellValue.Blank());
            }
        }

        private static XElement GetOrCreateCell(XElement row, int column, int excelRow)
        {
            foreach (var cell in row.Elements(SpreadsheetNs + "c"))
            {
                int existingColumn;
                int ignored;
                if (!TryParseCell((string)cell.Attribute("r"), out existingColumn, out ignored))
                    throw new InvalidDataException("Template data row contains an invalid cell reference.");
                if (existingColumn == column) return cell;
            }

            var created = new XElement(SpreadsheetNs + "c", new XAttribute("r", ColumnName(column) + excelRow.ToString(CultureInfo.InvariantCulture)));
            var next = row.Elements(SpreadsheetNs + "c").FirstOrDefault(cell =>
            {
                int existingColumn;
                int ignored;
                return TryParseCell((string)cell.Attribute("r"), out existingColumn, out ignored) && existingColumn > column;
            });
            if (next == null) row.Add(created); else next.AddBeforeSelf(created);
            return created;
        }

        private static CellValue ProjectValue(QuantityReportRow row, int index, QsWorkbookTemplateField field)
        {
            switch (field)
            {
                case QsWorkbookTemplateField.Index: return CellValue.Number(index);
                case QsWorkbookTemplateField.Floor: return CellValue.Text(row.Floor);
                case QsWorkbookTemplateField.Zone: return CellValue.Text(row.Zone);
                case QsWorkbookTemplateField.FloorZone: return CellValue.Text(row.FloorZoneText);
                case QsWorkbookTemplateField.Category: return CellValue.Text(row.Category);
                case QsWorkbookTemplateField.FamilyId: return CellValue.Text(row.FamilyId);
                case QsWorkbookTemplateField.FamilyName: return CellValue.Text(row.FamilyName);
                case QsWorkbookTemplateField.ElementName: return CellValue.Text(row.ElementName);
                case QsWorkbookTemplateField.Material: return CellValue.Text(row.Material);
                case QsWorkbookTemplateField.Note: return CellValue.Text(row.Note);
                case QsWorkbookTemplateField.Count: return CellValue.Number(row.Count);
                case QsWorkbookTemplateField.GrossConcreteM3: return Evidence(row.GrossConcreteM3, row.HasGrossConcreteM3Evidence);
                case QsWorkbookTemplateField.DeductionM3: return Evidence(row.DeductionM3, row.HasDeductionM3Evidence);
                case QsWorkbookTemplateField.NetConcreteM3: return Evidence(row.NetConcreteM3, row.HasNetConcreteM3Evidence);
                case QsWorkbookTemplateField.FormworkM2: return Evidence(row.FormworkM2, row.HasFormworkM2Evidence);
                case QsWorkbookTemplateField.LengthM: return Evidence(row.LengthM, row.HasLengthMEvidence);
                case QsWorkbookTemplateField.OuterPerimeterM: return Evidence(row.OuterPerimeterM, row.HasOuterPerimeterMEvidence);
                case QsWorkbookTemplateField.InnerPerimeterM: return Evidence(row.InnerPerimeterM, row.HasInnerPerimeterMEvidence);
                case QsWorkbookTemplateField.DoorAreaM2: return Evidence(row.DoorAreaM2, row.HasDoorAreaM2Evidence);
                case QsWorkbookTemplateField.SideAreaM2: return Evidence(row.SideAreaM2, row.HasSideAreaM2Evidence);
                case QsWorkbookTemplateField.BottomAreaM2: return Evidence(row.BottomAreaM2, row.HasBottomAreaM2Evidence);
                case QsWorkbookTemplateField.TopAreaM2: return Evidence(row.TopAreaM2, row.HasTopAreaM2Evidence);
                case QsWorkbookTemplateField.OtherAreaM2: return Evidence(row.OtherAreaM2, row.HasOtherAreaM2Evidence);
                case QsWorkbookTemplateField.DensityKgM3: return row.DensityKgM3.HasValue ? CellValue.Number(row.DensityKgM3.Value) : CellValue.Blank();
                case QsWorkbookTemplateField.MassKg: return row.MassKg.HasValue ? CellValue.Number(row.MassKg.Value) : CellValue.Blank();
                case QsWorkbookTemplateField.ElementIds: return CellValue.Text(string.Join(";", row.ElementIds));
                case QsWorkbookTemplateField.SourceHandles: return CellValue.Text(string.Join(";", row.SourceHandles));
                case QsWorkbookTemplateField.DrawingFingerprint: return CellValue.Text(row.DrawingFingerprint);
                case QsWorkbookTemplateField.TraceKey: return CellValue.Text(TraceKey(row.DrawingFingerprint, row.ElementIds, row.SourceHandles));
                default: throw new InvalidDataException("Unsupported template field: " + field + ".");
            }
        }

        private static CellValue Evidence(double value, bool hasEvidence)
        {
            return hasEvidence ? CellValue.Number(value) : CellValue.Blank();
        }

        private static void WriteValue(XElement cell, CellValue value)
        {
            var reference = cell.Attribute("r");
            var style = cell.Attribute("s");
            cell.RemoveAttributes();
            if (reference != null) cell.Add(new XAttribute("r", reference.Value));
            if (style != null) cell.Add(new XAttribute("s", style.Value));
            cell.RemoveNodes();
            if (value.Kind == CellValueKind.Blank) return;
            if (value.Kind == CellValueKind.Number)
            {
                cell.Add(new XElement(SpreadsheetNs + "v", value.NumberValue.ToString("R", CultureInfo.InvariantCulture)));
                return;
            }

            cell.SetAttributeValue("t", "inlineStr");
            var textValue = value.TextValue ?? string.Empty;
            var text = new XElement(SpreadsheetNs + "t", textValue);
            if (textValue.Length > 0 &&
                (char.IsWhiteSpace(textValue[0]) || char.IsWhiteSpace(textValue[textValue.Length - 1])))
                text.SetAttributeValue(XNamespace.Xml + "space", "preserve");
            cell.Add(new XElement(SpreadsheetNs + "is", text));
        }

        private static int ParseRowIndex(XElement row)
        {
            int value;
            if (!int.TryParse((string)row.Attribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out value) || value < 1 || value > MaxExcelRows)
                throw new InvalidDataException("Template worksheet contains an invalid row index.");
            return value;
        }

        private static void ExpandDimension(XElement root, int lastRow)
        {
            var dimension = root.Element(SpreadsheetNs + "dimension");
            if (dimension == null || lastRow < 1) return;
            var reference = (string)dimension.Attribute("ref");
            if (string.IsNullOrWhiteSpace(reference)) return;
            var parts = reference.Split(':');
            int startColumn;
            int startRow;
            int endColumn;
            int endRow;
            if (!TryParseCell(parts[0], out startColumn, out startRow)) return;
            if (parts.Length == 1)
            {
                endColumn = startColumn;
                endRow = startRow;
            }
            else if (parts.Length == 2 && TryParseCell(parts[1], out endColumn, out endRow))
            {
            }
            else return;
            if (lastRow > endRow) endRow = lastRow;
            dimension.SetAttributeValue("ref", ColumnName(startColumn) + startRow.ToString(CultureInfo.InvariantCulture) + ":" + ColumnName(endColumn) + endRow.ToString(CultureInfo.InvariantCulture));
        }

        private static void ExpandAutoFilter(XElement root, int firstDataRow, int reservedEnd, int generatedEnd)
        {
            if (generatedEnd <= reservedEnd) return;
            var filter = root.Element(SpreadsheetNs + "autoFilter");
            if (filter == null) return;
            var reference = (string)filter.Attribute("ref");
            if (string.IsNullOrWhiteSpace(reference)) return;
            var parts = reference.Split(':');
            if (parts.Length != 2) return;
            int startColumn;
            int startRow;
            int endColumn;
            int endRow;
            if (!TryParseCell(parts[0], out startColumn, out startRow) || !TryParseCell(parts[1], out endColumn, out endRow)) return;
            if (endRow != reservedEnd && endRow != firstDataRow) return;
            filter.SetAttributeValue("ref", ColumnName(startColumn) + startRow.ToString(CultureInfo.InvariantCulture) + ":" + ColumnName(endColumn) + generatedEnd.ToString(CultureInfo.InvariantCulture));
        }

        private static bool TryParseRangeRows(string reference, out int startRow, out int endRow)
        {
            startRow = 0;
            endRow = 0;
            if (string.IsNullOrWhiteSpace(reference)) return false;
            var parts = reference.Split(':');
            if (parts.Length < 1 || parts.Length > 2) return false;
            int startColumn;
            if (!TryParseCell(parts[0].Replace("$", string.Empty), out startColumn, out startRow)) return false;
            if (parts.Length == 1) { endRow = startRow; return true; }
            int endColumn;
            return TryParseCell(parts[1].Replace("$", string.Empty), out endColumn, out endRow);
        }

        internal static bool TryParseCell(string reference, out int column, out int row)
        {
            column = 0;
            row = 0;
            if (string.IsNullOrWhiteSpace(reference)) return false;
            var index = 0;
            while (index < reference.Length && reference[index] >= 'A' && reference[index] <= 'Z')
            {
                column = column * 26 + reference[index] - 'A' + 1;
                if (column > MaxExcelColumns) return false;
                index++;
            }
            if (index == 0 || index == reference.Length) return false;
            return int.TryParse(reference.Substring(index), NumberStyles.None, CultureInfo.InvariantCulture, out row) && row >= 1 && row <= MaxExcelRows;
        }

        internal static string ResolveWorksheetPart(ZipArchive archive, string worksheetName)
        {
            var workbook = ReadXml(UniqueEntry(archive, "xl/workbook.xml"), MaxTemplateMetadataXmlCharacters);
            var sheet = workbook.Descendants(SpreadsheetNs + "sheet")
                .SingleOrDefault(item => string.Equals((string)item.Attribute("name"), worksheetName, StringComparison.Ordinal));
            if (sheet == null) throw new InvalidDataException("Template workbook does not contain worksheet '" + worksheetName + "'.");
            var relationshipId = (string)sheet.Attribute(OfficeRelationshipNs + "id");
            if (string.IsNullOrWhiteSpace(relationshipId)) throw new InvalidDataException("Template worksheet has no relationship id.");

            var relationships = ReadXml(UniqueEntry(archive, "xl/_rels/workbook.xml.rels"), MaxTemplateMetadataXmlCharacters);
            var relationship = relationships.Descendants(PackageRelationshipNs + "Relationship")
                .SingleOrDefault(item => string.Equals((string)item.Attribute("Id"), relationshipId, StringComparison.Ordinal));
            if (relationship == null) throw new InvalidDataException("Template worksheet relationship cannot be resolved.");
            var target = (string)relationship.Attribute("Target");
            if (string.IsNullOrWhiteSpace(target)) throw new InvalidDataException("Template worksheet relationship has no target.");
            if (target.Contains("..") || target.Contains(":") || target.StartsWith("/", StringComparison.Ordinal))
                throw new InvalidDataException("Template worksheet relationship target must stay inside the XLSX package.");
            var normalized = target.Replace('\\', '/');
            return normalized.StartsWith("xl/", StringComparison.Ordinal) ? normalized : "xl/" + normalized;
        }

        internal static ZipArchiveEntry UniqueEntry(ZipArchive archive, string path)
        {
            var matches = archive.Entries.Where(entry => string.Equals(entry.FullName, path, StringComparison.Ordinal)).ToList();
            if (matches.Count != 1) throw new InvalidDataException("XLSX package must contain exactly one '" + path + "' part.");
            return matches[0];
        }

        internal static XDocument ReadXml(ZipArchiveEntry entry, long maxCharacters = MaxTemplateXmlCharacters)
        {
            if (entry.Length < 0 || entry.Length > maxCharacters)
                throw new InvalidDataException("XLSX template XML part is too large: " + entry.FullName + ".");
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = maxCharacters,
                MaxCharactersFromEntities = 0
            };
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, settings))
                return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }

        private static void ReplaceXmlEntry(ZipArchive archive, ZipArchiveEntry oldEntry, string path, XDocument document)
        {
            oldEntry.Delete();
            var replacement = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (var stream = replacement.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                document.Save(writer, SaveOptions.DisableFormatting);
        }

        internal static string Required(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(label + " is required.");
            if (value.Any(char.IsControl)) throw new InvalidDataException(label + " contains a control character.");
            return value.Trim();
        }

        internal static List<string> CanonicalTokens(IEnumerable<string> values, string label, bool handles)
        {
            if (values == null) throw new InvalidDataException(label + " provenance is missing.");
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                var canonical = Required(value, label);
                if (handles)
                {
                    ulong parsed;
                    if (!ulong.TryParse(canonical, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out parsed))
                        throw new InvalidDataException("Invalid CAD Handle: " + canonical + ".");
                    canonical = parsed.ToString("X", CultureInfo.InvariantCulture);
                }
                if (!seen.Add(canonical)) throw new InvalidDataException(label + " provenance contains duplicate value: " + canonical + ".");
                result.Add(canonical);
            }
            if (result.Count == 0) throw new InvalidDataException(label + " provenance is empty.");
            return result;
        }

        private static void ValidateEvidence(double value, bool hasEvidence, string label)
        {
            if (hasEvidence && (double.IsNaN(value) || double.IsInfinity(value)))
                throw new InvalidDataException(label + " evidence must be finite.");
        }

        private static void ValidateNullable(double? value, string label)
        {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value)))
                throw new InvalidDataException(label + " must be finite when present.");
        }

        private enum CellValueKind { Blank, Text, Number }

        private struct CellValue
        {
            internal CellValueKind Kind;
            internal string? TextValue;
            internal double NumberValue;

            internal static CellValue Blank() { return new CellValue { Kind = CellValueKind.Blank }; }
            internal static CellValue Text(string value) { return new CellValue { Kind = CellValueKind.Text, TextValue = value ?? string.Empty }; }
            internal static CellValue Number(double value) { return new CellValue { Kind = CellValueKind.Number, NumberValue = value }; }
        }
    }

    public static class QsWorkbookTemplateTraceReader
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        public static QsWorkbookTemplateTrace Read(string path, QsWorkbookTemplateDefinition definition, int excelRow)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Workbook path is required.", nameof(path));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (excelRow < definition.FirstDataRow || excelRow > QsWorkbookTemplateExporter.MaxExcelRows)
                throw new ArgumentOutOfRangeException(nameof(excelRow));

            var fields = definition.Mappings.ToDictionary(mapping => mapping.Field, mapping => QsWorkbookTemplateExporter.ParseColumn(mapping.Column));
            var required = new[]
            {
                QsWorkbookTemplateField.DrawingFingerprint,
                QsWorkbookTemplateField.ElementIds,
                QsWorkbookTemplateField.SourceHandles,
                QsWorkbookTemplateField.TraceKey
            };
            foreach (var field in required)
                if (!fields.ContainsKey(field)) throw new InvalidDataException("Template trace requires mapping for " + field + ".");

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Template workbook was not found.", fullPath);
            QsWorkbookTemplateExporter.ValidateTemplatePackageLength(new FileInfo(fullPath).Length);
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false, Encoding.UTF8))
            {
                var part = QsWorkbookTemplateExporter.ResolveWorksheetPart(archive, definition.WorksheetName);
                var worksheet = QsWorkbookTemplateExporter.ReadXml(QsWorkbookTemplateExporter.UniqueEntry(archive, part));
                var sharedStrings = ReadSharedStringsIfPresent(archive);
                var row = worksheet.Descendants(SpreadsheetNs + "row")
                    .SingleOrDefault(item => string.Equals((string)item.Attribute("r"), excelRow.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
                if (row == null) throw new InvalidDataException("Template workbook row " + excelRow + " was not found.");

                var fingerprint = QsWorkbookTemplateExporter.Required(ReadField(row, fields[QsWorkbookTemplateField.DrawingFingerprint], excelRow, sharedStrings), "Drawing Fingerprint");
                var ids = SplitTokens(ReadField(row, fields[QsWorkbookTemplateField.ElementIds], excelRow, sharedStrings), "QS3D Element ID", false);
                var handles = SplitTokens(ReadField(row, fields[QsWorkbookTemplateField.SourceHandles], excelRow, sharedStrings), "CAD Handle", true);
                var traceKey = QsWorkbookTemplateExporter.Required(ReadField(row, fields[QsWorkbookTemplateField.TraceKey], excelRow, sharedStrings), "Template Trace Key");
                var expected = QsWorkbookTemplateExporter.TraceKey(fingerprint, ids, handles);
                if (!string.Equals(traceKey, expected, StringComparison.Ordinal))
                    throw new InvalidDataException("Template workbook trace key does not match the row provenance.");
                return new QsWorkbookTemplateTrace(fingerprint, ids.AsReadOnly(), handles.AsReadOnly(), traceKey);
            }
        }

        private static List<string> SplitTokens(string value, string label, bool handles)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(label + " provenance is empty.");
            return QsWorkbookTemplateExporter.CanonicalTokens(value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), label, handles);
        }

        private static string ReadField(XElement row, int column, int excelRow, IReadOnlyList<string> sharedStrings)
        {
            var reference = QsWorkbookTemplateExporter.ColumnName(column) + excelRow.ToString(CultureInfo.InvariantCulture);
            var cell = row.Elements(SpreadsheetNs + "c")
                .SingleOrDefault(item => string.Equals((string)item.Attribute("r"), reference, StringComparison.Ordinal));
            if (cell == null) return string.Empty;
            var type = (string)cell.Attribute("t");
            if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
                return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(item => item.Value));
            var raw = (string)cell.Element(SpreadsheetNs + "v") ?? string.Empty;
            if (!string.Equals(type, "s", StringComparison.Ordinal)) return raw;
            int index;
            if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out index) || index < 0 || index >= sharedStrings.Count)
                throw new InvalidDataException("Template workbook contains an invalid shared-string index.");
            return sharedStrings[index];
        }

        private static IReadOnlyList<string> ReadSharedStringsIfPresent(ZipArchive archive)
        {
            var entries = archive.Entries.Where(entry => string.Equals(entry.FullName, "xl/sharedStrings.xml", StringComparison.Ordinal)).ToList();
            if (entries.Count > 1) throw new InvalidDataException("XLSX package contains duplicate sharedStrings.xml parts.");
            if (entries.Count == 0) return new List<string>().AsReadOnly();
            var document = QsWorkbookTemplateExporter.ReadXml(entries[0]);
            return document.Descendants(SpreadsheetNs + "si")
                .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
                .ToList()
                .AsReadOnly();
        }
    }
}
