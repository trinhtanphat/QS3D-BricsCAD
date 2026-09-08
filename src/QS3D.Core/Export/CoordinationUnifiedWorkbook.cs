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
        private const int MaxExportDataRowsPerSheet = 10000;
        private const int MaxExportTraceRows = MaxExportDataRowsPerSheet * 2;
        private const long MaxExportXmlEntryBytes = 32L * 1024L * 1024L;
        private const long MaxExportXmlTotalBytes = 64L * 1024L * 1024L;
        private const long MaxExportWorkbookBytes = 64L * 1024L * 1024L;
        private static readonly DateTimeOffset CanonicalZipTimestamp =
            new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
            if (clashes.Count > MaxExportDataRowsPerSheet || duplicates.Count > MaxExportDataRowsPerSheet)
                throw new InvalidDataException("Coordination workbook exceeds the bounded export row limit.");

            var clashRows = SnapshotClashes(clashes);
            var duplicateRows = SnapshotDuplicates(duplicates);
            RequireOneDrawing(clashRows, duplicateRows);
            var traceCapacity = checked(clashRows.Count + duplicateRows.Count);
            if (traceCapacity > MaxExportTraceRows)
                throw new InvalidDataException("Coordination workbook trace model exceeds the bounded export row limit.");
            var traces = new List<TraceProjection>(traceCapacity);

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                var xmlBudget = new ExportXmlBudget();
                using (var file = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var boundedArchive = new BoundedArchiveWriteStream(file, MaxExportWorkbookBytes))
                using (var archive = new ZipArchive(boundedArchive, ZipArchiveMode.Create, false, Encoding.UTF8))
                {
                    WriteEntry(archive, "[Content_Types].xml", ContentTypesXml, xmlBudget);
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml, xmlBudget);
                    WriteEntry(archive, "xl/workbook.xml", WorkbookXml, xmlBudget);
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml, xmlBudget);
                    WriteClashSheet(archive, clashRows, traces, xmlBudget);
                    WriteDuplicateSheet(archive, duplicateRows, traces, xmlBudget);
                    WriteTraceSheet(archive, traces, xmlBudget);
                }
                if (new FileInfo(tempPath).Length > MaxExportWorkbookBytes)
                    throw new InvalidDataException("Coordination workbook archive exceeds the bounded export size.");
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

        private static void WriteClashSheet(
            ZipArchive archive,
            IReadOnlyList<CoordinationClashExportRow> rows,
            ICollection<TraceProjection> traces,
            ExportXmlBudget budget)
        {
            var headers = new[]
            {
                "STT", "CLASH_ID", "TYPE", "SEVERITY", "STATUS", "FLOOR",
                "ELEMENT_A_ID", "ELEMENT_A_HANDLE", "ELEMENT_A_CATEGORY",
                "ELEMENT_B_ID", "ELEMENT_B_HANDLE", "ELEMENT_B_CATEGORY",
                "RULE_ID", "DRAWING_FINGERPRINT", "COMMENT", TraceHeader
            };
            WriteSheet(archive, "xl/worksheets/sheet1.xml", headers, rows.Count, (writer, index) =>
            {
                var row = rows[index];
                var excelRow = index + 2;
                var traceKey = CoordinationWorkbookIdentity.BuildTraceKey(
                    row.ClashId, row.DrawingFingerprint, row.RuleId, row.LeftHandle, row.RightHandle, ClashSheet);
                traces.Add(new TraceProjection(traceKey, ClashSheet, excelRow, row.ClashId, row.LeftHandle, row.RightHandle, row.DrawingFingerprint, row.RuleId));
                WriteRow(writer, excelRow, new[]
                {
                    (index + 1).ToString(CultureInfo.InvariantCulture), row.ClashId, row.Type, row.Severity, row.Status, row.Floor,
                    row.LeftElementId, row.LeftHandle, row.LeftCategory, row.RightElementId, row.RightHandle, row.RightCategory,
                    row.RuleId, row.DrawingFingerprint, row.Comment, traceKey
                });
            }, budget);
        }

        private static void WriteDuplicateSheet(
            ZipArchive archive,
            IReadOnlyList<CoordinationDuplicateExportRow> rows,
            ICollection<TraceProjection> traces,
            ExportXmlBudget budget)
        {
            var headers = new[]
            {
                "STT", "DUPLICATE_ID", "MATCH_KINDS", "FLOOR",
                "ELEMENT_A_ID", "ELEMENT_A_HANDLE", "ELEMENT_A_CATEGORY",
                "ELEMENT_B_ID", "ELEMENT_B_HANDLE", "ELEMENT_B_CATEGORY",
                "RULE_ID", "DRAWING_FINGERPRINT", "COMMENT", TraceHeader
            };
            WriteSheet(archive, "xl/worksheets/sheet2.xml", headers, rows.Count, (writer, index) =>
            {
                var row = rows[index];
                var excelRow = index + 2;
                var traceKey = CoordinationWorkbookIdentity.BuildTraceKey(
                    row.DuplicateId, row.DrawingFingerprint, row.RuleId, row.LeftHandle, row.RightHandle, DuplicateSheet);
                traces.Add(new TraceProjection(traceKey, DuplicateSheet, excelRow, row.DuplicateId, row.LeftHandle, row.RightHandle, row.DrawingFingerprint, row.RuleId));
                WriteRow(writer, excelRow, new[]
                {
                    (index + 1).ToString(CultureInfo.InvariantCulture), row.DuplicateId, row.MatchKindsText, row.Floor,
                    row.LeftElementId, row.LeftHandle, row.LeftCategory, row.RightElementId, row.RightHandle, row.RightCategory,
                    row.RuleId, row.DrawingFingerprint, row.Comment, traceKey
                });
            }, budget);
        }

        private static void WriteTraceSheet(ZipArchive archive, IReadOnlyList<TraceProjection> traces, ExportXmlBudget budget)
        {
            var headers = new[] { TraceHeader, "SHEET", "ROW", "ITEM_ID", "LEFT_HANDLE", "RIGHT_HANDLE", "DRAWING_FINGERPRINT", "RULE_ID" };
            WriteSheet(archive, "xl/worksheets/sheet3.xml", headers, traces.Count, (writer, index) =>
            {
                var trace = traces[index];
                WriteRow(writer, index + 2, new[]
                {
                    trace.TraceKey, trace.Sheet, trace.Row.ToString(CultureInfo.InvariantCulture), trace.ItemId,
                    trace.LeftHandle, trace.RightHandle, trace.DrawingFingerprint, trace.RuleId
                });
            }, budget);
        }

        private delegate void DataRowWriter(TextWriter writer, int index);

        private static void WriteSheet(
            ZipArchive archive,
            string name,
            IReadOnlyList<string> headers,
            int rowCount,
            DataRowWriter writeDataRow,
            ExportXmlBudget budget)
        {
            var entry = CreateCanonicalEntry(archive, name);
            using (var raw = entry.Open())
            using (var bounded = new BoundedXmlEntryWriteStream(raw, MaxExportXmlEntryBytes, budget))
            using (var writer = new StreamWriter(bounded, new UTF8Encoding(false), 4096, false))
            {
                writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
                WriteRow(writer, 1, headers);
                for (var index = 0; index < rowCount; index++) writeDataRow(writer, index);
                writer.Write("</sheetData></worksheet>");
            }
        }

        private static void WriteRow(TextWriter writer, int rowNumber, IReadOnlyList<string> values)
        {
            writer.Write("<row r=\"");
            writer.Write(rowNumber.ToString(CultureInfo.InvariantCulture));
            writer.Write("\">");
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index] ?? string.Empty;
                if (value.Length > 32767) throw new InvalidDataException("Coordination workbook cell exceeds the Excel text limit.");
                writer.Write("<c r=\"");
                writer.Write(Cell(index, rowNumber));
                writer.Write("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">");
                writer.Write(SecurityElement.Escape(value) ?? string.Empty);
                writer.Write("</t></is></c>");
            }
            writer.Write("</row>");
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

        private static void WriteEntry(ZipArchive archive, string name, string content, ExportXmlBudget budget)
        {
            var entry = CreateCanonicalEntry(archive, name);
            using (var raw = entry.Open())
            using (var bounded = new BoundedXmlEntryWriteStream(raw, MaxExportXmlEntryBytes, budget))
            using (var writer = new StreamWriter(bounded, new UTF8Encoding(false), 4096, false))
            {
                writer.Write(content);
            }
        }

        private static ZipArchiveEntry CreateCanonicalEntry(ZipArchive archive, string name)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = CanonicalZipTimestamp;
            return entry;
        }

        private sealed class ExportXmlBudget
        {
            private long _bytes;

            internal void ReserveExportXmlBytes(int count)
            {
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                var projected = checked(_bytes + count);
                if (projected > MaxExportXmlTotalBytes)
                    throw new InvalidDataException("Coordination workbook XML exceeds the bounded aggregate export size.");
                _bytes = projected;
            }
        }

        private sealed class BoundedXmlEntryWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxBytes;
            private readonly ExportXmlBudget _budget;
            private long _written;

            internal BoundedXmlEntryWriteStream(Stream inner, long maxBytes, ExportXmlBudget budget)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _maxBytes = maxBytes;
                _budget = budget ?? throw new ArgumentNullException(nameof(budget));
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => _written;
            public override long Position { get => _written; set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
                var projected = checked(_written + count);
                if (projected > _maxBytes)
                    throw new InvalidDataException("Coordination workbook XML entry exceeds the bounded export size.");
                _budget.ReserveExportXmlBytes(count);
                _inner.Write(buffer, offset, count);
                _written = projected;
            }
        }

        private sealed class BoundedArchiveWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxBytes;

            internal BoundedArchiveWriteStream(Stream inner, long maxBytes)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                if (!_inner.CanSeek || !_inner.CanWrite) throw new ArgumentException("Archive stream must be seekable and writable.", nameof(inner));
                _maxBytes = maxBytes;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

            public override void SetLength(long value)
            {
                if (value < 0 || value > _maxBytes)
                    throw new InvalidDataException("Coordination workbook archive exceeds the bounded export size.");
                _inner.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
                var end = checked(_inner.Position + count);
                var projected = Math.Max(_inner.Length, end);
                if (projected > _maxBytes)
                    throw new InvalidDataException("Coordination workbook archive exceeds the bounded export size.");
                _inner.Write(buffer, offset, count);
            }
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
