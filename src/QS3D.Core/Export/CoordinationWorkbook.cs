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

namespace QS3D.Core.Export
{
    /// <summary>
    /// Host-neutral export projection for an exact clash pair. This is deliberately not the
    /// canonical shared CoordinationIssue contract; it is a report/trace DTO that can be fed by
    /// the BricsCAD native exact-clash adapter today and by the shared contract after migration.
    /// </summary>
    public sealed class CoordinationClashExportRow
    {
        internal CoordinationClashExportRow(
            string clashId,
            string type,
            string severity,
            string status,
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
            ClashId = clashId;
            Type = type;
            Severity = severity;
            Status = status;
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

        public string ClashId { get; }
        public string Type { get; }
        public string Severity { get; }
        public string Status { get; }
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

        public static CoordinationClashExportRow CreateExactHard(
            string drawingFingerprint,
            string leftHandle,
            string rightHandle,
            string leftElementId = "",
            string rightElementId = "",
            string leftCategory = "",
            string rightCategory = "",
            string floor = "",
            string comment = "")
        {
            var fingerprint = CoordinationWorkbookIdentity.Required(drawingFingerprint, "Drawing Fingerprint");
            var left = CoordinationWorkbookIdentity.CanonicalHandle(leftHandle);
            var right = CoordinationWorkbookIdentity.CanonicalHandle(rightHandle);
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Coordination clash pair must reference two different CAD Handles.");

            var leftId = CoordinationWorkbookIdentity.Optional(leftElementId, "Element A ID");
            var rightId = CoordinationWorkbookIdentity.Optional(rightElementId, "Element B ID");
            var leftKind = CoordinationWorkbookIdentity.Optional(leftCategory, "Element A Category");
            var rightKind = CoordinationWorkbookIdentity.Optional(rightCategory, "Element B Category");
            if (StringComparer.OrdinalIgnoreCase.Compare(left, right) > 0)
            {
                Swap(ref left, ref right);
                Swap(ref leftId, ref rightId);
                Swap(ref leftKind, ref rightKind);
            }

            const string type = "HardClash";
            const string severity = "Error";
            const string status = "Open";
            const string ruleId = "MEP_EXACT_HARD";
            var clashId = CoordinationClashIdentity.Create(fingerprint, ruleId, left, right);
            return new CoordinationClashExportRow(
                clashId,
                type,
                severity,
                status,
                CoordinationWorkbookIdentity.Optional(floor, "Floor"),
                leftId,
                left,
                leftKind,
                rightId,
                right,
                rightKind,
                ruleId,
                fingerprint,
                CoordinationWorkbookIdentity.Optional(comment, "Comment"));
        }

        private static void Swap(ref string left, ref string right)
        {
            var value = left;
            left = right;
            right = value;
        }
    }

    public static class CoordinationClashIdentity
    {
        public static string Create(string drawingFingerprint, string ruleId, string leftHandle, string rightHandle)
        {
            var fingerprint = CoordinationWorkbookIdentity.Required(drawingFingerprint, "Drawing Fingerprint");
            var rule = CoordinationWorkbookIdentity.Required(ruleId, "Rule ID");
            var left = CoordinationWorkbookIdentity.CanonicalHandle(leftHandle);
            var right = CoordinationWorkbookIdentity.CanonicalHandle(rightHandle);
            if (StringComparer.OrdinalIgnoreCase.Compare(left, right) > 0)
            {
                var value = left;
                left = right;
                right = value;
            }
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Coordination clash identity requires two different CAD Handles.");

            return "CLASH:" + CoordinationWorkbookIdentity.Sha256Hex(
                fingerprint + "\u001f" + rule + "\u001f" + left + "\u001f" + right);
        }
    }

    public static class CoordinationWorkbookExporter
    {
        public const string ClashSheet = "CLASHES";
        public const string TraceSheet = "TRACE_MODEL";
        public const string TraceHeader = "TRACE_KEY";
        private const int HeaderStyle = 1;
        private const int IntegerStyle = 2;
        private const int WrappedStyle = 3;
        private const int MaxRows = 1048575;
        private static readonly DateTimeOffset DeterministicZipEntryTimestamp =
            new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static void Export(string path, IReadOnlyList<CoordinationClashExportRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var admittedRowCount = rows.Count;
            RequireCoordinationRowCountAdmission(admittedRowCount);

            var snapshot = Snapshot(rows, admittedRowCount);
            var traces = new List<CoordinationTraceProjection>(snapshot.Count);
            var clashXml = BuildClashSheet(snapshot, traces);
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
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", clashXml);
                    WriteEntry(archive, "xl/worksheets/sheet2.xml", traceXml);
                }

