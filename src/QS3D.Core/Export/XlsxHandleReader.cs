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
        {
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            Handles = handles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            DrawingFingerprint = (drawingFingerprint ?? string.Empty).Trim();
            UsesLegacyDecimalHandles = usesLegacyDecimalHandles;
        }

        public IReadOnlyList<string> Handles { get; }
        public string DrawingFingerprint { get; }
        public bool UsesLegacyDecimalHandles { get; }
    }

    public static class XlsxHandleReader
    {
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxXmlCharacters = 64L * 1024L * 1024L;
        private static readonly Regex DecimalHandlePattern = new Regex(@"\$(\d+)", RegexOptions.CultureInvariant);
        private static readonly Regex LegacyDecimalCellPattern = new Regex(@"^\s*(?:\$\d+\s*)+$", RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> ReadHandles(string path, int rowNumber) => ReadHandleLookup(path, rowNumber).Handles;

        public static XlsxHandleLookupResult ReadHandleLookup(string path, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Excel path is required.", nameof(path));
            if (rowNumber < 1) throw new ArgumentOutOfRangeException(nameof(rowNumber));
            var fullPath = Path.GetFullPath(path);
            var file = new FileInfo(fullPath);
            if (!file.Exists) throw new FileNotFoundException("Excel workbook was not found.", fullPath);
            if (file.Length > MaxWorkbookBytes) throw new InvalidDataException("Excel workbook is too large for Handle lookup.");

            using (var archive = ZipFile.OpenRead(fullPath))
            {
                var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? archive.Entries.FirstOrDefault(x => x.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && x.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                if (sheetEntry == null) throw new InvalidDataException("Excel workbook does not contain a worksheet.");
                var sharedStrings = ReadSharedStrings(archive);
                var sheet = LoadXml(sheetEntry);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var rows = sheet.Descendants(ns + "row").ToList();
                var target = rows.FirstOrDefault(x => ParsePositiveInt((string?)x.Attribute("r")) == rowNumber);
                if (target == null) return new XlsxHandleLookupResult(Array.Empty<string>(), string.Empty, false);

                var targetCells = ReadCells(target, ns, sharedStrings);
                var handleColumns = new HashSet<int>();
                var fingerprintColumns = new HashSet<int>();
                var hasQs3dElementIdHeader = false;
                foreach (var headerRow in rows.Where(x => ParsePositiveInt((string?)x.Attribute("r")) < rowNumber).Take(10))
                    foreach (var cell in ReadCells(headerRow, ns, sharedStrings))
                    {
                        if (cell.Value.IndexOf("handle", StringComparison.OrdinalIgnoreCase) >= 0) handleColumns.Add(cell.Key);
                        if (cell.Value.IndexOf("fingerprint", StringComparison.OrdinalIgnoreCase) >= 0) fingerprintColumns.Add(cell.Key);
                        if (cell.Value.IndexOf("QS3D Element ID", StringComparison.OrdinalIgnoreCase) >= 0) hasQs3dElementIdHeader = true;
                    }

                var explicitHandles = new List<string>();
                foreach (var column in handleColumns)
                    if (targetCells.TryGetValue(column, out var value)) AddHexHandles(explicitHandles, value);

                var drawingFingerprint = ReadDrawingFingerprint(targetCells, fingerprintColumns);
                var decimalHandles = ParseDecimalHandles(targetCells.Values);
                var preferLegacy = decimalHandles.Count > 0 && string.IsNullOrWhiteSpace(drawingFingerprint) && !hasQs3dElementIdHeader;
                if (preferLegacy || (explicitHandles.Count == 0 && decimalHandles.Count > 0))
                    return new XlsxHandleLookupResult(decimalHandles, drawingFingerprint, true);
                return new XlsxHandleLookupResult(explicitHandles, drawingFingerprint, false);
            }
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
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return Array.Empty<string>();
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            return document.Descendants(ns + "si").Select(x => string.Concat(x.Descendants(ns + "t").Select(t => t.Value))).ToList();
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

        private static Dictionary<int, string> ReadCells(XElement row, XNamespace ns, IReadOnlyList<string> sharedStrings)
        {
            var result = new Dictionary<int, string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var column = ColumnIndex((string?)cell.Attribute("r"));
                if (column < 0) continue;
                var type = (string?)cell.Attribute("t") ?? string.Empty;
                string value;
                if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase)) value = string.Concat(cell.Descendants(ns + "t").Select(x => x.Value));
                else
                {
                    value = cell.Element(ns + "v")?.Value ?? string.Empty;
                    if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < sharedStrings.Count) value = sharedStrings[index];
                }
                result[column] = value;
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

        private static void AddHexHandles(ICollection<string> result, string value)
        {
            foreach (var raw in (value ?? string.Empty).Split(new[] { ';', ',', '|', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = raw.Trim();
                if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) token = token.Substring(2);
                if (long.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number) && number > 0) AddUnique(result, number.ToString("X", CultureInfo.InvariantCulture));
            }
        }

        private static void AddUnique(ICollection<string> result, string handle)
        {
            if (!result.Contains(handle, StringComparer.OrdinalIgnoreCase)) result.Add(handle);
        }

        private static int ParsePositiveInt(string? value) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0 ? number : int.MaxValue;

        private static int ColumnIndex(string? cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference)) return -1;
            var value = 0;
            var letters = 0;
            foreach (var ch in cellReference!)
            {
                if (!char.IsLetter(ch)) break;
                value = checked(value * 26 + (char.ToUpperInvariant(ch) - 'A' + 1));
                letters++;
            }
            return letters == 0 ? -1 : value - 1;
        }
    }
}
