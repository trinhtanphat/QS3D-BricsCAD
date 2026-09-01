using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace QS3D.Core.Export
{
    public static class Qs3dReviewWorkbookTraceReader
    {
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxXmlCharacters = 64L * 1024L * 1024L;
        private const int MaxRows = 1048576;
        private const int MaxSharedStrings = MaxRows;
        private const string WorksheetRelationshipType =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";

        public static Qs3dReviewTrace Read(string path, string sheetName, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Workbook path is required.", nameof(path));
            var kind = Kind(sheetName);
            if (rowNumber < 2 || rowNumber > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(rowNumber),
                    "QS3D Review data row must be between 2 and " + MaxRows.ToString(CultureInfo.InvariantCulture) + ".");

            var fullPath = Path.GetFullPath(path);
            var info = new FileInfo(fullPath);
            if (!info.Exists) throw new FileNotFoundException("QS3D Review workbook was not found.", fullPath);
            if (info.Length <= 0 || info.Length > MaxWorkbookBytes)
                throw new InvalidDataException("QS3D Review workbook size is invalid or exceeds 128 MiB.");

            using (var archive = ZipFile.OpenRead(fullPath))
            {
                IReadOnlyList<string> sheetOrder;
                var sheets = ResolveSheets(archive, out sheetOrder);
                RequireExactSheetOrder(sheetOrder);
                var sharedStrings = ReadSharedStrings(archive);
                return ReadTrace(sheets[sheetName], kind, sheetName, rowNumber, sharedStrings);
            }
        }

        private static Qs3dReviewTrace ReadTrace(
            ZipArchiveEntry entry,
            Qs3dReviewTraceKind kind,
            string sheetName,
            int rowNumber,
            IReadOnlyList<string>? sharedStrings)
        {
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XElement header;
            XElement target;
            FindRequiredRows(document, ns, rowNumber, out header, out target);
            var headers = ReadCells(header, ns, sharedStrings, out var headerFormulaColumns);
            var cells = ReadCells(target, ns, sharedStrings, out var formulaColumns);

            if (kind == Qs3dReviewTraceKind.Quantity)
            {
                var columns = RequiredColumns(headers, headerFormulaColumns, new[]
                {
                    "Element ID", "CAD Handles", "DrawingFingerprint", "ModelRevision",
                    Qs3dReviewWorkbookExporter.TraceHeader
                });
                RequireLiteralIdentity(formulaColumns, columns.Values);
                var elementId = RequiredCell(cells, columns["Element ID"], "QTO Element ID");
                var handles = SplitIdentity(
                    RequiredCell(cells, columns["CAD Handles"], "QTO CAD Handles"), "QTO CAD Handles");
                return Trace(kind, sheetName, rowNumber, elementId, cells, columns, new[] { elementId }, handles);
            }

            var itemHeader = kind == Qs3dReviewTraceKind.Clash ? "Clash ID" : "Duplicate ID";
            var pairColumns = RequiredColumns(headers, headerFormulaColumns, new[]
            {
                itemHeader, "ElementA_ID", "ElementA_Handle", "ElementB_ID", "ElementB_Handle",
                "DrawingFingerprint", "ModelRevision", Qs3dReviewWorkbookExporter.TraceHeader
            });
            RequireLiteralIdentity(formulaColumns, pairColumns.Values);
            var itemId = RequiredCell(cells, pairColumns[itemHeader], itemHeader);
            var leftId = RequiredCell(cells, pairColumns["ElementA_ID"], "ElementA ID");
            var rightId = RequiredCell(cells, pairColumns["ElementB_ID"], "ElementB ID");
            var leftHandle = RequiredCell(cells, pairColumns["ElementA_Handle"], "ElementA Handle");
            var rightHandle = RequiredCell(cells, pairColumns["ElementB_Handle"], "ElementB Handle");
            if (string.Equals(leftHandle, rightHandle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("QS3D Review trace pair resolves to the same Handle twice.");
            return Trace(
                kind, sheetName, rowNumber, itemId, cells, pairColumns,
                new[] { leftId, rightId }, new[] { leftHandle, rightHandle });
        }

        private static Qs3dReviewTrace Trace(
            Qs3dReviewTraceKind kind,
            string sheetName,
            int rowNumber,
            string itemId,
            IReadOnlyDictionary<int, string> cells,
            IReadOnlyDictionary<string, int> columns,
            IEnumerable<string> elementIds,
            IEnumerable<string> handles)
        {
            var fingerprint = RequiredCell(cells, columns["DrawingFingerprint"], "DrawingFingerprint");
            var revision = RequiredCell(cells, columns["ModelRevision"], "ModelRevision");
            var traceKey = RequiredCell(
                cells, columns[Qs3dReviewWorkbookExporter.TraceHeader], Qs3dReviewWorkbookExporter.TraceHeader);
            return new Qs3dReviewTrace(
                kind, sheetName, rowNumber, itemId, fingerprint, revision, traceKey,
                CanonicalIdentityList(elementIds, "semantic Element ID"),
                CanonicalIdentityList(handles, "CAD Handle"));
        }

        private static Qs3dReviewTraceKind Kind(string sheetName)
        {
            if (string.Equals(sheetName, Qs3dReviewWorkbookExporter.QuantitySheet, StringComparison.Ordinal))
                return Qs3dReviewTraceKind.Quantity;
            if (string.Equals(sheetName, Qs3dReviewWorkbookExporter.ClashSheet, StringComparison.Ordinal))
                return Qs3dReviewTraceKind.Clash;
            if (string.Equals(sheetName, Qs3dReviewWorkbookExporter.DuplicateSheet, StringComparison.Ordinal))
                return Qs3dReviewTraceKind.Duplicate;
            throw new ArgumentException(
                "Only 02_CHI_TIET_QTO, 03_CLASHES and 04_DUPLICATES are traceable sheets.", nameof(sheetName));
        }

        private static Dictionary<string, ZipArchiveEntry> ResolveSheets(
            ZipArchive archive,
            out IReadOnlyList<string> sheetOrder)
        {
            var workbookEntry = UniqueEntry(archive, "xl/workbook.xml")
                ?? throw new InvalidDataException("QS3D Review workbook.xml is missing.");
            var relsEntry = UniqueEntry(archive, "xl/_rels/workbook.xml.rels")
                ?? throw new InvalidDataException("QS3D Review workbook.xml.rels is missing.");
            var workbook = LoadXml(workbookEntry);
            var rels = LoadXml(relsEntry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rns = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace pns = "http://schemas.openxmlformats.org/package/2006/relationships";
            var result = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
            var order = new List<string>();

            foreach (var sheet in workbook.Descendants(ns + "sheet"))
            {
                var rawName = (string?)sheet.Attribute("name") ?? string.Empty;
                var name = rawName.Trim();
                var rawId = (string?)sheet.Attribute(rns + "id") ?? string.Empty;
                var id = rawId.Trim();
                if (name.Length == 0 || id.Length == 0 ||
                    !string.Equals(rawName, name, StringComparison.Ordinal) ||
                    !string.Equals(rawId, id, StringComparison.Ordinal) || result.ContainsKey(name))
                    throw new InvalidDataException("QS3D Review workbook contains invalid or duplicate sheet metadata.");

                var matches = rels.Descendants(pns + "Relationship")
                    .Where(item => string.Equals((string?)item.Attribute("Id"), id, StringComparison.Ordinal)).ToList();
                if (matches.Count != 1)
                    throw new InvalidDataException("QS3D Review worksheet relationship is missing or ambiguous.");
                var type = ((string?)matches[0].Attribute("Type") ?? string.Empty).Trim();
                if (!string.Equals(type, WorksheetRelationshipType, StringComparison.Ordinal))
                    throw new InvalidDataException("QS3D Review sheet relationship is not a worksheet.");
                if (string.Equals((string?)matches[0].Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("External QS3D Review worksheet relationships are not supported.");

                var rawTarget = ((string?)matches[0].Attribute("Target") ?? string.Empty).Replace('\\', '/');
                var target = rawTarget.Trim();
                if (!string.Equals(rawTarget, target, StringComparison.Ordinal) || target.Contains(".."))
                    throw new InvalidDataException("QS3D Review worksheet target is invalid.");
                target = target.TrimStart('/');
                if (target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) target = target.Substring(3);
                var entry = UniqueEntry(archive, "xl/" + target)
                    ?? throw new InvalidDataException("QS3D Review worksheet part is missing: " + target + ".");
                result.Add(name, entry);
                order.Add(name);
            }

            sheetOrder = order.AsReadOnly();
            return result;
        }

        private static void RequireExactSheetOrder(IReadOnlyList<string> names)
        {
            var expected = new[]
            {
                Qs3dReviewWorkbookExporter.SummarySheet,
                Qs3dReviewWorkbookExporter.QuantitySheet,
                Qs3dReviewWorkbookExporter.ClashSheet,
                Qs3dReviewWorkbookExporter.DuplicateSheet,
                Qs3dReviewWorkbookExporter.RulesSheet,
                Qs3dReviewWorkbookExporter.ModelInfoSheet
            };
            if (names.Count != expected.Length || !names.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidDataException(
                    "Workbook is not the canonical six-sheet QS3D Review workbook or its sheet order was changed.");
        }

        private static IReadOnlyList<string>? ReadSharedStrings(ZipArchive archive)
        {
            var entry = UniqueEntry(archive, "xl/sharedStrings.xml");
            if (entry == null) return null;
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            if (document.Root == null || document.Root.Name != ns + "sst")
                throw new InvalidDataException("QS3D Review sharedStrings.xml has an invalid root element.");
            var result = new List<string>();
            foreach (var item in document.Root.Elements(ns + "si"))
            {
                if (result.Count == MaxSharedStrings)
                    throw new InvalidDataException("QS3D Review shared-string table exceeds the supported limit.");
                var nodes = item.Descendants(ns + "t").ToList();
                if (nodes.Count == 0)
                    throw new InvalidDataException("QS3D Review shared-string item contains no text.");
                result.Add(string.Concat(nodes.Select(node => node.Value)));
            }
            return result.AsReadOnly();
        }

        private static Dictionary<string, int> RequiredColumns(
            IReadOnlyDictionary<int, string> headers,
            ISet<int> formulaColumns,
            IEnumerable<string> requiredNames)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var name in requiredNames)
            {
                var matches = headers.Where(pair => string.Equals(pair.Value, name, StringComparison.Ordinal))
                    .Select(pair => pair.Key).ToList();
                if (matches.Count != 1)
                    throw new InvalidDataException("QS3D Review sheet must contain exactly one literal " + name + " header.");
                if (formulaColumns.Contains(matches[0]))
                    throw new InvalidDataException("QS3D Review identity headers must be literal values.");
                result.Add(name, matches[0]);
            }
            return result;
        }

        private static void RequireLiteralIdentity(ISet<int> formulaColumns, IEnumerable<int> columns)
        {
            if (columns.Any(formulaColumns.Contains))
                throw new InvalidDataException("QS3D Review identity cells must be literal values, not formulas.");
        }

        private static void FindRequiredRows(
            XDocument document,
            XNamespace ns,
            int rowNumber,
            out XElement header,
            out XElement target)
        {
            XElement? headerCandidate = null;
            XElement? targetCandidate = null;
            var headerMatches = 0;
            var targetMatches = 0;
            foreach (var row in document.Descendants(ns + "row"))
            {
                var declaredRow = ParseRow(row);
                if (declaredRow == int.MaxValue)
                    throw new InvalidDataException("QS3D Review workbook contains an invalid row number.");
                if (declaredRow == 1)
                {
                    headerMatches++;
                    if (headerCandidate == null) headerCandidate = row;
                }
                if (declaredRow == rowNumber)
                {
                    targetMatches++;
                    if (targetCandidate == null) targetCandidate = row;
                }
            }
            if (headerMatches != 1)
                throw new InvalidDataException("QS3D Review workbook row 1 is missing or duplicated.");
            if (targetMatches != 1)
                throw new InvalidDataException("QS3D Review workbook row " +
                    rowNumber.ToString(CultureInfo.InvariantCulture) + " is missing or duplicated.");
            header = headerCandidate!;
            target = targetCandidate!;
        }

        private static int ParseRow(XElement row)
        {
            int value;
            return int.TryParse((string?)row.Attribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out value) &&
                   value >= 1 && value <= MaxRows ? value : int.MaxValue;
        }

        private static Dictionary<int, string> ReadCells(
            XElement row,
            XNamespace ns,
            IReadOnlyList<string>? sharedStrings,
            out HashSet<int> formulaColumns)
        {
            var rowNumber = ParseRow(row);
            if (rowNumber == int.MaxValue)
                throw new InvalidDataException("QS3D Review workbook contains an invalid row number.");
            var result = new Dictionary<int, string>();
            formulaColumns = new HashSet<int>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = ((string?)cell.Attribute("r") ?? string.Empty).Trim();
                var column = ColumnIndex(reference, rowNumber);
                if (result.ContainsKey(column))
                    throw new InvalidDataException("QS3D Review workbook contains duplicate cell coordinates.");
                if (cell.Element(ns + "f") != null) formulaColumns.Add(column);
                var type = ((string?)cell.Attribute("t") ?? string.Empty).Trim();
                string value;
                if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
                {
                    value = string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));
                }
                else if (string.Equals(type, "s", StringComparison.Ordinal))
                {
                    if (sharedStrings == null)
                        throw new InvalidDataException(
                            "QS3D Review workbook references shared strings but xl/sharedStrings.xml is missing.");
                    var values = cell.Elements(ns + "v").ToList();
                    int index;
                    if (values.Count != 1 ||
                        !int.TryParse(values[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out index) ||
                        index < 0 || index >= sharedStrings.Count)
                        throw new InvalidDataException("QS3D Review shared-string index is invalid or out of range.");
                    value = sharedStrings[index];
                }
                else
                {
                    value = (string?)cell.Element(ns + "v") ?? string.Empty;
                }
                result.Add(column, value);
            }
            return result;
        }

        private static int ColumnIndex(string reference, int expectedRow)
        {
            var index = 0;
            while (index < reference.Length && char.IsLetter(reference[index])) index++;
            if (index == 0 || index == reference.Length)
                throw new InvalidDataException("QS3D Review cell reference is invalid: " + reference + ".");
            int row;
            if (!int.TryParse(reference.Substring(index), NumberStyles.None, CultureInfo.InvariantCulture, out row) ||
                row != expectedRow)
                throw new InvalidDataException("QS3D Review cell row does not match its containing row.");
            var column = 0;
            for (var i = 0; i < index; i++)
            {
                var letter = char.ToUpperInvariant(reference[i]);
                if (letter < 'A' || letter > 'Z')
                    throw new InvalidDataException("QS3D Review column reference is invalid.");
                column = checked(column * 26 + letter - 'A' + 1);
            }
            if (column < 1 || column > 16384)
                throw new InvalidDataException("QS3D Review column exceeds the XLSX limit.");
            return column - 1;
        }

        private static IReadOnlyList<string> CanonicalIdentityList(IEnumerable<string> source, string label)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in source)
            {
                var value = (raw ?? string.Empty).Trim();
                if (value.Length == 0 || !string.Equals(raw, value, StringComparison.Ordinal) || value.Any(char.IsControl))
                    throw new InvalidDataException("QS3D Review " + label + " is not canonical.");
                if (!seen.Add(value))
                    throw new InvalidDataException("QS3D Review " + label + " is duplicated: " + value + ".");
                result.Add(value);
            }
            if (result.Count == 0) throw new InvalidDataException("QS3D Review " + label + " set is empty.");
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> SplitIdentity(string value, string label)
        {
            return CanonicalIdentityList(value.Split(new[] { ';' }, StringSplitOptions.None), label);
        }

        private static string RequiredCell(IReadOnlyDictionary<int, string> cells, int column, string label)
        {
            string value;
            if (!cells.TryGetValue(column, out value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("QS3D Review workbook row is missing " + label + ".");
            var canonical = value.Trim();
            if (!string.Equals(value, canonical, StringComparison.Ordinal) || canonical.Any(char.IsControl))
                throw new InvalidDataException("QS3D Review " + label + " must be a canonical literal value.");
            return canonical;
        }

        private static ZipArchiveEntry? UniqueEntry(ZipArchive archive, string path)
        {
            var matches = archive.Entries.Where(entry => string.Equals(entry.FullName, path, StringComparison.Ordinal))
                .Take(2).ToList();
            if (matches.Count > 1)
                throw new InvalidDataException("QS3D Review workbook contains duplicate package part: " + path + ".");
            return matches.Count == 0 ? null : matches[0];
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            if (entry.Length < 0 || entry.Length > MaxXmlCharacters)
                throw new InvalidDataException("QS3D Review workbook XML part is too large: " + entry.FullName + ".");
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxXmlCharacters,
                MaxCharactersFromEntities = 0
            };
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, settings))
                return XDocument.Load(reader, LoadOptions.None);
        }
    }
}