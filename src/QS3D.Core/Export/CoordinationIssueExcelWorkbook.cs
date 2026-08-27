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
    /// Conflict-safe XLSX projection for canonical CoordinationIssue lifecycle fields.
    /// Immutable trace/provenance columns are validated on import; only status, severity,
    /// assignee and a new comment are accepted as edits.
    /// </summary>
    public static class CoordinationIssueExcelWorkbook
    {
        public const string MetaSheet = "META";
        public const string IssuesSheet = "ISSUES";
        public const string SchemaVersion = "QS3D_COORDINATION_ISSUES_V1";
        private const int MaxRows = 1048576;
        private const int MaxColumns = 16384;
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxXmlCharacters = 64L * 1024L * 1024L;
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        private static readonly string[] IssueHeaders =
        {
            "STT", "ISSUE_ID", "ISSUE_REVISION", "KIND", "STATUS", "SEVERITY", "ASSIGNEE",
            "COMMENT_AUTHOR", "COMMENT", "TITLE", "LEFT_SEMANTIC_ID", "LEFT_DRAWING_ID", "LEFT_HANDLE",
            "RIGHT_SEMANTIC_ID", "RIGHT_DRAWING_ID", "RIGHT_HANDLE", "DISCIPLINE", "CATEGORY", "SYSTEM",
            "REGION", "SEPARATION_M", "UPDATED_AT_UTC"
        };

        public static void Export(string path, CoordinationIssuePersistenceSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Issues.Count == 0) throw new InvalidDataException("Coordination issue workbook requires at least one issue.");
            if (snapshot.Issues.Count >= MaxRows) throw new InvalidDataException("Coordination issue workbook exceeds the Excel row limit.");

            var rows = CoordinationIssueExcelLifecycle.Project(snapshot);
            var metaRows = new List<IReadOnlyList<string>>
            {
                new[] { "SCHEMA", SchemaVersion },
                new[] { "PROJECT_ID", snapshot.ProjectId },
                new[] { "DRAWING_FINGERPRINT", snapshot.DrawingFingerprint },
                new[] { "WORKBOOK_REVISION", snapshot.Revision.ToString(CultureInfo.InvariantCulture) }
            };
            var issueRows = new List<IReadOnlyList<string>>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                issueRows.Add(new[]
                {
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    row.IssueId,
                    row.IssueRevision,
                    row.Kind.ToString(),
                    row.Status.ToString(),
                    row.Severity.ToString(),
                    row.Assignee,
                    string.Empty,
                    string.Empty,
                    row.Title,
                    row.LeftSemanticId,
                    DrawingId(row.LeftCadReference),
                    Handle(row.LeftCadReference),
                    row.RightSemanticId,
                    DrawingId(row.RightCadReference),
                    Handle(row.RightCadReference),
                    row.DisciplineContext,
                    row.CategoryContext,
                    row.SystemContext,
                    row.RegionContext,
                    row.SeparationM.ToString("R", CultureInfo.InvariantCulture),
                    row.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture)
                });
            }

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
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheet(new[] { "KEY", "VALUE" }, metaRows));
                    WriteEntry(archive, "xl/worksheets/sheet2.xml", BuildSheet(IssueHeaders, issueRows));
                }
                XlsxPackageValidator.Validate(
                    tempPath,
                    "[Content_Types].xml",
                    "xl/workbook.xml",
                    "xl/_rels/workbook.xml.rels",
                    "xl/worksheets/sheet1.xml",
                    "xl/worksheets/sheet2.xml");
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        public static CoordinationIssueExcelImportPlan ReadAndPlanImport(
            string path,
            CoordinationIssuePersistenceSnapshot current,
            DateTime changedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Excel path is required.", nameof(path));
            if (current == null) throw new ArgumentNullException(nameof(current));
            var fullPath = Path.GetFullPath(path);
            var info = new FileInfo(fullPath);
            if (!info.Exists) throw new FileNotFoundException("Coordination issue workbook was not found.", fullPath);
            if (info.Length > MaxWorkbookBytes) throw new InvalidDataException("Coordination issue workbook is too large.");

            using (var archive = ZipFile.OpenRead(fullPath))
            {
                var sheets = ResolveSheets(archive);
                if (sheets.Count != 2 || !sheets.ContainsKey(MetaSheet) || !sheets.ContainsKey(IssuesSheet))
                    throw new InvalidDataException("Coordination issue workbook must contain exactly META and ISSUES worksheets.");
                var sharedStrings = ReadSharedStrings(archive);
                var meta = ReadMeta(sheets[MetaSheet], sharedStrings);
                RequireMeta(meta, "SCHEMA", SchemaVersion);
                var projectId = RequiredMeta(meta, "PROJECT_ID");
                var drawingFingerprint = RequiredMeta(meta, "DRAWING_FINGERPRINT");
                var revisionToken = RequiredMeta(meta, "WORKBOOK_REVISION");
                long workbookRevision;
                if (!long.TryParse(revisionToken, NumberStyles.None, CultureInfo.InvariantCulture, out workbookRevision) || workbookRevision <= 0L ||
                    !string.Equals(revisionToken, workbookRevision.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination issue workbook revision is invalid or non-canonical.");

                var projected = CoordinationIssueExcelLifecycle.Project(current).ToDictionary(x => x.IssueId, StringComparer.OrdinalIgnoreCase);
                var edits = ReadIssueEdits(sheets[IssuesSheet], sharedStrings, projected);
                if (edits.Count != projected.Count)
                    throw new InvalidDataException("Coordination issue workbook is missing one or more canonical IssueId rows. Re-export before importing edits.");
                return CoordinationIssueExcelLifecycle.PlanImport(
                    current,
                    projectId,
                    drawingFingerprint,
                    workbookRevision,
                    edits,
                    changedAtUtc);
            }
        }

        private static List<CoordinationIssueExcelEdit> ReadIssueEdits(
            ZipArchiveEntry sheet,
            IReadOnlyList<string> sharedStrings,
            IReadOnlyDictionary<string, CoordinationIssueExcelRow> projected)
        {
            var rows = ReadRows(sheet, sharedStrings);
            if (rows.Count == 0) throw new InvalidDataException("ISSUES worksheet is empty.");
            var header = HeaderMap(rows[0]);
            if (header.Count != IssueHeaders.Length || IssueHeaders.Any(name => !header.ContainsKey(name)))
                throw new InvalidDataException("ISSUES worksheet headers are missing, duplicated, or unsupported.");

            var result = new List<CoordinationIssueExcelEdit>(rows.Count - 1);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < rows.Count; i++)
            {
                var values = rows[i];
                if (values.All(string.IsNullOrWhiteSpace)) continue;
                var issueId = Cell(values, header, "ISSUE_ID").Trim();
                if (!seen.Add(issueId)) throw new InvalidDataException("ISSUES worksheet contains duplicate IssueId: " + issueId + ".");
                CoordinationIssueExcelRow expected;
                if (!projected.TryGetValue(issueId, out expected))
                    throw new InvalidDataException("ISSUES worksheet references an unknown IssueId: " + issueId + ".");

                RequireSame(values, header, "ISSUE_REVISION", expected.IssueRevision, issueId);
                RequireSame(values, header, "KIND", expected.Kind.ToString(), issueId);
                RequireSame(values, header, "TITLE", expected.Title, issueId);
                RequireSame(values, header, "LEFT_SEMANTIC_ID", expected.LeftSemanticId, issueId);
                RequireSame(values, header, "LEFT_DRAWING_ID", DrawingId(expected.LeftCadReference), issueId);
                RequireSame(values, header, "LEFT_HANDLE", Handle(expected.LeftCadReference), issueId, true);
                RequireSame(values, header, "RIGHT_SEMANTIC_ID", expected.RightSemanticId, issueId);
                RequireSame(values, header, "RIGHT_DRAWING_ID", DrawingId(expected.RightCadReference), issueId);
                RequireSame(values, header, "RIGHT_HANDLE", Handle(expected.RightCadReference), issueId, true);
                RequireSame(values, header, "DISCIPLINE", expected.DisciplineContext, issueId);
                RequireSame(values, header, "CATEGORY", expected.CategoryContext, issueId);
                RequireSame(values, header, "SYSTEM", expected.SystemContext, issueId);
                RequireSame(values, header, "REGION", expected.RegionContext, issueId);
                RequireSame(values, header, "SEPARATION_M", expected.SeparationM.ToString("R", CultureInfo.InvariantCulture), issueId);
                RequireSame(values, header, "UPDATED_AT_UTC", expected.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture), issueId);

                result.Add(new CoordinationIssueExcelEdit(
                    issueId,
                    Cell(values, header, "ISSUE_REVISION"),
                    Cell(values, header, "STATUS"),
                    Cell(values, header, "SEVERITY"),
                    Cell(values, header, "ASSIGNEE"),
                    Cell(values, header, "COMMENT_AUTHOR"),
                    Cell(values, header, "COMMENT")));
            }
            return result;
        }

        private static void RequireSame(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> header, string column, string expected, string issueId, bool ignoreCase = false)
        {
            var actual = Cell(row, header, column);
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!string.Equals(actual, expected ?? string.Empty, comparison))
                throw new InvalidDataException("ISSUES worksheet immutable column " + column + " was changed for IssueId " + issueId + ". Re-export before importing edits.");
        }

        private static Dictionary<string, string> ReadMeta(ZipArchiveEntry sheet, IReadOnlyList<string> sharedStrings)
        {
            var rows = ReadRows(sheet, sharedStrings);
            if (rows.Count == 0 || rows[0].Count < 2 || !string.Equals(rows[0][0], "KEY", StringComparison.Ordinal) || !string.Equals(rows[0][1], "VALUE", StringComparison.Ordinal))
                throw new InvalidDataException("META worksheet header is invalid.");
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 1; i < rows.Count; i++)
            {
                if (rows[i].Count < 2) continue;
                var key = rows[i][0];
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (result.ContainsKey(key)) throw new InvalidDataException("META worksheet contains duplicate key: " + key + ".");
                result.Add(key, rows[i][1]);
            }
            return result;
        }

        private static void RequireMeta(IReadOnlyDictionary<string, string> meta, string key, string expected)
        {
            var actual = RequiredMeta(meta, key);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException("Coordination issue workbook " + key + " is unsupported.");
        }

        private static string RequiredMeta(IReadOnlyDictionary<string, string> meta, string key)
        {
            string value;
            if (!meta.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Coordination issue workbook META is missing " + key + ".");
            return value.Trim();
        }

        private static Dictionary<string, ZipArchiveEntry> ResolveSheets(ZipArchive archive)
        {
            var workbook = LoadXml(RequiredEntry(archive, "xl/workbook.xml"));
            var relationships = LoadXml(RequiredEntry(archive, "xl/_rels/workbook.xml.rels"));
            var targets = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var relationship in relationships.Root.Elements(PackageRelationshipNs + "Relationship"))
            {
                var id = (string)relationship.Attribute("Id");
                var target = (string)relationship.Attribute("Target");
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target)) targets[id] = target;
            }

            var result = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in workbook.Descendants(SpreadsheetNs + "sheet"))
            {
                var name = (string)sheet.Attribute("name");
                var relationshipId = (string)sheet.Attribute(RelationshipNs + "id");
                string target;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relationshipId) || !targets.TryGetValue(relationshipId, out target))
                    throw new InvalidDataException("Coordination issue workbook sheet relationship is invalid.");
                var normalized = target.Replace('\\', '/').TrimStart('/');
                if (!normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) normalized = "xl/" + normalized;
                var entry = RequiredEntry(archive, normalized);
                if (result.ContainsKey(name)) throw new InvalidDataException("Coordination issue workbook contains duplicate sheet name: " + name + ".");
                result.Add(name, entry);
            }
            return result;
        }

        private static List<IReadOnlyList<string>> ReadRows(ZipArchiveEntry sheet, IReadOnlyList<string> sharedStrings)
        {
            var document = LoadXml(sheet);
            var result = new List<IReadOnlyList<string>>();
            foreach (var row in document.Descendants(SpreadsheetNs + "row"))
            {
                var rowReference = (string)row.Attribute("r");
                int expectedRowIndex;
                if (!int.TryParse(rowReference, NumberStyles.None, CultureInfo.InvariantCulture, out expectedRowIndex)
                    || expectedRowIndex <= 0
                    || expectedRowIndex > MaxRows
                    || !string.Equals(rowReference, expectedRowIndex.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                    throw new InvalidDataException($"Malformed XLSX row reference '{rowReference ?? "<null>"}'.");

                var cells = new SortedDictionary<int, string>();
                foreach (var cell in row.Elements(SpreadsheetNs + "c"))
                {
                    var cellReference = (string)cell.Attribute("r");
                    int columnIndex;
                    int parsedRowIndex;
                    if (!TryParseA1CellReference(cellReference, out columnIndex, out parsedRowIndex))
                        throw new InvalidDataException($"Malformed XLSX cell reference '{cellReference ?? "<null>"}'.");
                    if (parsedRowIndex != expectedRowIndex)
                        throw new InvalidDataException($"XLSX cell reference '{cellReference}' targets row {parsedRowIndex}, but appears inside worksheet row {expectedRowIndex}.");
                    if (cells.ContainsKey(columnIndex)) throw new InvalidDataException("Coordination issue workbook row contains duplicate cell references.");
                    cells.Add(columnIndex, CellText(cell, sharedStrings));
                }
                if (cells.Count == 0)
                {
                    result.Add(new string[0]);
                    continue;
                }
                var values = new string[cells.Keys.Max() + 1];
                foreach (var pair in cells) values[pair.Key] = pair.Value;
                for (var i = 0; i < values.Length; i++) if (values[i] == null) values[i] = string.Empty;
                result.Add(values);
                if (result.Count > MaxRows) throw new InvalidDataException("Coordination issue workbook exceeds the supported row count.");
            }
            return result;
        }

        private static string CellText(XElement cell, IReadOnlyList<string> sharedStrings)
        {
            var type = ((string)cell.Attribute("t") ?? string.Empty).Trim();
            if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
                return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(x => (string)x));
            var value = (string)cell.Element(SpreadsheetNs + "v") ?? string.Empty;
            if (string.Equals(type, "s", StringComparison.Ordinal))
            {
                int index;
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out index) || index < 0 || index >= sharedStrings.Count)
                    throw new InvalidDataException("Coordination issue workbook contains an invalid shared-string index.");
                return sharedStrings[index];
            }
            return value;
        }

        private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return new string[0];
            var document = LoadXml(entry);
            return document.Descendants(SpreadsheetNs + "si")
                .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => (string)text)))
                .ToList();
        }

        private static Dictionary<string, int> HeaderMap(IReadOnlyList<string> row)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < row.Count; i++)
            {
                var name = row[i] ?? string.Empty;
                if (name.Length == 0) continue;
                if (result.ContainsKey(name)) throw new InvalidDataException("ISSUES worksheet contains duplicate header: " + name + ".");
                result.Add(name, i);
            }
            return result;
        }

        private static string Cell(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> header, string name)
        {
            var index = header[name];
            return index < row.Count ? row[index] ?? string.Empty : string.Empty;
        }

        private static bool TryParseA1CellReference(string reference, out int columnIndex, out int parsedRowIndex)
        {
            columnIndex = -1;
            parsedRowIndex = -1;
            if (string.IsNullOrEmpty(reference)) return false;

            var columnNumber = 0;
            var index = 0;
            while (index < reference.Length)
            {
                var character = reference[index];
                if (character >= 'a' && character <= 'z') character = (char)(character - ('a' - 'A'));
                if (character < 'A' || character > 'Z') break;
                try
                {
                    columnNumber = checked(columnNumber * 26 + (character - 'A' + 1));
                }
                catch (OverflowException)
                {
                    return false;
                }
                index++;
            }

            if (index == 0 || index == reference.Length || columnNumber <= 0 || columnNumber > MaxColumns) return false;
            if (reference[index] == '0') return false;
            for (var i = index; i < reference.Length; i++)
            {
                if (reference[i] < '0' || reference[i] > '9') return false;
            }

            var rowToken = reference.Substring(index);
            if (!int.TryParse(rowToken, NumberStyles.None, CultureInfo.InvariantCulture, out parsedRowIndex)
                || parsedRowIndex <= 0
                || parsedRowIndex > MaxRows
                || !string.Equals(rowToken, parsedRowIndex.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                return false;

            columnIndex = columnNumber - 1;
            return true;
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            if (entry.Length > MaxXmlCharacters) throw new InvalidDataException("Coordination issue workbook XML part is too large: " + entry.FullName + ".");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxXmlCharacters };
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, settings)) return XDocument.Load(reader, LoadOptions.None);
        }

        private static ZipArchiveEntry RequiredEntry(ZipArchive archive, string name)
        {
            var entry = archive.GetEntry(name);
            if (entry == null) throw new InvalidDataException("Coordination issue workbook is missing package part: " + name + ".");
            return entry;
        }

        private static string DrawingId(QS3D.Platform.Domain.CadReference? reference)
        {
            return reference.HasValue ? reference.Value.DrawingId.Value.ToString("D", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string Handle(QS3D.Platform.Domain.CadReference? reference)
        {
            return reference.HasValue ? reference.Value.Handle.Value : string.Empty;
        }

        private static string BuildSheet(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            AppendRow(builder, 1, headers);
            for (var i = 0; i < rows.Count; i++) AppendRow(builder, i + 2, rows[i]);
            builder.Append("</sheetData></worksheet>");
            return builder.ToString();
        }

        private static void AppendRow(StringBuilder builder, int rowNumber, IReadOnlyList<string> values)
        {
            builder.Append("<row r=\"").Append(rowNumber.ToString(CultureInfo.InvariantCulture)).Append("\">");
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i] ?? string.Empty;
                if (value.Length > 32767) throw new InvalidDataException("Coordination issue workbook cell exceeds the Excel text limit.");
                builder.Append("<c r=\"").Append(CellReference(i, rowNumber)).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">")
                    .Append(SecurityElement.Escape(value) ?? string.Empty)
                    .Append("</t></is></c>");
            }
            builder.Append("</row>");
        }

        private static string CellReference(int column, int row)
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
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"META\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"ISSUES\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/></Relationships>";
    }
}
