using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Coordination;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    /// <summary>
    /// Stable duplicate-pair projection. Semantic element ids are the durable pair identity;
    /// CAD handles are current-drawing locate evidence only.
    /// </summary>
    public sealed class CoordinationDuplicateExportRow
    {
        private const DuplicateMatchKind KnownKinds =
            DuplicateMatchKind.ExactGeometry | DuplicateMatchKind.NearGeometry | DuplicateMatchKind.SemanticIdentity;

        internal CoordinationDuplicateExportRow(
            string duplicateId,
            DuplicateMatchKind matchKinds,
            string floor,
            string leftElementId,
            string leftHandle,
            string leftCategory,
            string rightElementId,
            string rightHandle,
            string rightCategory,
            string ruleId,
            string drawingFingerprint,
            string comment)
        {
            DuplicateId = duplicateId;
            MatchKinds = matchKinds;
            Floor = floor;
            LeftElementId = leftElementId;
            LeftHandle = leftHandle;
            LeftCategory = leftCategory;
            RightElementId = rightElementId;
            RightHandle = rightHandle;
            RightCategory = rightCategory;
            RuleId = ruleId;
            DrawingFingerprint = drawingFingerprint;
            Comment = comment;
        }

        public string DuplicateId { get; }
        public DuplicateMatchKind MatchKinds { get; }
        public string MatchKindsText => FormatMatchKinds(MatchKinds);
        public string Floor { get; }
        public string LeftElementId { get; }
        public string LeftHandle { get; }
        public string LeftCategory { get; }
        public string RightElementId { get; }
        public string RightHandle { get; }
        public string RightCategory { get; }
        public string RuleId { get; }
        public string DrawingFingerprint { get; }
        public string Comment { get; }

        public static CoordinationDuplicateExportRow Create(
            string drawingFingerprint,
            string leftElementId,
            string leftHandle,
            string rightElementId,
            string rightHandle,
            DuplicateMatchKind matchKinds,
            string leftCategory = "",
            string rightCategory = "",
            string floor = "",
            string comment = "")
        {
            var fingerprint = CoordinationWorkbookIdentity.Required(drawingFingerprint, "Drawing Fingerprint");
            var leftId = CoordinationWorkbookIdentity.Required(leftElementId, "Duplicate Element A ID");
            var rightId = CoordinationWorkbookIdentity.Required(rightElementId, "Duplicate Element B ID");
            var left = CoordinationWorkbookIdentity.CanonicalHandle(leftHandle);
            var right = CoordinationWorkbookIdentity.CanonicalHandle(rightHandle);
            var leftKind = CoordinationWorkbookIdentity.Optional(leftCategory, "Duplicate Element A Category");
            var rightKind = CoordinationWorkbookIdentity.Optional(rightCategory, "Duplicate Element B Category");
            ValidateMatchKinds(matchKinds);

            if (string.Equals(leftId, rightId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Duplicate pair must reference two different semantic Element IDs.");
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Duplicate pair must reference two different live CAD Handles.");

            if (CompareCanonical(leftId, rightId) > 0)
            {
                Swap(ref leftId, ref rightId);
                Swap(ref left, ref right);
                Swap(ref leftKind, ref rightKind);
            }

            const string ruleId = "QS3D_DUPLICATE_V1";
            var duplicateId = CoordinationDuplicateIdentity.Create(
                fingerprint, ruleId, leftId, rightId, matchKinds);
            return new CoordinationDuplicateExportRow(
                duplicateId,
                matchKinds,
                CoordinationWorkbookIdentity.Optional(floor, "Duplicate Floor"),
                leftId,
                left,
                leftKind,
                rightId,
                right,
                rightKind,
                ruleId,
                fingerprint,
                CoordinationWorkbookIdentity.Optional(comment, "Duplicate Comment"));
        }

        internal static string FormatMatchKinds(DuplicateMatchKind kinds)
        {
            ValidateMatchKinds(kinds);
            var values = new List<string>(3);
            if ((kinds & DuplicateMatchKind.ExactGeometry) != 0) values.Add("ExactGeometry");
            if ((kinds & DuplicateMatchKind.NearGeometry) != 0) values.Add("NearGeometry");
            if ((kinds & DuplicateMatchKind.SemanticIdentity) != 0) values.Add("SemanticIdentity");
            return string.Join("|", values);
        }

        internal static DuplicateMatchKind ParseMatchKinds(string value)
        {
            var text = CoordinationWorkbookIdentity.Required(value, "MATCH_KINDS");
            var result = DuplicateMatchKind.None;
            foreach (var token in text.Split('|'))
            {
                if (string.Equals(token, "ExactGeometry", StringComparison.Ordinal)) result |= DuplicateMatchKind.ExactGeometry;
                else if (string.Equals(token, "NearGeometry", StringComparison.Ordinal)) result |= DuplicateMatchKind.NearGeometry;
                else if (string.Equals(token, "SemanticIdentity", StringComparison.Ordinal)) result |= DuplicateMatchKind.SemanticIdentity;
                else throw new InvalidDataException("Coordination workbook contains an unknown duplicate match kind: " + token + ".");
            }
            ValidateMatchKinds(result);
            if (!string.Equals(text, FormatMatchKinds(result), StringComparison.Ordinal))
                throw new InvalidDataException("MATCH_KINDS must use canonical ordering without duplicates.");
            return result;
        }

        private static void ValidateMatchKinds(DuplicateMatchKind kinds)
        {
            if (kinds == DuplicateMatchKind.None || (kinds & ~KnownKinds) != 0)
                throw new InvalidDataException("Duplicate match kinds must contain only known non-empty evidence flags.");
        }

        private static int CompareCanonical(string left, string right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left, right);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left, right);
        }

        private static void Swap(ref string left, ref string right)
        {
            var value = left;
            left = right;
            right = value;
        }
    }

    public static class CoordinationDuplicateIdentity
    {
        public static string Create(
            string drawingFingerprint,
            string ruleId,
            string leftElementId,
            string rightElementId,
            DuplicateMatchKind matchKinds)
        {
            var fingerprint = CoordinationWorkbookIdentity.Required(drawingFingerprint, "Drawing Fingerprint");
            var rule = CoordinationWorkbookIdentity.Required(ruleId, "Rule ID");
            var left = CoordinationWorkbookIdentity.Required(leftElementId, "Duplicate Element A ID");
            var right = CoordinationWorkbookIdentity.Required(rightElementId, "Duplicate Element B ID");
            var evidence = CoordinationDuplicateExportRow.FormatMatchKinds(matchKinds);
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left, right);
            if (compare > 0 || (compare == 0 && StringComparer.Ordinal.Compare(left, right) > 0))
            {
                var value = left;
                left = right;
                right = value;
            }
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Duplicate identity requires two different semantic Element IDs.");
            return "DUP:" + CoordinationWorkbookIdentity.Sha256Hex(
                fingerprint + "\u001f" + rule + "\u001f" + left + "\u001f" + right + "\u001f" + evidence);
        }
    }

    /// <summary>
    /// Three-sheet coordination workbook used by current clash/duplicate commands.
    /// The older two-sheet exporter remains readable for backward compatibility but new exports
    /// use CLASHES + DUPLICATES + TRACE_MODEL so one artifact can carry both coordination kinds.
    /// </summary>
    public static class CoordinationUnifiedWorkbookExporter
    {
        public const string ClashSheet = "CLASHES";
        public const string DuplicateSheet = "DUPLICATES";
        public const string TraceSheet = "TRACE_MODEL";
        public const string TraceHeader = "TRACE_KEY";
        private const int MaxRows = 1048575;

        public static void Export(
            string path,
            IReadOnlyList<CoordinationClashExportRow> clashes,
            IReadOnlyList<CoordinationDuplicateExportRow> duplicates)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (clashes == null) throw new ArgumentNullException(nameof(clashes));
            if (duplicates == null) throw new ArgumentNullException(nameof(duplicates));
            if (clashes.Count == 0 && duplicates.Count == 0)
                throw new InvalidDataException("Coordination workbook requires at least one clash or duplicate row.");
            if (clashes.Count > MaxRows || duplicates.Count > MaxRows)
                throw new InvalidDataException("Coordination workbook exceeds the Excel row limit.");

            var clashRows = SnapshotClashes(clashes);
            var duplicateRows = SnapshotDuplicates(duplicates);
            RequireOneDrawing(clashRows, duplicateRows);

            var traces = new List<TraceProjection>(clashRows.Count + duplicateRows.Count);
            var clashXml = BuildClashSheet(clashRows, traces);
            var duplicateXml = BuildDuplicateSheet(duplicateRows, traces);
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
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", clashXml);
                    WriteEntry(archive, "xl/worksheets/sheet2.xml", duplicateXml);
                    WriteEntry(archive, "xl/worksheets/sheet3.xml", traceXml);
                }
                XlsxPackageValidator.Validate(
                    tempPath,
                    "[Content_Types].xml",
                    "xl/workbook.xml",
                    "xl/_rels/workbook.xml.rels",
                    "xl/worksheets/sheet1.xml",
                    "xl/worksheets/sheet2.xml",
                    "xl/worksheets/sheet3.xml");
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static List<CoordinationClashExportRow> SnapshotClashes(IReadOnlyList<CoordinationClashExportRow> source)
        {
            var result = new List<CoordinationClashExportRow>(source.Count);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in source)
            {
                if (row == null) throw new InvalidDataException("Coordination workbook contains a null clash row.");
                var expected = CoordinationClashIdentity.Create(row.DrawingFingerprint, row.RuleId, row.LeftHandle, row.RightHandle);
                if (!string.Equals(expected, row.ClashId, StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination ClashId does not match canonical pair identity.");
                if (!ids.Add(row.ClashId)) throw new InvalidDataException("Duplicate ClashId: " + row.ClashId + ".");
                result.Add(row);
            }
            result.Sort((a, b) => StringComparer.Ordinal.Compare(a.ClashId, b.ClashId));
            return result;
        }

        private static List<CoordinationDuplicateExportRow> SnapshotDuplicates(IReadOnlyList<CoordinationDuplicateExportRow> source)
        {
            var result = new List<CoordinationDuplicateExportRow>(source.Count);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in source)
            {
                if (row == null) throw new InvalidDataException("Coordination workbook contains a null duplicate row.");
                var expected = CoordinationDuplicateIdentity.Create(
                    row.DrawingFingerprint, row.RuleId, row.LeftElementId, row.RightElementId, row.MatchKinds);
                if (!string.Equals(expected, row.DuplicateId, StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination DuplicateId does not match canonical semantic pair identity.");
                if (!ids.Add(row.DuplicateId)) throw new InvalidDataException("Duplicate DuplicateId: " + row.DuplicateId + ".");
                result.Add(row);
            }
            result.Sort((a, b) => StringComparer.Ordinal.Compare(a.DuplicateId, b.DuplicateId));
            return result;
        }

        private static void RequireOneDrawing(
            IReadOnlyList<CoordinationClashExportRow> clashes,
            IReadOnlyList<CoordinationDuplicateExportRow> duplicates)
        {
            string? fingerprint = null;
            foreach (var value in clashes.Select(row => row.DrawingFingerprint).Concat(duplicates.Select(row => row.DrawingFingerprint)))
            {
                var canonical = CoordinationWorkbookIdentity.Required(value, "Drawing Fingerprint");
                if (fingerprint == null) fingerprint = canonical;
                else if (!string.Equals(fingerprint, canonical, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Coordination workbook rows contain conflicting drawing fingerprints.");
            }
        }

        private static string BuildClashSheet(IReadOnlyList<CoordinationClashExportRow> rows, ICollection<TraceProjection> traces)
        {
            var headers = new[]
            {
                "STT", "CLASH_ID", "TYPE", "SEVERITY", "STATUS", "FLOOR",
                "ELEMENT_A_ID", "ELEMENT_A_HANDLE", "ELEMENT_A_CATEGORY",
                "ELEMENT_B_ID", "ELEMENT_B_HANDLE", "ELEMENT_B_CATEGORY",
                "RULE_ID", "DRAWING_FINGERPRINT", "COMMENT", TraceHeader
            };
            var data = new List<IReadOnlyList<string>>(rows.Count);
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = index + 2;
                var traceKey = CoordinationWorkbookIdentity.BuildTraceKey(
                    row.ClashId, row.DrawingFingerprint, row.RuleId, row.LeftHandle, row.RightHandle, ClashSheet);
                traces.Add(new TraceProjection(traceKey, ClashSheet, excelRow, row.ClashId, row.LeftHandle, row.RightHandle, row.DrawingFingerprint, row.RuleId));
                data.Add(new[]
                {
                    (index + 1).ToString(CultureInfo.InvariantCulture), row.ClashId, row.Type, row.Severity, row.Status, row.Floor,
                    row.LeftElementId, row.LeftHandle, row.LeftCategory, row.RightElementId, row.RightHandle, row.RightCategory,
                    row.RuleId, row.DrawingFingerprint, row.Comment, traceKey
                });
            }
            return BuildSheet(headers, data);
        }

        private static string BuildDuplicateSheet(IReadOnlyList<CoordinationDuplicateExportRow> rows, ICollection<TraceProjection> traces)
        {
            var headers = new[]
            {
                "STT", "DUPLICATE_ID", "MATCH_KINDS", "FLOOR",
                "ELEMENT_A_ID", "ELEMENT_A_HANDLE", "ELEMENT_A_CATEGORY",
                "ELEMENT_B_ID", "ELEMENT_B_HANDLE", "ELEMENT_B_CATEGORY",
                "RULE_ID", "DRAWING_FINGERPRINT", "COMMENT", TraceHeader
            };
            var data = new List<IReadOnlyList<string>>(rows.Count);
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = index + 2;
                var traceKey = CoordinationWorkbookIdentity.BuildTraceKey(
                    row.DuplicateId, row.DrawingFingerprint, row.RuleId, row.LeftHandle, row.RightHandle, DuplicateSheet);
                traces.Add(new TraceProjection(traceKey, DuplicateSheet, excelRow, row.DuplicateId, row.LeftHandle, row.RightHandle, row.DrawingFingerprint, row.RuleId));
                data.Add(new[]
                {
                    (index + 1).ToString(CultureInfo.InvariantCulture), row.DuplicateId, row.MatchKindsText, row.Floor,
                    row.LeftElementId, row.LeftHandle, row.LeftCategory, row.RightElementId, row.RightHandle, row.RightCategory,
                    row.RuleId, row.DrawingFingerprint, row.Comment, traceKey
                });
            }
            return BuildSheet(headers, data);
        }

        private static string BuildTraceSheet(IReadOnlyList<TraceProjection> traces)
        {
            var headers = new[] { TraceHeader, "SHEET", "ROW", "ITEM_ID", "LEFT_HANDLE", "RIGHT_HANDLE", "DRAWING_FINGERPRINT", "RULE_ID" };
            var data = new List<IReadOnlyList<string>>(traces.Count);
            foreach (var trace in traces)
            {
                data.Add(new[]
                {
                    trace.TraceKey, trace.Sheet, trace.Row.ToString(CultureInfo.InvariantCulture), trace.ItemId,
                    trace.LeftHandle, trace.RightHandle, trace.DrawingFingerprint, trace.RuleId
                });
            }
            return BuildSheet(headers, data);
        }

        private static string BuildSheet(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            AppendRow(sb, 1, headers);
            for (var index = 0; index < rows.Count; index++) AppendRow(sb, index + 2, rows[index]);
            sb.Append("</sheetData></worksheet>");
            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, int rowNumber, IReadOnlyList<string> values)
        {
            sb.Append("<row r=\"").Append(rowNumber.ToString(CultureInfo.InvariantCulture)).Append("\">");
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index] ?? string.Empty;
                if (value.Length > 32767) throw new InvalidDataException("Coordination workbook cell exceeds the Excel text limit.");
                sb.Append("<c r=\"").Append(Cell(index, rowNumber)).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">")
                  .Append(SecurityElement.Escape(value) ?? string.Empty)
                  .Append("</t></is></c>");
            }
            sb.Append("</row>");
        }

        private static string Cell(int column, int row)
        {
            var n = column + 1;
            var name = string.Empty;
            while (n > 0)
            {
                n--;
                name = (char)('A' + (n % 26)) + name;
                n /= 26;
            }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private sealed class TraceProjection
        {
            internal TraceProjection(string traceKey, string sheet, int row, string itemId, string leftHandle, string rightHandle, string drawingFingerprint, string ruleId)
            {
                TraceKey = traceKey; Sheet = sheet; Row = row; ItemId = itemId;
                LeftHandle = leftHandle; RightHandle = rightHandle; DrawingFingerprint = drawingFingerprint; RuleId = ruleId;
            }
            internal string TraceKey { get; }
            internal string Sheet { get; }
            internal int Row { get; }
            internal string ItemId { get; }
            internal string LeftHandle { get; }
            internal string RightHandle { get; }
            internal string DrawingFingerprint { get; }
            internal string RuleId { get; }
        }

        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet3.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"CLASHES\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"DUPLICATES\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"TRACE_MODEL\" sheetId=\"3\" r:id=\"rId3\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet3.xml\"/></Relationships>";
    }

    public sealed class CoordinationUnifiedWorkbookTrace
    {
        internal CoordinationUnifiedWorkbookTrace(int rowNumber, string sheet, string itemId, string leftHandle, string rightHandle, string drawingFingerprint, string ruleId)
        {
            RowNumber = rowNumber; Sheet = sheet; ItemId = itemId; LeftHandle = leftHandle; RightHandle = rightHandle;
            DrawingFingerprint = drawingFingerprint; RuleId = ruleId;
        }
        public int RowNumber { get; }
        public string Sheet { get; }
        public string ItemId { get; }
        public string ClashId => string.Equals(Sheet, CoordinationUnifiedWorkbookExporter.ClashSheet, StringComparison.Ordinal) ? ItemId : string.Empty;
        public string DuplicateId => string.Equals(Sheet, CoordinationUnifiedWorkbookExporter.DuplicateSheet, StringComparison.Ordinal) ? ItemId : string.Empty;
        public string LeftHandle { get; }
        public string RightHandle { get; }
        public string DrawingFingerprint { get; }
        public string RuleId { get; }
    }

    public static class CoordinationUnifiedWorkbookTraceReader
    {
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxXmlCharacters = 64L * 1024L * 1024L;
        private const int MaxRows = 1048576;
        private const string WorksheetRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";

        public static CoordinationUnifiedWorkbookTrace ReadClash(string path, int rowNumber)
        {
            return Read(path, CoordinationUnifiedWorkbookExporter.ClashSheet, rowNumber);
        }

        public static CoordinationUnifiedWorkbookTrace ReadDuplicate(string path, int rowNumber)
        {
            return Read(path, CoordinationUnifiedWorkbookExporter.DuplicateSheet, rowNumber);
        }

        private static CoordinationUnifiedWorkbookTrace Read(string path, string sourceSheet, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Excel path is required.", nameof(path));
            if (rowNumber < 2 || rowNumber > MaxRows) throw new ArgumentOutOfRangeException(nameof(rowNumber));
            var fullPath = Path.GetFullPath(path);
            var info = new FileInfo(fullPath);
            if (!info.Exists) throw new FileNotFoundException("Coordination workbook was not found.", fullPath);
            if (info.Length > MaxWorkbookBytes) throw new InvalidDataException("Coordination workbook is too large for trace lookup.");

            using (var archive = ZipFile.OpenRead(fullPath))
            {
                var sheets = ResolveSheets(archive);
                var expected = new HashSet<string>(new[]
                {
                    CoordinationUnifiedWorkbookExporter.ClashSheet,
                    CoordinationUnifiedWorkbookExporter.DuplicateSheet,
                    CoordinationUnifiedWorkbookExporter.TraceSheet
                }, StringComparer.OrdinalIgnoreCase);
                if (!expected.SetEquals(sheets.Keys))
                    throw new InvalidDataException("Unified coordination workbook must contain exactly CLASHES, DUPLICATES and TRACE_MODEL worksheets.");

                var sharedStrings = ReadSharedStrings(archive);
                var source = ReadSourceRow(sheets[sourceSheet], sourceSheet, rowNumber, sharedStrings);
                var trace = ReadTraceRow(sheets[CoordinationUnifiedWorkbookExporter.TraceSheet], source.TraceKey, sharedStrings);
                if (!string.Equals(trace.Sheet, sourceSheet, StringComparison.Ordinal) || trace.Row != rowNumber ||
                    !string.Equals(trace.ItemId, source.ItemId, StringComparison.Ordinal) ||
                    !string.Equals(trace.LeftHandle, source.LeftHandle, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(trace.RightHandle, source.RightHandle, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(trace.DrawingFingerprint, source.DrawingFingerprint, StringComparison.Ordinal) ||
                    !string.Equals(trace.RuleId, source.RuleId, StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination source row does not match TRACE_MODEL provenance.");

                if (string.Equals(sourceSheet, CoordinationUnifiedWorkbookExporter.ClashSheet, StringComparison.Ordinal))
                {
                    var expectedId = CoordinationClashIdentity.Create(source.DrawingFingerprint, source.RuleId, source.LeftHandle, source.RightHandle);
                    if (!string.Equals(expectedId, source.ItemId, StringComparison.Ordinal))
                        throw new InvalidDataException("CLASH_ID does not match canonical pair identity.");
                }
                else
                {
                    var expectedId = CoordinationDuplicateIdentity.Create(source.DrawingFingerprint, source.RuleId, source.LeftElementId, source.RightElementId, source.MatchKinds);
                    if (!string.Equals(expectedId, source.ItemId, StringComparison.Ordinal))
                        throw new InvalidDataException("DUPLICATE_ID does not match canonical semantic pair identity.");
                }

                var expectedTrace = CoordinationWorkbookIdentity.BuildTraceKey(
                    source.ItemId, source.DrawingFingerprint, source.RuleId, source.LeftHandle, source.RightHandle, sourceSheet);
                if (!string.Equals(expectedTrace, source.TraceKey, StringComparison.Ordinal))
                    throw new InvalidDataException("TRACE_KEY does not match canonical source provenance.");
                return new CoordinationUnifiedWorkbookTrace(rowNumber, sourceSheet, source.ItemId, source.LeftHandle, source.RightHandle, source.DrawingFingerprint, source.RuleId);
            }
        }

        private static SourceProjection ReadSourceRow(ZipArchiveEntry entry, string sheet, int rowNumber, IReadOnlyList<string> sharedStrings)
        {
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var selected = SelectUnifiedRowsBounded(document, ns, rowNumber);
            if (ParseRow(selected.Header) != 1 || selected.Target == null || ParseRow(selected.Target) != rowNumber)
                throw new InvalidDataException("Unified coordination workbook selected row metadata changed during lookup.");
            var header = ReadCells(selected.Header, ns, sharedStrings, out var headerFormulas);
            var target = ReadCells(selected.Target, ns, sharedStrings, out var targetFormulas);
            if (string.Equals(sheet, CoordinationUnifiedWorkbookExporter.ClashSheet, StringComparison.Ordinal))
            {
                var columns = RequiredColumns(header, headerFormulas, new[] { "CLASH_ID", "ELEMENT_A_HANDLE", "ELEMENT_B_HANDLE", "RULE_ID", "DRAWING_FINGERPRINT", CoordinationUnifiedWorkbookExporter.TraceHeader });
                RequireLiteral(targetFormulas, columns.Values);
                return new SourceProjection(
                    RequiredCell(target, columns["CLASH_ID"], "CLASH_ID"), string.Empty, string.Empty, DuplicateMatchKind.None,
                    CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(target, columns["ELEMENT_A_HANDLE"], "ELEMENT_A_HANDLE")),
                    CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(target, columns["ELEMENT_B_HANDLE"], "ELEMENT_B_HANDLE")),
                    RequiredCell(target, columns["DRAWING_FINGERPRINT"], "DRAWING_FINGERPRINT"),
                    RequiredCell(target, columns["RULE_ID"], "RULE_ID"),
                    RequiredCell(target, columns[CoordinationUnifiedWorkbookExporter.TraceHeader], CoordinationUnifiedWorkbookExporter.TraceHeader));
            }
            else
            {
                var columns = RequiredColumns(header, headerFormulas, new[] { "DUPLICATE_ID", "MATCH_KINDS", "ELEMENT_A_ID", "ELEMENT_A_HANDLE", "ELEMENT_B_ID", "ELEMENT_B_HANDLE", "RULE_ID", "DRAWING_FINGERPRINT", CoordinationUnifiedWorkbookExporter.TraceHeader });
                RequireLiteral(targetFormulas, columns.Values);
                return new SourceProjection(
                    RequiredCell(target, columns["DUPLICATE_ID"], "DUPLICATE_ID"),
                    RequiredCell(target, columns["ELEMENT_A_ID"], "ELEMENT_A_ID"),
                    RequiredCell(target, columns["ELEMENT_B_ID"], "ELEMENT_B_ID"),
                    CoordinationDuplicateExportRow.ParseMatchKinds(RequiredCell(target, columns["MATCH_KINDS"], "MATCH_KINDS")),
                    CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(target, columns["ELEMENT_A_HANDLE"], "ELEMENT_A_HANDLE")),
                    CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(target, columns["ELEMENT_B_HANDLE"], "ELEMENT_B_HANDLE")),
                    RequiredCell(target, columns["DRAWING_FINGERPRINT"], "DRAWING_FINGERPRINT"),
                    RequiredCell(target, columns["RULE_ID"], "RULE_ID"),
                    RequiredCell(target, columns[CoordinationUnifiedWorkbookExporter.TraceHeader], CoordinationUnifiedWorkbookExporter.TraceHeader));
            }
        }

        private static TraceProjection ReadTraceRow(ZipArchiveEntry entry, string traceKey, IReadOnlyList<string> sharedStrings)
        {
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var selected = SelectUnifiedRowsBounded(document, ns, null);
            if (ParseRow(selected.Header) != 1)
                throw new InvalidDataException("Unified coordination workbook TRACE_MODEL header metadata changed during lookup.");
            var header = ReadCells(selected.Header, ns, sharedStrings, out var headerFormulas);
            var columns = RequiredColumns(header, headerFormulas, new[] { CoordinationUnifiedWorkbookExporter.TraceHeader, "SHEET", "ROW", "ITEM_ID", "LEFT_HANDLE", "RIGHT_HANDLE", "DRAWING_FINGERPRINT", "RULE_ID" });

            Dictionary<int, string>? matchedCells = null;
            HashSet<int>? matchedFormulas = null;
            foreach (var row in document.Descendants(ns + "row"))
            {
                var declaredRow = ParseRow(row);
                if (declaredRow < 2) continue;
                var cells = ReadCells(row, ns, sharedStrings, out var formulas);
                string value;
                if (!cells.TryGetValue(columns[CoordinationUnifiedWorkbookExporter.TraceHeader], out value) || !string.Equals(value, traceKey, StringComparison.Ordinal))
                    continue;
                if (matchedCells != null)
                    throw new InvalidDataException("TRACE_MODEL lookup is missing or ambiguous for TRACE_KEY " + traceKey + ".");
                matchedCells = cells;
                matchedFormulas = formulas;
            }
            if (matchedCells == null || matchedFormulas == null)
                throw new InvalidDataException("TRACE_MODEL lookup is missing or ambiguous for TRACE_KEY " + traceKey + ".");
            RequireLiteral(matchedFormulas, columns.Values);
            int rowNumber;
            if (!int.TryParse(RequiredCell(matchedCells, columns["ROW"], "TRACE_MODEL ROW"), NumberStyles.Integer, CultureInfo.InvariantCulture, out rowNumber) || rowNumber < 2 || rowNumber > MaxRows)
                throw new InvalidDataException("TRACE_MODEL ROW is invalid.");
            return new TraceProjection(
                RequiredCell(matchedCells, columns["SHEET"], "TRACE_MODEL SHEET"), rowNumber,
                RequiredCell(matchedCells, columns["ITEM_ID"], "TRACE_MODEL ITEM_ID"),
                CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(matchedCells, columns["LEFT_HANDLE"], "TRACE_MODEL LEFT_HANDLE")),
                CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(matchedCells, columns["RIGHT_HANDLE"], "TRACE_MODEL RIGHT_HANDLE")),
                RequiredCell(matchedCells, columns["DRAWING_FINGERPRINT"], "TRACE_MODEL DRAWING_FINGERPRINT"),
                RequiredCell(matchedCells, columns["RULE_ID"], "TRACE_MODEL RULE_ID"));
        }

        private static SelectedUnifiedRows SelectUnifiedRowsBounded(XDocument document, XNamespace ns, int? rowNumber)
        {
            XElement? header = null;
            XElement? target = null;
            foreach (var row in document.Descendants(ns + "row"))
            {
                var declaredRow = ParseRow(row);
                if (declaredRow == 1)
                {
                    if (header != null) throw new InvalidDataException("Coordination workbook row 1 is duplicated.");
                    header = row;
                }
                if (rowNumber.HasValue && declaredRow == rowNumber.Value)
                {
                    if (target != null) throw new InvalidDataException("Coordination workbook target row is duplicated.");
                    target = row;
                }
            }
            if (header == null) throw new InvalidDataException("Coordination workbook row 1 is missing.");
            if (rowNumber.HasValue && target == null)
                throw new InvalidDataException("Coordination workbook row " + rowNumber.Value + " is missing.");
            return new SelectedUnifiedRows(header, target);
        }

        private static Dictionary<string, int> RequiredColumns(Dictionary<int, string> headers, HashSet<int> formulas, IEnumerable<string> names)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                var matches = headers.Where(pair => string.Equals(pair.Value, name, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key).ToList();
                if (matches.Count != 1) throw new InvalidDataException("Coordination workbook must contain exactly one " + name + " column.");
                if (formulas.Contains(matches[0])) throw new InvalidDataException("Coordination workbook identity headers must be literal values.");
                result.Add(name, matches[0]);
            }
            return result;
        }

        private static void RequireLiteral(HashSet<int> formulas, IEnumerable<int> columns)
        {
            foreach (var column in columns) if (formulas.Contains(column)) throw new InvalidDataException("Coordination workbook identity cells must be literal values.");
        }

        private static Dictionary<string, ZipArchiveEntry> ResolveSheets(ZipArchive archive)
        {
            var workbookEntry = UniqueEntry(archive, "xl/workbook.xml") ?? throw new InvalidDataException("Coordination workbook.xml is missing.");
            var relsEntry = UniqueEntry(archive, "xl/_rels/workbook.xml.rels") ?? throw new InvalidDataException("Coordination workbook relationships are missing.");
            var workbook = LoadXml(workbookEntry);
            var rels = LoadXml(relsEntry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rns = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace pns = "http://schemas.openxmlformats.org/package/2006/relationships";
            var result = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in workbook.Descendants(ns + "sheet"))
            {
                var name = ((string)sheet.Attribute("name") ?? string.Empty).Trim();
                var id = ((string)sheet.Attribute(rns + "id") ?? string.Empty).Trim();
                if (name.Length == 0 || id.Length == 0 || result.ContainsKey(name)) throw new InvalidDataException("Coordination workbook contains invalid sheet metadata.");
                var rel = rels.Descendants(pns + "Relationship").Where(item => string.Equals((string)item.Attribute("Id"), id, StringComparison.Ordinal)).ToList();
                if (rel.Count != 1 || !string.Equals(((string)rel[0].Attribute("Type") ?? string.Empty).Trim(), WorksheetRelationshipType, StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination workbook worksheet relationship is invalid.");
                if (string.Equals((string)rel[0].Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("External worksheet relationships are not supported.");
                var target = ((string)rel[0].Attribute("Target") ?? string.Empty).Replace('\\', '/').Trim().TrimStart('/');
                if (target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) target = target.Substring(3);
                if (target.Length == 0 || target.Contains("..")) throw new InvalidDataException("Coordination workbook worksheet target is invalid.");
                var entry = UniqueEntry(archive, "xl/" + target) ?? throw new InvalidDataException("Coordination workbook worksheet part is missing: " + target + ".");
                result.Add(name, entry);
            }
            return result;
        }

        private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = UniqueEntry(archive, "xl/sharedStrings.xml");
            if (entry == null) return new string[0];
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            if (document.Root == null || document.Root.Name != ns + "sst") throw new InvalidDataException("Coordination sharedStrings.xml has an invalid root element.");
            var result = new List<string>();
            foreach (var item in document.Root.Elements(ns + "si"))
            {
                if (result.Count >= MaxRows) throw new InvalidDataException("Coordination shared-string table exceeds the supported limit.");
                result.Add(string.Concat(item.Descendants(ns + "t").Select(text => text.Value)));
            }
            return result.AsReadOnly();
        }

        private static Dictionary<int, string> ReadCells(XElement row, XNamespace ns, IReadOnlyList<string> sharedStrings, out HashSet<int> formulaColumns)
        {
            var result = new Dictionary<int, string>();
            formulaColumns = new HashSet<int>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = ((string)cell.Attribute("r") ?? string.Empty).Trim();
                var column = ParseColumn(reference);
                if (result.ContainsKey(column)) throw new InvalidDataException("Coordination workbook row contains duplicate cell coordinates.");
                if (cell.Element(ns + "f") != null) formulaColumns.Add(column);
                var type = ((string)cell.Attribute("t") ?? string.Empty).Trim();
                string value;
                if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
                {
                    var inline = cell.Element(ns + "is");
                    value = inline == null ? string.Empty : string.Concat(inline.Descendants(ns + "t").Select(text => text.Value));
                }
                else
                {
                    value = (string)cell.Element(ns + "v") ?? string.Empty;
                    if (string.Equals(type, "s", StringComparison.Ordinal))
                    {
                        int index;
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index) || index < 0 || index >= sharedStrings.Count)
                            throw new InvalidDataException("Coordination workbook contains an invalid shared-string index.");
                        value = sharedStrings[index];
                    }
                }
                result.Add(column, value);
            }
            return result;
        }

        private static int ParseRow(XElement row)
        {
            int value;
            if (!int.TryParse((string)row.Attribute("r"), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 1 || value > MaxRows)
                throw new InvalidDataException("Coordination workbook contains an invalid row coordinate.");
            return value;
        }

        private static int ParseColumn(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) throw new InvalidDataException("Coordination workbook cell coordinate is missing.");
            var value = 0;
            var count = 0;
            foreach (var ch in reference)
            {
                if (ch >= 'A' && ch <= 'Z') { value = checked(value * 26 + (ch - 'A' + 1)); count++; }
                else if (ch >= 'a' && ch <= 'z') { value = checked(value * 26 + (ch - 'a' + 1)); count++; }
                else break;
            }
            if (count == 0) throw new InvalidDataException("Coordination workbook cell coordinate is invalid: " + reference + ".");
            return value - 1;
        }

        private static string RequiredCell(IReadOnlyDictionary<int, string> cells, int column, string label)
        {
            string value;
            if (!cells.TryGetValue(column, out value) || string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(label + " is missing.");
            return CoordinationWorkbookIdentity.Required(value, label);
        }

        private static ZipArchiveEntry? UniqueEntry(ZipArchive archive, string name)
        {
            var matches = archive.Entries.Where(entry => string.Equals(entry.FullName.Replace('\\', '/'), name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count > 1) throw new InvalidDataException("Coordination workbook contains duplicate package entry: " + name + ".");
            return matches.Count == 0 ? null : matches[0];
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            if (entry.Length > MaxXmlCharacters) throw new InvalidDataException("Coordination workbook XML entry is too large: " + entry.FullName + ".");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxXmlCharacters };
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, settings)) return XDocument.Load(reader, LoadOptions.None);
        }

        private sealed class SelectedUnifiedRows
        {
            internal SelectedUnifiedRows(XElement header, XElement? target)
            {
                Header = header;
                Target = target;
            }
            internal XElement Header { get; }
            internal XElement? Target { get; }
        }

        private sealed class SourceProjection
        {
            internal SourceProjection(string itemId, string leftElementId, string rightElementId, DuplicateMatchKind matchKinds, string leftHandle, string rightHandle, string drawingFingerprint, string ruleId, string traceKey)
            {
                ItemId = itemId; LeftElementId = leftElementId; RightElementId = rightElementId; MatchKinds = matchKinds;
                LeftHandle = leftHandle; RightHandle = rightHandle; DrawingFingerprint = drawingFingerprint; RuleId = ruleId; TraceKey = traceKey;
            }
            internal string ItemId { get; }
            internal string LeftElementId { get; }
            internal string RightElementId { get; }
            internal DuplicateMatchKind MatchKinds { get; }
            internal string LeftHandle { get; }
            internal string RightHandle { get; }
            internal string DrawingFingerprint { get; }
            internal string RuleId { get; }
            internal string TraceKey { get; }
        }

        private sealed class TraceProjection
        {
            internal TraceProjection(string sheet, int row, string itemId, string leftHandle, string rightHandle, string drawingFingerprint, string ruleId)
            {
                Sheet = sheet; Row = row; ItemId = itemId; LeftHandle = leftHandle; RightHandle = rightHandle;
                DrawingFingerprint = drawingFingerprint; RuleId = ruleId;
            }
            internal string Sheet { get; }
            internal int Row { get; }
            internal string ItemId { get; }
            internal string LeftHandle { get; }
            internal string RightHandle { get; }
            internal string DrawingFingerprint { get; }
            internal string RuleId { get; }
        }
    }
}
