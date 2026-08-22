using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QS3D.Core.Export
{
    public static class Qs3dReviewWorkbookTraceReader
    {
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxWorksheetBytes = 32L * 1024L * 1024L;

        public static Qs3dReviewTrace Read(string path, string sheetName, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Workbook path is required.", nameof(path));
            if (rowNumber < 2) throw new ArgumentOutOfRangeException(nameof(rowNumber), "Data row must be at least 2.");
            var kind = Kind(sheetName);
            var fullPath = Path.GetFullPath(path);
            var info = new FileInfo(fullPath);
            if (!info.Exists) throw new FileNotFoundException("QS3D Review workbook was not found.", fullPath);
            if (info.Length <= 0 || info.Length > MaxWorkbookBytes) throw new InvalidDataException("QS3D Review workbook size is invalid or exceeds 128 MiB.");

            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false, Encoding.UTF8))
            {
                SheetContract(archive);
                var entryPath = kind == Qs3dReviewTraceKind.Quantity ? "xl/worksheets/sheet2.xml" : kind == Qs3dReviewTraceKind.Clash ? "xl/worksheets/sheet3.xml" : "xl/worksheets/sheet4.xml";
                var cells = Row(archive, entryPath, rowNumber);
                if (kind == Qs3dReviewTraceKind.Quantity)
                {
                    var elementId = Required(cells, "B", "QTO ElementId");
                    var handles = Split(Required(cells, "Y", "QTO CAD Handles"), "QTO CAD Handles");
                    return Trace(kind, sheetName, rowNumber, cells, "Z", "AA", "AB", new[] { elementId }, handles);
                }

                var leftId = Required(cells, "J", "ElementA ID");
                var rightId = Required(cells, "L", "ElementB ID");
                var leftHandle = Required(cells, "K", "ElementA Handle");
                var rightHandle = Required(cells, "M", "ElementB Handle");
                if (string.Equals(leftHandle, rightHandle, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("QS3D Review trace pair resolves to the same Handle twice.");
                return Trace(kind, sheetName, rowNumber, cells, "N", "O", "X", new[] { leftId, rightId }, new[] { leftHandle, rightHandle });
            }
        }

        private static Qs3dReviewTrace Trace(Qs3dReviewTraceKind kind, string sheetName, int rowNumber, IReadOnlyDictionary<string, string> cells, string fpColumn, string revisionColumn, string traceColumn, IEnumerable<string> elementIds, IEnumerable<string> handles)
        {
            var fp = Required(cells, fpColumn, "DrawingFingerprint");
            var revision = Required(cells, revisionColumn, "ModelRevision");
            var trace = Required(cells, traceColumn, Qs3dReviewWorkbookExporter.TraceHeader);
            var ids = elementIds.Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var hs = handles.Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (ids.Length == 0 || hs.Length == 0) throw new InvalidDataException("QS3D Review trace lacks semantic/CAD identity.");
            return new Qs3dReviewTrace(kind, sheetName, rowNumber, fp, revision, trace, Array.AsReadOnly(ids), Array.AsReadOnly(hs));
        }

        private static Qs3dReviewTraceKind Kind(string sheetName)
        {
            if (string.Equals(sheetName, Qs3dReviewWorkbookExporter.QuantitySheet, StringComparison.Ordinal)) return Qs3dReviewTraceKind.Quantity;
            if (string.Equals(sheetName, Qs3dReviewWorkbookExporter.ClashSheet, StringComparison.Ordinal)) return Qs3dReviewTraceKind.Clash;
            if (string.Equals(sheetName, Qs3dReviewWorkbookExporter.DuplicateSheet, StringComparison.Ordinal)) return Qs3dReviewTraceKind.Duplicate;
            throw new ArgumentException("Only 02_CHI_TIET_QTO, 03_CLASHES and 04_DUPLICATES are traceable sheets.", nameof(sheetName));
        }

        private static void SheetContract(ZipArchive archive)
        {
            var workbook = Xml(archive, "xl/workbook.xml", 512L * 1024L);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var names = workbook.Descendants(ns + "sheet").Select(e => (string?)e.Attribute("name") ?? string.Empty).ToArray();
            var expected = new[] { Qs3dReviewWorkbookExporter.SummarySheet, Qs3dReviewWorkbookExporter.QuantitySheet, Qs3dReviewWorkbookExporter.ClashSheet, Qs3dReviewWorkbookExporter.DuplicateSheet, Qs3dReviewWorkbookExporter.RulesSheet, Qs3dReviewWorkbookExporter.ModelInfoSheet };
            if (names.Length != expected.Length || !names.SequenceEqual(expected, StringComparer.Ordinal)) throw new InvalidDataException("Workbook is not the canonical six-sheet QS3D Review workbook or its sheet order was changed.");
        }

        private static Dictionary<string, string> Row(ZipArchive archive, string entryPath, int rowNumber)
        {
            var document = Xml(archive, entryPath, MaxWorksheetBytes);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var row = document.Descendants(ns + "row").SingleOrDefault(e => (string?)e.Attribute("r") == rowNumber.ToString(CultureInfo.InvariantCulture));
            if (row == null) throw new InvalidDataException("Requested QS3D Review workbook row does not exist: " + rowNumber.ToString(CultureInfo.InvariantCulture) + ".");
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? string.Empty;
                var column = new string(reference.TakeWhile(char.IsLetter).ToArray());
                if (column.Length == 0 || result.ContainsKey(column)) throw new InvalidDataException("QS3D Review workbook row contains an invalid or duplicate cell reference.");
                var type = (string?)cell.Attribute("t") ?? string.Empty;
                var value = type == "inlineStr" ? string.Concat(cell.Descendants(ns + "t").Select(t => t.Value)) : (cell.Element(ns + "v")?.Value ?? string.Empty);
                result.Add(column, value);
            }
            return result;
        }

        private static XDocument Xml(ZipArchive archive, string entryPath, long maxBytes)
        {
            var entry = archive.GetEntry(entryPath) ?? throw new InvalidDataException("QS3D Review workbook is missing " + entryPath + ".");
            if (entry.Length < 0 || entry.Length > maxBytes) throw new InvalidDataException("QS3D Review workbook XML entry is too large: " + entryPath + ".");
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = maxBytes })) return XDocument.Load(reader, LoadOptions.None);
        }

        private static string Required(IReadOnlyDictionary<string, string> cells, string column, string label)
        {
            string? raw; var value = cells.TryGetValue(column, out raw) ? (raw ?? string.Empty).Trim() : string.Empty;
            if (value.Length == 0) throw new InvalidDataException("QS3D Review workbook row is missing " + label + ".");
            return value;
        }
        private static IReadOnlyList<string> Split(string value, string label)
        {
            var parts = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (parts.Length == 0) throw new InvalidDataException(label + " contains no values.");
            return Array.AsReadOnly(parts);
        }
    }
}
