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

namespace QS3D.Core.Export
{
    public sealed class QsCustomerWorkbookTrace
    {
        internal QsCustomerWorkbookTrace(string worksheetName, int rowNumber, string traceKey, IEnumerable<string> elementIds, IEnumerable<string> handles, string drawingFingerprint)
        {
            WorksheetName = worksheetName;
            RowNumber = rowNumber;
            TraceKey = traceKey;
            ElementIds = elementIds.ToList().AsReadOnly();
            Handles = handles.ToList().AsReadOnly();
            DrawingFingerprint = drawingFingerprint;
        }

        public string WorksheetName { get; }
        public int RowNumber { get; }
        public string TraceKey { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public IReadOnlyList<string> Handles { get; }
        public string DrawingFingerprint { get; }
    }

    public static class QsCustomerWorkbookTraceReader
    {
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxXmlCharacters = 64L * 1024L * 1024L;
        private const int MaxRows = 1048576;
        private const int MaxSharedStrings = MaxRows;
        private const string WorksheetRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";

        public static QsCustomerWorkbookTrace Read(string path, string worksheetName, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Excel path is required.", nameof(path));
            var requestedSheet = NormalizeBusinessSheet(worksheetName);
            if (rowNumber < 2 || rowNumber > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(rowNumber), "Customer workbook data row must be between 2 and " + MaxRows + ".");

            var fullPath = Path.GetFullPath(path);
            var info = new FileInfo(fullPath);
            if (!info.Exists) throw new FileNotFoundException("Customer workbook was not found.", fullPath);
            if (info.Length > MaxWorkbookBytes) throw new InvalidDataException("Customer workbook is too large for trace lookup.");

            using (var archive = ZipFile.OpenRead(fullPath))
            {
                var sheets = ResolveSheets(archive);
                RequireExactCustomerSheets(sheets.Keys);
                var sharedStrings = ReadSharedStrings(archive);
                var business = sheets[requestedSheet];
                var traceSheet = sheets[QsCustomerWorkbookExporter.TraceSheet];
                var traceKey = ReadCriticalBusinessTraceKey(business, rowNumber, sharedStrings);
                var trace = ReadTraceProjection(traceSheet, traceKey, requestedSheet, rowNumber, sharedStrings);
                if (string.Equals(requestedSheet, QsCustomerWorkbookExporter.DetailSheet, StringComparison.OrdinalIgnoreCase) && trace.ElementIds.Count != 1)
                    throw new InvalidDataException("Customer workbook CHI_TIET trace must reference exactly one QS3D Element ID.");
                if (trace.ElementIds.Count == 0 || trace.Handles.Count == 0 || string.IsNullOrWhiteSpace(trace.DrawingFingerprint))
                    throw new InvalidDataException("Customer workbook TRACE_MODEL identity is incomplete.");
                return trace;
            }
        }

        private static string ReadCriticalBusinessTraceKey(ZipArchiveEntry entry, int rowNumber, IReadOnlyList<string>? sharedStrings)
        {
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var selectedRows = SelectBusinessRowsBounded(document.Descendants(ns + "row"), rowNumber, MaxRows);
            var header = selectedRows.Item1;
            var target = selectedRows.Item2;
            var headerCells = ReadCells(header, ns, sharedStrings, out var headerFormulaColumns);
            var traceColumns = headerCells.Where(pair => string.Equals(pair.Value, QsCustomerWorkbookExporter.TraceHeader, StringComparison.OrdinalIgnoreCase))
                                          .Select(pair => pair.Key).ToList();
            if (traceColumns.Count != 1) throw new InvalidDataException("Customer workbook business sheet must contain exactly one TRACE_KEY header.");
            if (headerFormulaColumns.Contains(traceColumns[0])) throw new InvalidDataException("Customer workbook TRACE_KEY header must be literal.");
            var targetCells = ReadCells(target, ns, sharedStrings, out var targetFormulaColumns);
            if (targetFormulaColumns.Contains(traceColumns[0])) throw new InvalidDataException("Customer workbook TRACE_KEY must be a literal value.");
            string traceKey;
            if (!targetCells.TryGetValue(traceColumns[0], out traceKey) || string.IsNullOrWhiteSpace(traceKey))
                throw new InvalidDataException("Customer workbook business row is missing TRACE_KEY.");
            var canonicalTraceKey = traceKey.Trim();
            if (!string.Equals(traceKey, canonicalTraceKey, StringComparison.Ordinal) || canonicalTraceKey.Any(char.IsControl))
                throw new InvalidDataException("Customer workbook TRACE_KEY must be a canonical literal value.");
            return canonicalTraceKey;
        }

