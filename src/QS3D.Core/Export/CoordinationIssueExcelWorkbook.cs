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
        private const int MaxWorksheetRows = 1048576;
        private const int MaxExportIssueRows = 10000;
        private const int MaxColumns = 16384;
        private const long MaxWorkbookBytes = 128L * 1024L * 1024L;
        private const long MaxXmlCharacters = 64L * 1024L * 1024L;
        private const long MaxExportXmlEntryBytes = 32L * 1024L * 1024L;
        private const long MaxExportXmlTotalBytes = 64L * 1024L * 1024L;
        private const long MaxExportWorkbookBytes = 64L * 1024L * 1024L;
        private static readonly DateTimeOffset FixedZipTimestamp = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private const string WorksheetRelationshipTypeHttp = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
        private const string WorksheetRelationshipTypeHttps = "https://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
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
            var admittedIssueCount = snapshot.Issues.Count;
            if (admittedIssueCount == 0) throw new InvalidDataException("Coordination issue workbook requires at least one issue.");
            if (admittedIssueCount > MaxExportIssueRows)
                throw new InvalidDataException("Coordination issue workbook exceeds the bounded export issue limit.");
            RequireStableExportIssueCount(snapshot, admittedIssueCount);

            var rows = CoordinationIssueExcelLifecycle.Project(snapshot);
            RequireStableExportIssueCount(snapshot, admittedIssueCount);
            if (rows.Count != admittedIssueCount)
                throw new InvalidDataException("Coordination issue workbook projected issue Count changed during export.");
            var metaRows = new List<IReadOnlyList<string>>
            {
                new[] { "SCHEMA", SchemaVersion },
                new[] { "PROJECT_ID", snapshot.ProjectId },
                new[] { "DRAWING_FINGERPRINT", snapshot.DrawingFingerprint },
                new[] { "WORKBOOK_REVISION", snapshot.Revision.ToString(CultureInfo.InvariantCulture) }
            };
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                long totalExportXmlBytes = 0L;
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var boundedArchive = new BoundedArchiveWriteStream(stream, MaxExportWorkbookBytes, true))
                {
                    using (var archive = new ZipArchive(boundedArchive, ZipArchiveMode.Create, true, StrictUtf8))
                    {
                        WriteEntry(archive, "[Content_Types].xml", ContentTypesXml, ref totalExportXmlBytes);
                        WriteEntry(archive, "_rels/.rels", RootRelationshipsXml, ref totalExportXmlBytes);
                        WriteEntry(archive, "xl/workbook.xml", WorkbookXml, ref totalExportXmlBytes);
                        WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml, ref totalExportXmlBytes);
                        WriteSheet(archive, "xl/worksheets/sheet1.xml", new[] { "KEY", "VALUE" }, metaRows, ref totalExportXmlBytes);
                        WriteIssueSheet(archive, "xl/worksheets/sheet2.xml", rows, admittedIssueCount, ref totalExportXmlBytes);
                    }
                    boundedArchive.Flush();
                    if (stream.Length <= 0L || stream.Length > MaxExportWorkbookBytes)
                        throw new InvalidDataException("Coordination issue workbook export exceeds the supported archive size.");
                    stream.Flush(true);
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

        private static void RequireStableExportIssueCount(CoordinationIssuePersistenceSnapshot snapshot, int admittedIssueCount)
        {
            if (snapshot.Issues.Count != admittedIssueCount)
                throw new InvalidDataException("Coordination issue workbook source issue Count changed during export.");
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
            var relationshipsById = new Dictionary<string, List<XElement>>(StringComparer.Ordinal);
            foreach (var relationship in relationships.Root.Elements(PackageRelationshipNs + "Relationship"))
            {
                var id = (string)relationship.Attribute("Id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                List<XElement> matches;
                if (!relationshipsById.TryGetValue(id, out matches))
                {
                    matches = new List<XElement>();
                    relationshipsById.Add(id, matches);
                }
                matches.Add(relationship);
            }

            var result = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in workbook.Descendants(SpreadsheetNs + "sheet"))
            {
                var name = (string)sheet.Attribute("name");
                var relationshipId = (string)sheet.Attribute(RelationshipNs + "id");
                List<XElement> matches;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relationshipId) ||
                    !relationshipsById.TryGetValue(relationshipId, out matches) || matches.Count != 1)
                    throw new InvalidDataException("Coordination issue workbook sheet relationship is missing or ambiguous.");

                var relationship = matches[0];
                var relationshipType = (string)relationship.Attribute("Type") ?? string.Empty;
                if (!IsWorksheetRelationshipType(relationshipType))
                    throw new InvalidDataException("Coordination issue workbook sheet relationship type is invalid.");

                var targetMode = (string)relationship.Attribute("TargetMode") ?? string.Empty;
                if (targetMode.Length != 0 && !string.Equals(targetMode, "Internal", StringComparison.Ordinal))
                    throw new InvalidDataException("Coordination issue workbook sheet relationship must be internal.");

                var target = (string)relationship.Attribute("Target");
                if (string.IsNullOrWhiteSpace(target))
                    throw new InvalidDataException("Coordination issue workbook sheet relationship target is invalid.");
                var normalized = target.Replace('\\', '/');
                if (ContainsParentTraversal(normalized))
                    throw new InvalidDataException("Coordination issue workbook sheet relationship target contains parent traversal.");
                normalized = normalized.TrimStart('/');
                if (!normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) normalized = "xl/" + normalized;
                var entry = RequiredEntry(archive, normalized);
                if (result.ContainsKey(name)) throw new InvalidDataException("Coordination issue workbook contains duplicate sheet name: " + name + ".");
                result.Add(name, entry);
            }
            return result;
        }

        private static bool IsWorksheetRelationshipType(string relationshipType)
        {
            return string.Equals(relationshipType, WorksheetRelationshipTypeHttp, StringComparison.Ordinal) ||
                   string.Equals(relationshipType, WorksheetRelationshipTypeHttps, StringComparison.Ordinal);
        }

        private static bool ContainsParentTraversal(string target)
        {
            var segments = target.Split('/');
            for (var i = 0; i < segments.Length; i++)
                if (string.Equals(segments[i], "..", StringComparison.Ordinal)) return true;
            return false;
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
                    || expectedRowIndex > MaxWorksheetRows
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
                if (result.Count > MaxWorksheetRows) throw new InvalidDataException("Coordination issue workbook exceeds the supported row count.");
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
                || parsedRowIndex > MaxWorksheetRows
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

        private static void WriteSheet(
            ZipArchive archive,
            string name,
            IReadOnlyList<string> headers,
            IReadOnlyList<IReadOnlyList<string>> rows,
            ref long totalExportXmlBytes)
        {
            using (var buffer = new MemoryStream())
            {
                using (var bounded = new BoundedEntryWriteStream(buffer, MaxExportXmlEntryBytes, true))
                using (var writer = new StreamWriter(bounded, StrictUtf8, 4096, true))
                {
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
                    WriteRow(writer, 1, headers);
                    for (var i = 0; i < rows.Count; i++) WriteRow(writer, i + 2, rows[i]);
                    writer.Write("</sheetData></worksheet>");
                    writer.Flush();
                }

                ReserveExportXmlBytes(ref totalExportXmlBytes, buffer.Length, name);
                buffer.Position = 0L;
                var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                entry.LastWriteTime = FixedZipTimestamp;
                using (var output = entry.Open()) buffer.CopyTo(output);
            }
        }

        private static void WriteIssueSheet(
            ZipArchive archive,
            string name,
            IReadOnlyList<CoordinationIssueExcelRow> rows,
            int admittedIssueCount,
            ref long totalExportXmlBytes)
        {
            using (var buffer = new MemoryStream())
            {
                using (var bounded = new BoundedEntryWriteStream(buffer, MaxExportXmlEntryBytes, true))
                using (var writer = new StreamWriter(bounded, StrictUtf8, 4096, true))
                {
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
                    WriteRow(writer, 1, IssueHeaders);
                    for (var i = 0; i < admittedIssueCount; i++)
                    {
                        if (rows.Count != admittedIssueCount)
                            throw new InvalidDataException("Coordination issue workbook projected issue Count changed during worksheet export.");
                        var row = rows[i];
                        WriteRow(writer, i + 2, new[]
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
                    if (rows.Count != admittedIssueCount)
                        throw new InvalidDataException("Coordination issue workbook projected issue Count changed during worksheet export.");
                    writer.Write("</sheetData></worksheet>");
                    writer.Flush();
                }

                ReserveExportXmlBytes(ref totalExportXmlBytes, buffer.Length, name);
                buffer.Position = 0L;
                var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                entry.LastWriteTime = FixedZipTimestamp;
                using (var output = entry.Open()) buffer.CopyTo(output);
            }
        }

        private static void WriteRow(TextWriter writer, int rowNumber, IReadOnlyList<string> values)
        {
            writer.Write("<row r=\"");
            writer.Write(rowNumber.ToString(CultureInfo.InvariantCulture));
            writer.Write("\">");
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i] ?? string.Empty;
                if (value.Length > 32767) throw new InvalidDataException("Coordination issue workbook cell exceeds the Excel text limit.");
                writer.Write("<c r=\"");
                writer.Write(CellReference(i, rowNumber));
                writer.Write("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">");
                writer.Write(SecurityElement.Escape(value) ?? string.Empty);
                writer.Write("</t></is></c>");
            }
            writer.Write("</row>");
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

        private static void WriteEntry(ZipArchive archive, string name, string content, ref long totalExportXmlBytes)
        {
            var bytes = StrictUtf8.GetBytes(content);
            if (bytes.LongLength > MaxExportXmlEntryBytes)
                throw new InvalidDataException("Coordination issue workbook XML part exceeds the export entry limit: " + name + ".");
            ReserveExportXmlBytes(ref totalExportXmlBytes, bytes.LongLength, name);
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = FixedZipTimestamp;
            using (var output = entry.Open()) output.Write(bytes, 0, bytes.Length);
        }

        private static void ReserveExportXmlBytes(ref long totalExportXmlBytes, long entryBytes, string name)
        {
            if (entryBytes < 0L || entryBytes > MaxExportXmlEntryBytes)
                throw new InvalidDataException("Coordination issue workbook XML part exceeds the export entry limit: " + name + ".");
            if (totalExportXmlBytes > MaxExportXmlTotalBytes - entryBytes)
                throw new InvalidDataException("Coordination issue workbook exceeds the aggregate uncompressed XML export limit.");
            totalExportXmlBytes += entryBytes;
        }

        private sealed class BoundedEntryWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;
            private readonly bool _leaveOpen;

            public BoundedEntryWriteStream(Stream inner, long maxLength, bool leaveOpen)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                if (maxLength <= 0L) throw new ArgumentOutOfRangeException(nameof(maxLength));
                _maxLength = maxLength;
                _leaveOpen = leaveOpen;
            }

            public override bool CanRead => false;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set { EnsureProjectedLength(value, 0); _inner.Position = value; } }
            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin)
            {
                var target = origin == SeekOrigin.Begin ? offset : origin == SeekOrigin.Current ? checked(_inner.Position + offset) : checked(_inner.Length + offset);
                EnsureProjectedLength(target, 0);
                return _inner.Seek(offset, origin);
            }
            public override void SetLength(long value)
            {
                EnsureProjectedLength(value, 0);
                _inner.SetLength(value);
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
                EnsureProjectedLength(_inner.Position, count);
                _inner.Write(buffer, offset, count);
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing && !_leaveOpen) _inner.Dispose();
                base.Dispose(disposing);
            }
            private void EnsureProjectedLength(long position, int additionalBytes)
            {
                if (position < 0L) throw new IOException("Cannot seek before the start of the bounded XLSX entry stream.");
                long projected;
                try { projected = checked(position + additionalBytes); }
                catch (OverflowException) { throw new InvalidDataException("Coordination issue workbook XML part exceeds the export entry limit."); }
                projected = Math.Max(projected, _inner.Length);
                if (projected > _maxLength) throw new InvalidDataException("Coordination issue workbook XML part exceeds the export entry limit.");
            }
        }

        private sealed class BoundedArchiveWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;
            private readonly bool _leaveOpen;

            public BoundedArchiveWriteStream(Stream inner, long maxLength, bool leaveOpen)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                if (maxLength <= 0L) throw new ArgumentOutOfRangeException(nameof(maxLength));
                _maxLength = maxLength;
                _leaveOpen = leaveOpen;
            }

            public override bool CanRead => false;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set { EnsureProjectedLength(value, 0); _inner.Position = value; } }
            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin)
            {
                var target = origin == SeekOrigin.Begin ? offset : origin == SeekOrigin.Current ? checked(_inner.Position + offset) : checked(_inner.Length + offset);
                EnsureProjectedLength(target, 0);
                return _inner.Seek(offset, origin);
            }
            public override void SetLength(long value)
            {
                EnsureProjectedLength(value, 0);
                _inner.SetLength(value);
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
                EnsureProjectedLength(_inner.Position, count);
                _inner.Write(buffer, offset, count);
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing && !_leaveOpen) _inner.Dispose();
                base.Dispose(disposing);
            }
            private void EnsureProjectedLength(long position, int additionalBytes)
            {
                if (position < 0L) throw new IOException("Cannot seek before the start of the bounded XLSX archive stream.");
                long projected;
                try { projected = checked(position + additionalBytes); }
                catch (OverflowException) { throw new InvalidDataException("Coordination issue workbook export exceeds the supported archive size."); }
                projected = Math.Max(projected, _inner.Length);
                if (projected > _maxLength) throw new InvalidDataException("Coordination issue workbook export exceeds the supported archive size.");
            }
        }

        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"META\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"ISSUES\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/></Relationships>";
    }
}
