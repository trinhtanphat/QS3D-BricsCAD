using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace QS3D.Core.Export
{
    public sealed class XlsxHandleLookupResult
    {
        public XlsxHandleLookupResult(IEnumerable<string> handles, string drawingFingerprint, bool usesLegacyDecimalHandles)
            : this(handles, Array.Empty<string>(), drawingFingerprint, usesLegacyDecimalHandles, string.Empty, false, false)
        {
        }

        public XlsxHandleLookupResult(IEnumerable<string> handles, IEnumerable<string> elementIds, string drawingFingerprint, bool usesLegacyDecimalHandles)
            : this(handles, elementIds, drawingFingerprint, usesLegacyDecimalHandles, string.Empty, false, false)
        {
        }

        internal XlsxHandleLookupResult(
            IEnumerable<string> handles,
            IEnumerable<string> elementIds,
            string drawingFingerprint,
            bool usesLegacyDecimalHandles,
            string worksheetName,
            bool isModernSchema,
            bool isEd2Detail)
        {
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            Handles = handles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            ElementIds = elementIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            DrawingFingerprint = (drawingFingerprint ?? string.Empty).Trim();
            UsesLegacyDecimalHandles = usesLegacyDecimalHandles;
            WorksheetName = (worksheetName ?? string.Empty).Trim();
            IsModernSchema = isModernSchema;
            IsEd2Detail = isEd2Detail;
        }

        public IReadOnlyList<string> Handles { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public string DrawingFingerprint { get; }
        public bool UsesLegacyDecimalHandles { get; }
        public string WorksheetName { get; }
        public bool IsModernSchema { get; }
        public bool IsEd2Detail { get; }
    }

    public static class XlsxHandleReader
    {
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxXmlCharacters = 64L * 1024L * 1024L;
        private const int MaxColumns = 16384;
        private const int MaxRows = 1048576;
        private const string UnsupportedCellSentinel = "#QS3D_XLSX_UNSUPPORTED!";
        private const string WorksheetRelationshipTypeHttp = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
        private const string WorksheetRelationshipTypeHttps = "https://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
        private static readonly Regex DecimalHandlePattern = new Regex(@"\$(\d+)", RegexOptions.CultureInvariant);
        private static readonly Regex LegacyDecimalCellPattern = new Regex(@"^\s*(?:\$\d+\s*)+$", RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> ReadHandles(string path, int rowNumber) => ReadHandleLookup(path, rowNumber).Handles;

        public static XlsxHandleLookupResult ReadHandleLookup(string path, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Excel path is required.", nameof(path));
            if (rowNumber < 1 || rowNumber > MaxRows) throw new ArgumentOutOfRangeException(nameof(rowNumber), "Excel row number must be between 1 and " + MaxRows + ".");
            var fullPath = Path.GetFullPath(path);
            var file = new FileInfo(fullPath);
            if (!file.Exists) throw new FileNotFoundException("Excel workbook was not found.", fullPath);
            if (file.Length > MaxWorkbookBytes) throw new InvalidDataException("Excel workbook is too large for Handle lookup.");

            using (var archive = ZipFile.OpenRead(fullPath))
            {
                var worksheet = ResolveWorksheet(archive);
                var sheetEntry = worksheet.Entry;
                var sharedStrings = ReadSharedStrings(archive);
                var sheet = LoadXml(sheetEntry);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var rows = sheet.Descendants(ns + "row").ToList();
                foreach (var row in rows)
                {
                    var declaredRowText = (string?)row.Attribute("r");
                    if (declaredRowText == null) continue;
                    var declaredRow = ParsePositiveInt(declaredRowText);
                    if (declaredRow == int.MaxValue || declaredRow > MaxRows)
                        throw new InvalidDataException("Excel worksheet row number is invalid or exceeds the XLSX row limit.");
                }
                var targets = rows.Where(x => ParsePositiveInt((string?)x.Attribute("r")) == rowNumber).ToList();
                if (targets.Count > 1) throw new InvalidDataException("Excel worksheet contains duplicate row number " + rowNumber + ".");
                var target = targets.SingleOrDefault();
                if (target == null)
                    return new XlsxHandleLookupResult(Array.Empty<string>(), Array.Empty<string>(), string.Empty, false, worksheet.Name, false, worksheet.IsEd2Detail);

                var targetCells = ReadCells(target, ns, sharedStrings, out var targetFormulaColumns);
                var handleColumns = new HashSet<int>();
                var fuzzyHandleColumns = new HashSet<int>();
                var elementIdColumns = new HashSet<int>();
                var fingerprintColumns = new HashSet<int>();
                var formulaIdentityHeaderColumns = new HashSet<int>();
                foreach (var headerRow in rows.Where(x => ParsePositiveInt((string?)x.Attribute("r")) < rowNumber).Take(10))
                {
                    var headerCells = ReadCells(headerRow, ns, sharedStrings, out var headerFormulaColumns);
                    foreach (var cell in headerCells)
                    {
                        var header = (cell.Value ?? string.Empty).Trim();
                        if (string.Equals(header, "CAD Handle (hex)", StringComparison.OrdinalIgnoreCase))
                        {
                            handleColumns.Add(cell.Key);
                            if (headerFormulaColumns.Contains(cell.Key)) formulaIdentityHeaderColumns.Add(cell.Key);
                        }
                        else if (header.IndexOf("handle", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            fuzzyHandleColumns.Add(cell.Key);
                            if (headerFormulaColumns.Contains(cell.Key)) formulaIdentityHeaderColumns.Add(cell.Key);
                        }
                        if (string.Equals(header, "QS3D Drawing Fingerprint", StringComparison.OrdinalIgnoreCase))
                        {
                            fingerprintColumns.Add(cell.Key);
                            if (headerFormulaColumns.Contains(cell.Key)) formulaIdentityHeaderColumns.Add(cell.Key);
                        }
                        if (string.Equals(header, "QS3D Element ID", StringComparison.OrdinalIgnoreCase))
                        {
                            elementIdColumns.Add(cell.Key);
                            if (headerFormulaColumns.Contains(cell.Key)) formulaIdentityHeaderColumns.Add(cell.Key);
                        }
                    }
                }
                if (handleColumns.Count == 0) handleColumns.UnionWith(fuzzyHandleColumns);

                var isModernSchema = elementIdColumns.Count > 0 || fingerprintColumns.Count > 0;
                var identityColumns = handleColumns.Concat(elementIdColumns).Concat(fingerprintColumns).Distinct().ToList();
                if (isModernSchema && formulaIdentityHeaderColumns.Overlaps(identityColumns))
                    throw new InvalidDataException("QS3D Excel identity headers must contain literal values, not formulas.");
                if (isModernSchema && targetFormulaColumns.Overlaps(identityColumns))
                    throw new InvalidDataException("QS3D Excel identity cells must contain literal values, not formulas.");
                foreach (var column in identityColumns)
                    if (targetCells.TryGetValue(column, out var criticalValue) && string.Equals(criticalValue, UnsupportedCellSentinel, StringComparison.Ordinal))
                        throw new InvalidDataException("Excel identity cell contains an unsupported XLSX value type.");
                if (worksheet.IsEd2Detail && !isModernSchema)
                    throw new InvalidDataException("ED2 CHI_TIET is missing its modern QS3D identity headers and cannot be treated as a legacy BLT sheet.");
                if (isModernSchema && (elementIdColumns.Count != 1 || handleColumns.Count != 1 || fingerprintColumns.Count != 1))
                    throw new InvalidDataException("QS3D Excel schema must contain exactly one Element ID, CAD Handle, and drawing fingerprint column.");

                var elementIds = new List<string>();
                foreach (var column in elementIdColumns)
                    if (targetCells.TryGetValue(column, out var value)) AddElementIds(elementIds, value, isModernSchema);

                var drawingFingerprint = ReadDrawingFingerprint(targetCells, fingerprintColumns);
                var decimalHandles = ParseDecimalHandles(targetCells.Values);
                var preferLegacy = !isModernSchema && handleColumns.Count == 0 && decimalHandles.Count > 0 && string.IsNullOrWhiteSpace(drawingFingerprint);
                if (preferLegacy)
                    return new XlsxHandleLookupResult(decimalHandles, Array.Empty<string>(), drawingFingerprint, true, worksheet.Name, false, false);
                var explicitHandles = new List<string>();
                foreach (var column in handleColumns)
                    if (targetCells.TryGetValue(column, out var value)) AddHexHandles(explicitHandles, value, isModernSchema);
                if (isModernSchema)
                {
                    if (elementIds.Count == 0) throw new InvalidDataException("QS3D Excel row is missing its Element ID.");
                    if (worksheet.IsEd2Detail && elementIds.Count != 1) throw new InvalidDataException("ED2 CHI_TIET row must contain exactly one Element ID.");
                    if (explicitHandles.Count == 0) throw new InvalidDataException("QS3D Excel row is missing its CAD Handle provenance.");
                    if (string.IsNullOrWhiteSpace(drawingFingerprint)) throw new InvalidDataException("QS3D Excel row is missing its drawing fingerprint.");
                }
                return new XlsxHandleLookupResult(explicitHandles, elementIds, drawingFingerprint, false, worksheet.Name, isModernSchema, worksheet.IsEd2Detail);
            }
        }

        private static WorksheetReference ResolveWorksheet(ZipArchive archive)
        {
            var workbookEntry = GetUniqueEntry(archive, "xl/workbook.xml");
            var relationshipsEntry = GetUniqueEntry(archive, "xl/_rels/workbook.xml.rels");
            if ((workbookEntry == null) != (relationshipsEntry == null))
                throw new InvalidDataException("Excel workbook metadata is incomplete: workbook.xml and workbook.xml.rels must either both be present or both be absent.");
            if (workbookEntry != null && relationshipsEntry != null)
            {
                var workbook = LoadXml(workbookEntry);
                var relationships = LoadXml(relationshipsEntry);
                XNamespace workbookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XNamespace documentRelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                XNamespace packageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";
                var sheets = workbook.Descendants(workbookNs + "sheet").ToList();
                if (sheets.Count == 0) throw new InvalidDataException("Excel workbook does not declare any worksheets.");
                var detailSheets = sheets.Where(x => string.Equals(((string?)x.Attribute("name") ?? string.Empty).Trim(), "CHI_TIET", StringComparison.OrdinalIgnoreCase)).ToList();
                if (detailSheets.Count > 1) throw new InvalidDataException("Excel workbook contains duplicate CHI_TIET worksheets.");
                var selected = detailSheets.Count == 1 ? detailSheets[0] : sheets[0];
                var relationshipId = (string?)selected.Attribute(documentRelationshipNs + "id");
                if (string.IsNullOrWhiteSpace(relationshipId)) throw new InvalidDataException("Excel worksheet relationship id is missing.");
                var matches = relationships.Descendants(packageRelationshipNs + "Relationship")
                    .Where(x => string.Equals((string?)x.Attribute("Id"), relationshipId, StringComparison.Ordinal))
                    .ToList();
                if (matches.Count != 1) throw new InvalidDataException("Excel worksheet relationship is missing or ambiguous.");
                var relationshipType = ((string?)matches[0].Attribute("Type") ?? string.Empty).Trim();
                if (!string.Equals(relationshipType, WorksheetRelationshipTypeHttp, StringComparison.Ordinal) &&
                    !string.Equals(relationshipType, WorksheetRelationshipTypeHttps, StringComparison.Ordinal))
                    throw new InvalidDataException("Excel workbook sheet relationship is not a worksheet relationship.");
                if (string.Equals((string?)matches[0].Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("External Excel worksheet relationships are not supported.");
                var target = ((string?)matches[0].Attribute("Target") ?? string.Empty).Replace('\\', '/').TrimStart('/');
                if (target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) target = target.Substring(3);
                if (target.IndexOf("..", StringComparison.Ordinal) >= 0) throw new InvalidDataException("Excel worksheet relationship target is invalid.");
                var entry = GetUniqueEntry(archive, "xl/" + target);
                if (entry == null) throw new InvalidDataException("Excel worksheet part is missing: " + target + ".");
                var name = ((string?)selected.Attribute("name") ?? string.Empty).Trim();
                return new WorksheetReference(entry, name, string.Equals(name, "CHI_TIET", StringComparison.OrdinalIgnoreCase));
            }

            var fallback = GetUniqueEntry(archive, "xl/worksheets/sheet1.xml");
            if (fallback == null)
            {
                var candidates = archive.Entries
                    .Where(x => x.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && x.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var duplicate = candidates.GroupBy(x => x.FullName, StringComparer.Ordinal).FirstOrDefault(x => x.Skip(1).Any());
                if (duplicate != null) throw new InvalidDataException("Excel workbook contains duplicate worksheet part: " + duplicate.Key + ".");
                fallback = candidates.FirstOrDefault();
            }
            if (fallback == null) throw new InvalidDataException("Excel workbook does not contain a worksheet.");
            return new WorksheetReference(fallback, string.Empty, false);
        }

        private static string ReadDrawingFingerprint(IReadOnlyDictionary<int, string> cells, IEnumerable<int> columns)
        {
            var values = columns
                .Where(cells.ContainsKey)
                .Select(x => cells[x]?.Trim() ?? string.Empty)
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (values.Count > 1) throw new InvalidDataException("Excel row contains conflicting drawing fingerprints.");
            return values.Count == 0 ? string.Empty : values[0];
        }

        private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = GetUniqueEntry(archive, "xl/sharedStrings.xml");
            if (entry == null) return Array.Empty<string>();
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            return document.Descendants(ns + "si").Select(x => string.Concat(x.Descendants(ns + "t").Select(t => t.Value))).ToList();
        }

        private static ZipArchiveEntry? GetUniqueEntry(ZipArchive archive, string fullName)
        {
            var matches = archive.Entries.Where(x => string.Equals(x.FullName, fullName, StringComparison.Ordinal)).Take(2).ToList();
            if (matches.Count > 1) throw new InvalidDataException("Excel workbook contains duplicate package part: " + fullName + ".");
            return matches.Count == 0 ? null : matches[0];
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            if (entry.Length > MaxXmlCharacters) throw new InvalidDataException("Excel XML part is too large for Handle lookup.");
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxXmlCharacters,
                MaxCharactersFromEntities = 0
            };
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, settings)) return XDocument.Load(reader, LoadOptions.None);
        }

        private static Dictionary<int, string> ReadCells(XElement row, XNamespace ns, IReadOnlyList<string> sharedStrings) =>
            ReadCells(row, ns, sharedStrings, out _);

        private static Dictionary<int, string> ReadCells(XElement row, XNamespace ns, IReadOnlyList<string> sharedStrings, out HashSet<int> formulaColumns)
        {
            var rowNumber = ParsePositiveInt((string?)row.Attribute("r"));
            if (rowNumber == int.MaxValue || rowNumber > MaxRows) throw new InvalidDataException("Excel worksheet row number is missing, invalid, or exceeds the XLSX row limit.");
            var result = new Dictionary<int, string>();
            formulaColumns = new HashSet<int>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var column = ColumnIndex((string?)cell.Attribute("r"), rowNumber);
                if (column >= MaxColumns) throw new InvalidDataException("Excel cell column exceeds the XLSX column limit.");
                if (cell.Element(ns + "f") != null) formulaColumns.Add(column);
                var type = (string?)cell.Attribute("t") ?? string.Empty;
                string value;
                if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase)) value = string.Concat(cell.Descendants(ns + "t").Select(x => x.Value));
                else if (string.Equals(type, "e", StringComparison.OrdinalIgnoreCase)) value = UnsupportedCellSentinel;
                else
                {
                    value = cell.Element(ns + "v")?.Value ?? string.Empty;
                    if (string.Equals(type, "d", StringComparison.OrdinalIgnoreCase))
                    {
                        value = "[Date] " + value;
                    }
                    else if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index < 0 || index >= sharedStrings.Count)
                            throw new InvalidDataException("Excel shared-string cell contains an invalid shared-string index.");
                        value = sharedStrings[index];
                    }
                    else if (string.Equals(type, "b", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(value, "0", StringComparison.Ordinal)) value = "FALSE";
                        else if (string.Equals(value, "1", StringComparison.Ordinal)) value = "TRUE";
                        else throw new InvalidDataException("Excel Boolean cell contains an invalid Boolean value.");
                    }
                }
                if (result.ContainsKey(column)) throw new InvalidDataException("Excel row contains duplicate cells in column " + (column + 1) + ".");
                result.Add(column, value);
            }
            return result;
        }

        private static IReadOnlyList<string> ParseDecimalHandles(IEnumerable<string> values)
        {
            var result = new List<string>();
            foreach (var value in values)
            {
                var candidate = value ?? string.Empty;
                if (!LegacyDecimalCellPattern.IsMatch(candidate)) continue;
                foreach (Match match in DecimalHandlePattern.Matches(candidate))
                    if (long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0) AddUnique(result, number.ToString("X", CultureInfo.InvariantCulture));
            }
            return result;
        }

        private static void AddHexHandles(ICollection<string> result, string value, bool rejectDuplicates)
        {
            foreach (var raw in (value ?? string.Empty).Split(new[] { ';', ',', '|', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = raw.Trim();
                if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) token = token.Substring(2);
                if (!long.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number) || number <= 0)
                    throw new InvalidDataException("Excel row contains an invalid CAD Handle token: " + raw.Trim() + ".");
                var handle = number.ToString("X", CultureInfo.InvariantCulture);
                if (result.Contains(handle, StringComparer.OrdinalIgnoreCase))
                {
                    if (rejectDuplicates)
                        throw new InvalidDataException("QS3D Excel row contains a duplicate CAD Handle token after hexadecimal normalization: " + raw.Trim() + ".");
                    continue;
                }
                result.Add(handle);
            }
        }

        private static void AddElementIds(ICollection<string> result, string value, bool rejectDuplicates)
        {
            foreach (var raw in (value ?? string.Empty).Split(new[] { ';', '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var id = raw.Trim();
                if (id.Length == 0) continue;
                if (result.Contains(id, StringComparer.OrdinalIgnoreCase))
                {
                    if (rejectDuplicates)
                        throw new InvalidDataException("QS3D Excel row contains a duplicate Element ID token: " + id + ".");
                    continue;
                }
                result.Add(id);
            }
        }

        private static void AddUnique(ICollection<string> result, string handle)
        {
            if (!result.Contains(handle, StringComparer.OrdinalIgnoreCase)) result.Add(handle);
        }

        private static int ParsePositiveInt(string? value) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0 ? number : int.MaxValue;

        private static int ColumnIndex(string? cellReference, int expectedRow)
        {
            if (string.IsNullOrWhiteSpace(cellReference)) throw new InvalidDataException("Excel cell reference is missing.");
            var reference = cellReference!;
            var value = 0;
            var index = 0;
            try
            {
                while (index < reference.Length)
                {
                    var ch = reference[index];
                    int letter;
                    if (ch >= 'A' && ch <= 'Z') letter = ch - 'A' + 1;
                    else if (ch >= 'a' && ch <= 'z') letter = ch - 'a' + 1;
                    else break;
                    value = checked(value * 26 + letter);
                    index++;
                }
            }
            catch (OverflowException ex)
            {
                throw new InvalidDataException("Excel cell reference column is invalid.", ex);
            }
            if (index == 0 || index >= reference.Length)
                throw new InvalidDataException("Excel cell reference is invalid: " + reference + ".");

            var rowToken = reference.Substring(index);
            if (!int.TryParse(rowToken, NumberStyles.None, CultureInfo.InvariantCulture, out var referencedRow) || referencedRow <= 0 || referencedRow > MaxRows)
                throw new InvalidDataException("Excel cell reference is invalid or exceeds the XLSX row limit: " + reference + ".");
            if (referencedRow != expectedRow)
                throw new InvalidDataException("Excel cell reference " + reference + " does not match containing row " + expectedRow + ".");
            return value - 1;
        }

        private sealed class WorksheetReference
        {
            public WorksheetReference(ZipArchiveEntry entry, string name, bool isEd2Detail)
            {
                Entry = entry ?? throw new ArgumentNullException(nameof(entry));
                Name = name ?? string.Empty;
                IsEd2Detail = isEd2Detail;
            }

            public ZipArchiveEntry Entry { get; }
            public string Name { get; }
            public bool IsEd2Detail { get; }
        }
    }
}