        private static QsCustomerWorkbookTrace ReadTraceProjection(ZipArchiveEntry entry, string traceKey, string worksheetName, int rowNumber, IReadOnlyList<string>? sharedStrings)
        {
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var rows = MaterializeWorksheetRowsBounded(document.Descendants(ns + "row"), MaxRows);
            var header = FindUniqueRow(rows, 1);
            var headers = ReadCells(header, ns, sharedStrings, out var headerFormulaColumns);
            var required = new[]
            {
                QsCustomerWorkbookExporter.TraceHeader,
                "SHEET",
                "ROW",
                "QS3D Element ID",
                "CAD Handle (hex)",
                "QS3D Drawing Fingerprint"
            };
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in required)
            {
                var matches = headers.Where(pair => string.Equals(pair.Value, name, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key).ToList();
                if (matches.Count != 1) throw new InvalidDataException("TRACE_MODEL must contain exactly one " + name + " column.");
                if (headerFormulaColumns.Contains(matches[0])) throw new InvalidDataException("TRACE_MODEL identity headers must be literal.");
                columns[name] = matches[0];
            }

            var matchesByKey = new List<Tuple<int, Dictionary<int, string>, HashSet<int>>>();
            foreach (var row in rows.Where(item => ParseRow(item) >= 2))
            {
                var cells = ReadCells(row, ns, sharedStrings, out var formulas);
                string value;
                if (cells.TryGetValue(columns[QsCustomerWorkbookExporter.TraceHeader], out value) &&
                    string.Equals(value, traceKey, StringComparison.Ordinal))
                {
                    matchesByKey.Add(Tuple.Create(ParseRow(row), cells, formulas));
                }
            }
            if (matchesByKey.Count != 1) throw new InvalidDataException("TRACE_MODEL lookup is missing or ambiguous for TRACE_KEY " + traceKey + ".");
            var match = matchesByKey[0];
            foreach (var column in columns.Values)
                if (match.Item3.Contains(column)) throw new InvalidDataException("TRACE_MODEL identity cells must be literal values.");

            var sourceSheet = RequiredCell(match.Item2, columns["SHEET"], "TRACE_MODEL SHEET");
            var sourceRowText = RequiredCell(match.Item2, columns["ROW"], "TRACE_MODEL ROW");
            int sourceRow;
            if (!int.TryParse(sourceRowText, NumberStyles.Integer, CultureInfo.InvariantCulture, out sourceRow) || sourceRow != rowNumber)
                throw new InvalidDataException("TRACE_MODEL ROW does not match the selected business row.");
            if (!string.Equals(sourceSheet, worksheetName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("TRACE_MODEL SHEET does not match the selected business sheet.");

            var elementIds = SplitIdentity(RequiredCell(match.Item2, columns["QS3D Element ID"], "TRACE_MODEL Element ID"), false);
            var handles = SplitIdentity(RequiredCell(match.Item2, columns["CAD Handle (hex)"], "TRACE_MODEL CAD Handle"), true);
            var fingerprint = RequiredCell(match.Item2, columns["QS3D Drawing Fingerprint"], "TRACE_MODEL Drawing Fingerprint");
            var expectedTraceKey = BuildTraceKey(sourceSheet, fingerprint, elementIds, handles);
            if (!string.Equals(traceKey, expectedTraceKey, StringComparison.Ordinal))
                throw new InvalidDataException("Customer workbook TRACE_KEY does not match canonical TRACE_MODEL identity provenance.");
            return new QsCustomerWorkbookTrace(worksheetName, rowNumber, traceKey, elementIds, handles, fingerprint);
        }

        private static Tuple<XElement, XElement> SelectBusinessRowsBounded(IEnumerable<XElement> source, int targetRowNumber, int maximum)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (targetRowNumber < 2 || targetRowNumber > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(targetRowNumber), "Customer workbook data row must be between 2 and " + MaxRows + ".");
            if (maximum < 0 || maximum > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(maximum), "Customer workbook worksheet row limit is invalid.");

            XElement? header = null;
            XElement? target = null;
            var retainedCount = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (retainedCount == maximum)
                        throw new InvalidDataException("Customer workbook worksheet row count exceeds the supported limit of " + maximum.ToString(CultureInfo.InvariantCulture) + ".");
                    var row = enumerator.Current;
                    if (row == null)
                        throw new InvalidDataException("Customer workbook worksheet contains a null row element.");
                    retainedCount++;

                    var parsedRow = ParseRow(row);
                    if (parsedRow == int.MaxValue)
                        throw new InvalidDataException("Customer workbook contains an invalid row number.");
                    if (parsedRow == 1)
                    {
                        if (header != null) throw new InvalidDataException("Customer workbook row 1 is missing or duplicated.");
                        header = row;
                    }
                    if (parsedRow == targetRowNumber)
                    {
                        if (target != null) throw new InvalidDataException("Customer workbook row " + targetRowNumber + " is missing or duplicated.");
                        target = row;
                    }
                }
            }