                XlsxPackageValidator.Validate(
                    tempPath,
                    "[Content_Types].xml",
                    "xl/workbook.xml",
                    "xl/_rels/workbook.xml.rels",
                    "xl/styles.xml",
                    "xl/worksheets/sheet1.xml",
                    "xl/worksheets/sheet2.xml");
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static void RequireCoordinationRowCountAdmission(int count)
        {
            if (count <= 0) throw new InvalidDataException("Coordination workbook CLASHES requires at least one row.");
            if (count > MaxRows) throw new InvalidDataException("Coordination workbook exceeds the Excel row limit.");
        }

        private static void RequireStableCoordinationRowCount(IReadOnlyList<CoordinationClashExportRow> source, int admittedRowCount)
        {
            if (source.Count != admittedRowCount)
                throw new InvalidDataException("Coordination workbook row Count changed during snapshot.");
        }

        private static List<CoordinationClashExportRow> Snapshot(IReadOnlyList<CoordinationClashExportRow> source, int admittedRowCount)
        {
            var result = new List<CoordinationClashExportRow>(admittedRowCount);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? fingerprint = null;
            for (var index = 0; index < admittedRowCount; index++)
            {
                RequireStableCoordinationRowCount(source, admittedRowCount);
                var row = source[index];
                if (row == null) throw new InvalidDataException("Coordination workbook contains a null clash row.");
                var expectedId = CoordinationClashIdentity.Create(row.DrawingFingerprint, row.RuleId, row.LeftHandle, row.RightHandle);
                if (!string.Equals(row.ClashId, expectedId, StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination clash ID does not match its canonical pair identity.");
                if (!ids.Add(row.ClashId)) throw new InvalidDataException("Coordination workbook contains duplicate ClashId: " + row.ClashId + ".");
                if (fingerprint == null) fingerprint = row.DrawingFingerprint;
                else if (!string.Equals(fingerprint, row.DrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Coordination workbook rows contain conflicting drawing fingerprints.");
                result.Add(row);
            }
            RequireStableCoordinationRowCount(source, admittedRowCount);
            result.Sort((a, b) => StringComparer.Ordinal.Compare(a.ClashId, b.ClashId));
            return result;
        }

        private static string BuildClashSheet(IReadOnlyList<CoordinationClashExportRow> rows, ICollection<CoordinationTraceProjection> traces)
        {
            var headers = new[]
            {
                "STT", "CLASH_ID", "TYPE", "SEVERITY", "STATUS", "FLOOR",
                "ELEMENT_A_ID", "ELEMENT_A_HANDLE", "ELEMENT_A_CATEGORY",
                "ELEMENT_B_ID", "ELEMENT_B_HANDLE", "ELEMENT_B_CATEGORY",
                "RULE_ID", "DRAWING_FINGERPRINT", "COMMENT", TraceHeader
            };
            var range = "A1:P" + (rows.Count + 1).ToString(CultureInfo.InvariantCulture);
            var sb = BeginSheet(range);
            sb.Append("<cols><col min=\"1\" max=\"1\" width=\"7\" customWidth=\"1\"/><col min=\"2\" max=\"16\" width=\"24\" customWidth=\"1\"/></cols>");
            sb.Append("<sheetData><row r=\"1\" ht=\"30\" customHeight=\"1\">");
            for (var column = 0; column < headers.Length; column++) Text(sb, Cell(column, 1), headers[column], HeaderStyle);
            sb.Append("</row>");

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = index + 2;
                var traceKey = CoordinationWorkbookIdentity.BuildTraceKey(row, ClashSheet);
                traces.Add(new CoordinationTraceProjection(traceKey, excelRow, row));
                sb.Append("<row r=\"").Append(excelRow).Append("\">");
                Number(sb, Cell(0, excelRow), index + 1, IntegerStyle);
                Text(sb, Cell(1, excelRow), row.ClashId, WrappedStyle);
                Text(sb, Cell(2, excelRow), row.Type, 0);
                Text(sb, Cell(3, excelRow), row.Severity, 0);
                Text(sb, Cell(4, excelRow), row.Status, 0);
                Text(sb, Cell(5, excelRow), row.Floor, 0);
                Text(sb, Cell(6, excelRow), row.LeftElementId, WrappedStyle);
                Text(sb, Cell(7, excelRow), row.LeftHandle, 0);
                Text(sb, Cell(8, excelRow), row.LeftCategory, 0);
                Text(sb, Cell(9, excelRow), row.RightElementId, WrappedStyle);
                Text(sb, Cell(10, excelRow), row.RightHandle, 0);
                Text(sb, Cell(11, excelRow), row.RightCategory, 0);
                Text(sb, Cell(12, excelRow), row.RuleId, 0);
                Text(sb, Cell(13, excelRow), row.DrawingFingerprint, WrappedStyle);
                Text(sb, Cell(14, excelRow), row.Comment, WrappedStyle);
                Text(sb, Cell(15, excelRow), traceKey, WrappedStyle);
                sb.Append("</row>");
            }
            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>");
            return sb.ToString();
        }

        private static string BuildTraceSheet(IReadOnlyList<CoordinationTraceProjection> traces)
        {
            var headers = new[] { TraceHeader, "SHEET", "ROW", "CLASH_ID", "LEFT_HANDLE", "RIGHT_HANDLE", "DRAWING_FINGERPRINT", "RULE_ID" };
            var range = "A1:H" + (traces.Count + 1).ToString(CultureInfo.InvariantCulture);
            var sb = BeginSheet(range);
            sb.Append("<cols><col min=\"1\" max=\"1\" width=\"44\" customWidth=\"1\"/><col min=\"2\" max=\"8\" width=\"28\" customWidth=\"1\"/></cols>");
            sb.Append("<sheetData><row r=\"1\">");
            for (var column = 0; column < headers.Length; column++) Text(sb, Cell(column, 1), headers[column], HeaderStyle);
            sb.Append("</row>");
            for (var index = 0; index < traces.Count; index++)
            {
                var trace = traces[index];
                var excelRow = index + 2;
                sb.Append("<row r=\"").Append(excelRow).Append("\">");
                Text(sb, Cell(0, excelRow), trace.TraceKey, WrappedStyle);
                Text(sb, Cell(1, excelRow), ClashSheet, 0);
                Number(sb, Cell(2, excelRow), trace.Row, IntegerStyle);
                Text(sb, Cell(3, excelRow), trace.Source.ClashId, WrappedStyle);
                Text(sb, Cell(4, excelRow), trace.Source.LeftHandle, 0);
                Text(sb, Cell(5, excelRow), trace.Source.RightHandle, 0);
                Text(sb, Cell(6, excelRow), trace.Source.DrawingFingerprint, WrappedStyle);
                Text(sb, Cell(7, excelRow), trace.Source.RuleId, 0);
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

        private static void Text(StringBuilder sb, string cell, string value, int style)
        {
            var text = value ?? string.Empty;
            if (text.Length > 32767) throw new InvalidDataException("Coordination workbook text cell exceeds the Excel 32,767-character limit: " + cell + ".");
            sb.Append("<c r=\"").Append(cell).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is>");
            XlsxXmlText.AppendTextElement(sb, text);
            sb.Append("</is></c>");
        }

        private static void Number(StringBuilder sb, string cell, int value, int style)
        {
            sb.Append("<c r=\"").Append(cell).Append("\" s=\"").Append(style).Append("\"><v>")
              .Append(value.ToString(CultureInfo.InvariantCulture)).Append("</v></c>");
        }

        private static string Cell(int column, int row)
        {
            var n = column + 1;
            var name = string.Empty;
            while (n > 0)
            {
                n--;
                name = (char)('A' + n % 26) + name;
                n /= 26;
            }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = DeterministicZipEntryTimestamp;
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private sealed class CoordinationTraceProjection
        {
            public CoordinationTraceProjection(string traceKey, int row, CoordinationClashExportRow source)
            {
                TraceKey = traceKey;
                Row = row;
                Source = source;
            }
            public string TraceKey { get; }
            public int Row { get; }
            public CoordinationClashExportRow Source { get; }
        }

        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"CLASHES\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"TRACE_MODEL\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFC000\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"2\"><border/><border><left style=\"thin\"/><right style=\"thin\"/><top style=\"thin\"/><bottom style=\"thin\"/></border></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"4\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"3\" applyNumberFormat=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf></cellXfs></styleSheet>";
    }

    public sealed class CoordinationWorkbookTrace
    {
        internal CoordinationWorkbookTrace(int rowNumber, string clashId, string leftHandle, string rightHandle, string drawingFingerprint, string ruleId)
        {
            RowNumber = rowNumber;
            ClashId = clashId;
            LeftHandle = leftHandle;
            RightHandle = rightHandle;
            DrawingFingerprint = drawingFingerprint;
            RuleId = ruleId;
        }
        public int RowNumber { get; }
        public string ClashId { get; }
        public string LeftHandle { get; }
        public string RightHandle { get; }
        public string DrawingFingerprint { get; }
        public string RuleId { get; }
    }

    public static class CoordinationWorkbookTraceReader
    {
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxXmlCharacters = 64L * 1024L * 1024L;
        private const int MaxRows = 1048576;
        private const string WorksheetRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";

        public static CoordinationWorkbookTrace Read(string path, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Excel path is required.", nameof(path));
            if (rowNumber < 2 || rowNumber > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(rowNumber), "Coordination workbook data row must be between 2 and " + MaxRows + ".");
            var fullPath = Path.GetFullPath(path);
            var info = new FileInfo(fullPath);
            if (!info.Exists) throw new FileNotFoundException("Coordination workbook was not found.", fullPath);
            if (info.Length > MaxWorkbookBytes) throw new InvalidDataException("Coordination workbook is too large for trace lookup.");

            using (var archive = ZipFile.OpenRead(fullPath))
            {
                var sheets = ResolveSheets(archive);
                var expected = new HashSet<string>(new[] { CoordinationWorkbookExporter.ClashSheet, CoordinationWorkbookExporter.TraceSheet }, StringComparer.OrdinalIgnoreCase);
                if (!expected.SetEquals(sheets.Keys))
                    throw new InvalidDataException("Coordination workbook must contain exactly CLASHES and TRACE_MODEL worksheets.");
                var sharedStrings = ReadSharedStrings(archive);
                var clash = ReadClashRow(sheets[CoordinationWorkbookExporter.ClashSheet], rowNumber, sharedStrings);
                var trace = ReadTraceProjection(sheets[CoordinationWorkbookExporter.TraceSheet], clash.TraceKey, rowNumber, sharedStrings);

                if (!string.Equals(clash.ClashId, trace.ClashId, StringComparison.Ordinal) ||
                    !string.Equals(clash.LeftHandle, trace.LeftHandle, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(clash.RightHandle, trace.RightHandle, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(clash.DrawingFingerprint, trace.DrawingFingerprint, StringComparison.Ordinal) ||
                    !string.Equals(clash.RuleId, trace.RuleId, StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination workbook CLASHES identity does not match TRACE_MODEL provenance.");

                var expectedId = CoordinationClashIdentity.Create(trace.DrawingFingerprint, trace.RuleId, trace.LeftHandle, trace.RightHandle);
                if (!string.Equals(expectedId, trace.ClashId, StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination workbook ClashId does not match canonical pair identity.");
                var expectedTraceKey = CoordinationWorkbookIdentity.BuildTraceKey(trace.ClashId, trace.DrawingFingerprint, trace.RuleId, trace.LeftHandle, trace.RightHandle, CoordinationWorkbookExporter.ClashSheet);
                if (!string.Equals(expectedTraceKey, clash.TraceKey, StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination workbook TRACE_KEY does not match canonical pair provenance.");
                return new CoordinationWorkbookTrace(rowNumber, trace.ClashId, trace.LeftHandle, trace.RightHandle, trace.DrawingFingerprint, trace.RuleId);
            }
        }

        private static ClashProjection ReadClashRow(ZipArchiveEntry entry, int rowNumber, IReadOnlyList<string> sharedStrings)
        {
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var selected = SelectCoordinationRowsBounded(document, ns, rowNumber);
            if (ParseRow(selected.Header) != 1 || selected.Target == null || ParseRow(selected.Target) != rowNumber)
                throw new InvalidDataException("Coordination workbook selected row metadata changed during lookup.");
            var header = ReadCells(selected.Header, ns, sharedStrings, out var headerFormulas);
            var target = ReadCells(selected.Target, ns, sharedStrings, out var targetFormulas);
            var columns = RequiredColumns(header, headerFormulas, new[] { "CLASH_ID", "ELEMENT_A_HANDLE", "ELEMENT_B_HANDLE", "RULE_ID", "DRAWING_FINGERPRINT", CoordinationWorkbookExporter.TraceHeader });
            foreach (var column in columns.Values)
                if (targetFormulas.Contains(column)) throw new InvalidDataException("Coordination workbook identity cells must be literal values.");
            return new ClashProjection(
                RequiredCell(target, columns["CLASH_ID"], "CLASH_ID"),
                CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(target, columns["ELEMENT_A_HANDLE"], "ELEMENT_A_HANDLE")),
                CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(target, columns["ELEMENT_B_HANDLE"], "ELEMENT_B_HANDLE")),
                RequiredCell(target, columns["DRAWING_FINGERPRINT"], "DRAWING_FINGERPRINT"),
                RequiredCell(target, columns["RULE_ID"], "RULE_ID"),
                RequiredCell(target, columns[CoordinationWorkbookExporter.TraceHeader], CoordinationWorkbookExporter.TraceHeader));
        }

        private static ClashProjection ReadTraceProjection(ZipArchiveEntry entry, string traceKey, int rowNumber, IReadOnlyList<string> sharedStrings)
        {
            var document = LoadXml(entry);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var selected = SelectCoordinationRowsBounded(document, ns, null);
            if (ParseRow(selected.Header) != 1)
                throw new InvalidDataException("Coordination workbook TRACE_MODEL header metadata changed during lookup.");
            var header = ReadCells(selected.Header, ns, sharedStrings, out var headerFormulas);
            var columns = RequiredColumns(header, headerFormulas, new[] { CoordinationWorkbookExporter.TraceHeader, "SHEET", "ROW", "CLASH_ID", "LEFT_HANDLE", "RIGHT_HANDLE", "DRAWING_FINGERPRINT", "RULE_ID" });

            Dictionary<int, string>? matchedCells = null;
            HashSet<int>? matchedFormulas = null;
            foreach (var row in document.Descendants(ns + "row"))
            {
                var declaredRow = ParseRow(row);
                if (declaredRow < 2) continue;
                var cells = ReadCells(row, ns, sharedStrings, out var formulas);
                string value;
                if (!cells.TryGetValue(columns[CoordinationWorkbookExporter.TraceHeader], out value) || !string.Equals(value, traceKey, StringComparison.Ordinal))
                    continue;
                if (matchedCells != null)
                    throw new InvalidDataException("TRACE_MODEL lookup is missing or ambiguous for TRACE_KEY " + traceKey + ".");
                matchedCells = cells;
                matchedFormulas = formulas;
            }
            if (matchedCells == null || matchedFormulas == null)
                throw new InvalidDataException("TRACE_MODEL lookup is missing or ambiguous for TRACE_KEY " + traceKey + ".");
            foreach (var column in columns.Values)
                if (matchedFormulas.Contains(column)) throw new InvalidDataException("TRACE_MODEL identity cells must be literal values.");
            var sourceSheet = RequiredCell(matchedCells, columns["SHEET"], "TRACE_MODEL SHEET");
            if (!string.Equals(sourceSheet, CoordinationWorkbookExporter.ClashSheet, StringComparison.Ordinal))
                throw new InvalidDataException("TRACE_MODEL SHEET does not reference CLASHES.");
            int sourceRow;
            if (!int.TryParse(RequiredCell(matchedCells, columns["ROW"], "TRACE_MODEL ROW"), NumberStyles.Integer, CultureInfo.InvariantCulture, out sourceRow) || sourceRow != rowNumber)
                throw new InvalidDataException("TRACE_MODEL ROW does not match the selected CLASHES row.");
            return new ClashProjection(
                RequiredCell(matchedCells, columns["CLASH_ID"], "TRACE_MODEL CLASH_ID"),
                CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(matchedCells, columns["LEFT_HANDLE"], "TRACE_MODEL LEFT_HANDLE")),
                CoordinationWorkbookIdentity.CanonicalHandle(RequiredCell(matchedCells, columns["RIGHT_HANDLE"], "TRACE_MODEL RIGHT_HANDLE")),
                RequiredCell(matchedCells, columns["DRAWING_FINGERPRINT"], "TRACE_MODEL DRAWING_FINGERPRINT"),
                RequiredCell(matchedCells, columns["RULE_ID"], "TRACE_MODEL RULE_ID"),
                traceKey);
        }

        private static SelectedCoordinationRows SelectCoordinationRowsBounded(XDocument document, XNamespace ns, int? rowNumber)
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
            return new SelectedCoordinationRows(header, target);
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
                var targetPath = ((string)rel[0].Attribute("Target") ?? string.Empty).Replace('\\', '/').Trim().TrimStart('/');
                if (targetPath.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) targetPath = targetPath.Substring(3);
                if (targetPath.Length == 0 || targetPath.Contains("..")) throw new InvalidDataException("Coordination workbook worksheet target is invalid.");
                var entry = UniqueEntry(archive, "xl/" + targetPath) ?? throw new InvalidDataException("Coordination workbook worksheet part is missing: " + targetPath + ".");
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

        private sealed class SelectedCoordinationRows
        {
            public SelectedCoordinationRows(XElement header, XElement? target)
            {
                Header = header;
                Target = target;
            }
            public XElement Header { get; }
            public XElement? Target { get; }
        }

        private sealed class ClashProjection
        {
            public ClashProjection(string clashId, string leftHandle, string rightHandle, string drawingFingerprint, string ruleId, string traceKey)
            {
                ClashId = clashId;
                LeftHandle = leftHandle;
                RightHandle = rightHandle;
                DrawingFingerprint = drawingFingerprint;
                RuleId = ruleId;
                TraceKey = traceKey;
            }
            public string ClashId { get; }
            public string LeftHandle { get; }
            public string RightHandle { get; }
            public string DrawingFingerprint { get; }
            public string RuleId { get; }
            public string TraceKey { get; }
        }
    }

    internal static class CoordinationWorkbookIdentity
    {
        public static string Required(string value, string label)
        {
            var raw = value ?? string.Empty;
            var canonical = raw.Trim();
            if (canonical.Length == 0) throw new InvalidDataException("Coordination workbook " + label + " is required.");
            if (!string.Equals(raw, canonical, StringComparison.Ordinal) || canonical.Any(char.IsControl))
                throw new InvalidDataException("Coordination workbook " + label + " must be a canonical literal value.");
            if (canonical.Length > 32767) throw new InvalidDataException("Coordination workbook " + label + " exceeds the Excel cell text limit.");
            return canonical;
        }

        public static string Optional(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Required(value, label);
        }

        public static string CanonicalHandle(string value)
        {
            var token = Required(value, "CAD Handle");
            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) token = token.Substring(2);
            ulong number;
            if (!ulong.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number) || number == 0UL)
                throw new InvalidDataException("Coordination workbook contains an invalid CAD Handle: " + value + ".");
            return number.ToString("X", CultureInfo.InvariantCulture);
        }

        public static string BuildTraceKey(CoordinationClashExportRow row, string sheet)
        {
            return BuildTraceKey(row.ClashId, row.DrawingFingerprint, row.RuleId, row.LeftHandle, row.RightHandle, sheet);
        }

        public static string BuildTraceKey(string clashId, string drawingFingerprint, string ruleId, string leftHandle, string rightHandle, string sheet)
        {
            var id = Required(clashId, "ClashId");
            var fingerprint = Required(drawingFingerprint, "Drawing Fingerprint");
            var rule = Required(ruleId, "Rule ID");
            var left = CanonicalHandle(leftHandle);
            var right = CanonicalHandle(rightHandle);
            if (StringComparer.OrdinalIgnoreCase.Compare(left, right) > 0)
            {
                var value = left;
                left = right;
                right = value;
            }
            return sheet + ":" + Sha256Hex(sheet + "\u001f" + fingerprint + "\u001f" + id + "\u001f" + rule + "\u001f" + left + "\u001f" + right);
        }

        public static string Sha256Hex(string value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var result = new StringBuilder(hash.Length * 2);
                foreach (var item in hash) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}