            if (header == null) throw new InvalidDataException("Customer workbook row 1 is missing or duplicated.");
            if (target == null) throw new InvalidDataException("Customer workbook row " + targetRowNumber + " is missing or duplicated.");
            return Tuple.Create(header, target);
        }

        private static IReadOnlyList<XElement> MaterializeWorksheetRowsBounded(IEnumerable<XElement> source, int maximum)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maximum < 0 || maximum > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(maximum), "Customer workbook worksheet row limit is invalid.");

            var result = new List<XElement>();
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count == maximum)
                        throw new InvalidDataException("Customer workbook worksheet row count exceeds the supported limit of " + maximum.ToString(CultureInfo.InvariantCulture) + ".");
                    var row = enumerator.Current;
                    if (row == null)
                        throw new InvalidDataException("Customer workbook worksheet contains a null row element.");
                    result.Add(row);
                }
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string>? ReadSharedStrings(ZipArchive archive)
        {
            var entry = UniqueEntry(archive, "xl/sharedStrings.xml");
            if (entry == null) return null;

            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            if (document.Root == null || document.Root.Name != ns + "sst")
                throw new InvalidDataException("Customer workbook sharedStrings.xml has an invalid root element.");

            var result = new List<string>();
            foreach (var item in document.Root.Elements(ns + "si"))
            {
                if (result.Count == MaxSharedStrings)
                    throw new InvalidDataException("Customer workbook shared-string table exceeds the supported limit.");
                var textNodes = item.Descendants(ns + "t").ToList();
                if (textNodes.Count == 0)
                    throw new InvalidDataException("Customer workbook shared-string item contains no text.");
                result.Add(string.Concat(textNodes.Select(text => text.Value)));
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> SplitIdentity(string text, bool handles)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in text.Split(new[] { ';' }, StringSplitOptions.None))
            {
                var value = raw.Trim();
                if (value.Length == 0 || !string.Equals(raw, value, StringComparison.Ordinal) || value.Any(char.IsControl))
                    throw new InvalidDataException("TRACE_MODEL contains a malformed identity token.");
                if (handles) value = CanonicalHandle(value);
                if (!seen.Add(value)) throw new InvalidDataException("TRACE_MODEL contains duplicate identity token: " + value + ".");
                result.Add(value);
            }
            if (result.Count == 0) throw new InvalidDataException("TRACE_MODEL identity set is empty.");
            return result.AsReadOnly();
        }

        private static string CanonicalHandle(string value)
        {
            var token = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value.Substring(2) : value;
            ulong number;
            if (!ulong.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number) || number == 0UL)
                throw new InvalidDataException("TRACE_MODEL contains an invalid CAD Handle: " + value + ".");
            return number.ToString("X", CultureInfo.InvariantCulture);
        }

        private static string BuildTraceKey(string sheet, string drawingFingerprint, IEnumerable<string> elementIds, IEnumerable<string> handles)
        {
            var ids = elementIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
            var orderedHandles = handles.OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
            var raw = sheet + "\u001f" + drawingFingerprint + "\u001f" + string.Join("\u001e", ids) + "\u001f" + string.Join("\u001e", orderedHandles);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return sheet + ":" + hex;
            }
        }

        private static string RequiredCell(IReadOnlyDictionary<int, string> cells, int column, string label)
        {
            string value;
            if (!cells.TryGetValue(column, out value) || string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(label + " is missing.");
            var canonical = value.Trim();
            if (!string.Equals(value, canonical, StringComparison.Ordinal) || canonical.Any(char.IsControl))
                throw new InvalidDataException(label + " must be a canonical literal value.");
            return canonical;
        }

        private static Dictionary<string, ZipArchiveEntry> ResolveSheets(ZipArchive archive)
        {
            var workbookEntry = UniqueEntry(archive, "xl/workbook.xml") ?? throw new InvalidDataException("Customer workbook.xml is missing.");
            var relsEntry = UniqueEntry(archive, "xl/_rels/workbook.xml.rels") ?? throw new InvalidDataException("Customer workbook relationships are missing.");
            var workbook = LoadXml(workbookEntry);
            var rels = LoadXml(relsEntry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rns = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace pns = "http://schemas.openxmlformats.org/package/2006/relationships";
            var result = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in workbook.Descendants(ns + "sheet"))
            {
                var rawName = (string)sheet.Attribute("name") ?? string.Empty;
                var name = rawName.Trim();
                var rawId = (string)sheet.Attribute(rns + "id") ?? string.Empty;
                var id = rawId.Trim();
                if (name.Length == 0 || id.Length == 0 ||
                    !string.Equals(rawName, name, StringComparison.Ordinal) ||
                    !string.Equals(rawId, id, StringComparison.Ordinal) ||
                    result.ContainsKey(name))
                    throw new InvalidDataException("Customer workbook contains invalid or duplicate sheet metadata.");
                var matches = rels.Descendants(pns + "Relationship").Where(item => string.Equals((string)item.Attribute("Id"), id, StringComparison.Ordinal)).ToList();
                if (matches.Count != 1) throw new InvalidDataException("Customer workbook worksheet relationship is missing or ambiguous.");
                var type = ((string)matches[0].Attribute("Type") ?? string.Empty).Trim();
                if (!string.Equals(type, WorksheetRelationshipType, StringComparison.Ordinal)) throw new InvalidDataException("Customer workbook relationship is not a worksheet.");
                if (string.Equals((string)matches[0].Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("External worksheet relationships are not supported.");
                var rawTarget = ((string)matches[0].Attribute("Target") ?? string.Empty).Replace('\\', '/');
                var target = rawTarget.Trim();
                if (!string.Equals(rawTarget, target, StringComparison.Ordinal)) throw new InvalidDataException("Customer workbook worksheet target is invalid.");
                target = target.TrimStart('/');
                if (target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) target = target.Substring(3);
                if (target.Contains("..")) throw new InvalidDataException("Customer workbook worksheet target is invalid.");
                var entry = UniqueEntry(archive, "xl/" + target) ?? throw new InvalidDataException("Customer workbook worksheet part is missing: " + target + ".");
                result.Add(name, entry);
            }
            return result;
        }

        private static void RequireExactCustomerSheets(IEnumerable<string> names)
        {
            var expected = new HashSet<string>(new[] { QsCustomerWorkbookExporter.DgklSheet, QsCustomerWorkbookExporter.FormworkSheet, QsCustomerWorkbookExporter.DetailSheet, QsCustomerWorkbookExporter.TraceSheet }, StringComparer.OrdinalIgnoreCase);
            var actual = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            if (!actual.SetEquals(expected)) throw new InvalidDataException("Customer workbook must contain exactly DGKL, COP_PHA, CHI_TIET and TRACE_MODEL worksheets.");
        }

        private static string NormalizeBusinessSheet(string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (string.Equals(normalized, QsCustomerWorkbookExporter.DgklSheet, StringComparison.OrdinalIgnoreCase)) return QsCustomerWorkbookExporter.DgklSheet;
            if (string.Equals(normalized, QsCustomerWorkbookExporter.FormworkSheet, StringComparison.OrdinalIgnoreCase)) return QsCustomerWorkbookExporter.FormworkSheet;
            if (string.Equals(normalized, QsCustomerWorkbookExporter.DetailSheet, StringComparison.OrdinalIgnoreCase)) return QsCustomerWorkbookExporter.DetailSheet;
            throw new ArgumentException("Customer workbook locate supports only DGKL, COP_PHA or CHI_TIET.", nameof(value));
        }

        private static XElement FindUniqueRow(IEnumerable<XElement> rows, int rowNumber)
        {
            var matches = rows.Where(row => ParseRow(row) == rowNumber).Take(2).ToList();
            if (matches.Count != 1) throw new InvalidDataException("Customer workbook row " + rowNumber + " is missing or duplicated.");
            return matches[0];
        }

        private static int ParseRow(XElement row)
        {
            int value;
            return int.TryParse((string)row.Attribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= 1 && value <= MaxRows ? value : int.MaxValue;
        }

        private static Dictionary<int, string> ReadCells(XElement row, XNamespace ns, IReadOnlyList<string>? sharedStrings, out HashSet<int> formulaColumns)
        {
            var rowNumber = ParseRow(row);
            if (rowNumber == int.MaxValue) throw new InvalidDataException("Customer workbook contains an invalid row number.");
            var result = new Dictionary<int, string>();
            formulaColumns = new HashSet<int>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = ((string)cell.Attribute("r") ?? string.Empty).Trim();
                var column = ColumnIndex(reference, rowNumber);
                if (result.ContainsKey(column)) throw new InvalidDataException("Customer workbook contains duplicate cell coordinates.");
                if (cell.Element(ns + "f") != null) formulaColumns.Add(column);
                var type = ((string)cell.Attribute("t") ?? string.Empty).Trim();
                string value;
                if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
                {
                    value = string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));
                }
                else if (string.Equals(type, "s", StringComparison.Ordinal))
                {
                    if (sharedStrings == null)
                        throw new InvalidDataException("Customer workbook references shared strings but xl/sharedStrings.xml is missing.");
                    var valueNodes = cell.Elements(ns + "v").ToList();
                    if (valueNodes.Count != 1)
                        throw new InvalidDataException("Customer workbook shared-string cell must contain exactly one index value.");
                    var indexText = valueNodes[0].Value;
                    int sharedStringIndex;
                    if (!int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out sharedStringIndex) ||
                        sharedStringIndex < 0 || sharedStringIndex >= sharedStrings.Count)
                        throw new InvalidDataException("Customer workbook shared-string index is invalid or out of range.");
                    value = sharedStrings[sharedStringIndex];
                }
                else
                {
                    value = (string)cell.Element(ns + "v") ?? string.Empty;
                }
                result[column] = value;
            }
            return result;
        }

        private static int ColumnIndex(string reference, int expectedRow)
        {
            var index = 0;
            while (index < reference.Length && char.IsLetter(reference[index])) index++;
            if (index == 0 || index == reference.Length) throw new InvalidDataException("Customer workbook cell reference is invalid: " + reference + ".");
            int row;
            if (!int.TryParse(reference.Substring(index), NumberStyles.None, CultureInfo.InvariantCulture, out row) || row != expectedRow)
                throw new InvalidDataException("Customer workbook cell row does not match its containing row.");
            var column = 0;
            for (var i = 0; i < index; i++)
            {
                var letter = char.ToUpperInvariant(reference[i]);
                if (letter < 'A' || letter > 'Z') throw new InvalidDataException("Customer workbook column reference is invalid.");
                column = checked(column * 26 + letter - 'A' + 1);
            }
            if (column < 1 || column > 16384) throw new InvalidDataException("Customer workbook column exceeds the XLSX limit.");
            return column - 1;
        }

        private static ZipArchiveEntry? UniqueEntry(ZipArchive archive, string path)
        {
            var matches = archive.Entries.Where(entry => string.Equals(entry.FullName, path, StringComparison.Ordinal)).Take(2).ToList();
            if (matches.Count > 1) throw new InvalidDataException("Customer workbook contains duplicate package part: " + path + ".");
            return matches.Count == 0 ? null : matches[0];
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            if (entry.Length > MaxXmlCharacters) throw new InvalidDataException("Customer workbook XML part is too large.");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxXmlCharacters, MaxCharactersFromEntities = 0 };
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, settings)) return XDocument.Load(reader, LoadOptions.None);
        }
    }
}
