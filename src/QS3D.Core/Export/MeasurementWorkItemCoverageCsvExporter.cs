using System;
using System.Globalization;
using System.IO;
using System.Text;
using QS3D.Core.Mapping;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public static class MeasurementWorkItemCoverageCsvExporter
    {
        private const string Header = "Category,MeasurementItemId,MappingId,ClassificationId,WorkItemId,IsReady,Issues,FindingCount,AffectedElementCount,AffectedElementIds";
        private const string ProvenanceHeader = ",SourceProjectId,SourceDrawingFingerprint,SourceChangeVersion,SourceUpdatedUtc";
        private const long MaxCsvBytes = 64L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8WithBom = new UTF8Encoding(true, true);

        public static void Export(string path, MeasurementWorkItemCoverageMatrix matrix)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            ValidateSemanticIdentities(matrix);

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var streamWriter = new StreamWriter(stream, StrictUtf8WithBom, 4096, leaveOpen: true))
                using (var writer = new BoundedUtf8TextWriter(
                    streamWriter,
                    MaxCsvBytes,
                    StrictUtf8WithBom.GetPreamble().Length))
                {
                    WriteCsv(writer, matrix);
                    writer.Flush();
                    streamWriter.Flush();
                    stream.Flush(true);
                }

                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        public static string ToCsv(MeasurementWorkItemCoverageMatrix matrix)
        {
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            ValidateSemanticIdentities(matrix);

            using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            using var writer = new BoundedUtf8TextWriter(
                stringWriter,
                MaxCsvBytes,
                StrictUtf8WithBom.GetPreamble().Length);
            WriteCsv(writer, matrix);
            writer.Flush();
            return stringWriter.ToString();
        }

        private static void WriteCsv(TextWriter writer, MeasurementWorkItemCoverageMatrix matrix)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            var provenance = matrix.Provenance;

            writer.Write(Header);
            if (provenance != null) writer.Write(ProvenanceHeader);
            writer.Write("\r\n");

            for (var i = 0; i < matrix.Cells.Count; i++)
            {
                var cell = matrix.Cells[i];
                if (cell == null)
                    throw new ArgumentException("Coverage matrix contains a null cell at index " + i + ".", nameof(matrix));

                WriteQuotedCsvValue(writer, cell.Category.ToString());
                writer.Write(',');
                WriteQuotedCsvValue(writer, cell.MeasurementItemId);
                writer.Write(',');
                WriteQuotedCsvValue(writer, cell.MappingId);
                writer.Write(',');
                WriteQuotedCsvValue(writer, cell.ClassificationId);
                writer.Write(',');
                WriteQuotedCsvValue(writer, cell.WorkItemId);
                writer.Write(',');
                writer.Write(cell.IsReady ? "true" : "false");
                writer.Write(',');
                WriteJoinedIssues(writer, cell.Issues);
                writer.Write(',');
                writer.Write(cell.FindingCount.ToString(CultureInfo.InvariantCulture));
                writer.Write(',');
                writer.Write(cell.AffectedElementCount.ToString(CultureInfo.InvariantCulture));
                writer.Write(',');
                WriteJoinedValues(writer, cell.AffectedElementIds);

                if (provenance != null)
                {
                    writer.Write(',');
                    WriteQuotedCsvValue(writer, provenance.ProjectId);
                    writer.Write(',');
                    WriteQuotedCsvValue(writer, provenance.DrawingFingerprint);
                    writer.Write(',');
                    writer.Write(provenance.ChangeVersion.ToString(CultureInfo.InvariantCulture));
                    writer.Write(',');
                    WriteQuotedCsvValue(writer, provenance.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
                }

                writer.Write("\r\n");
            }
        }

        private static void WriteQuotedCsvValue(TextWriter writer, string? value)
        {
            var safe = value ?? string.Empty;
            writer.Write('"');
            if (RequiresSpreadsheetFormulaEscape(safe)) writer.Write('\'');
            WriteEscapedCsvFragment(writer, safe);
            writer.Write('"');
        }

        private static void WriteJoinedIssues(TextWriter writer, System.Collections.Generic.IReadOnlyList<MeasurementWorkItemCoverageIssue> issues)
        {
            writer.Write('"');
            for (var i = 0; i < issues.Count; i++)
            {
                if (i != 0) writer.Write('|');
                WriteEscapedCsvFragment(writer, issues[i].ToString());
            }
            writer.Write('"');
        }

        private static void WriteJoinedValues(TextWriter writer, System.Collections.Generic.IReadOnlyList<string> values)
        {
            writer.Write('"');
            for (var i = 0; i < values.Count; i++)
            {
                if (i != 0) writer.Write('|');
                WriteEscapedCsvFragment(writer, values[i] ?? string.Empty);
            }
            writer.Write('"');
        }

        private static void WriteEscapedCsvFragment(TextWriter writer, string value)
        {
            var start = 0;
            while (start < value.Length)
            {
                var quote = value.IndexOf('"', start);
                if (quote < 0)
                {
                    writer.Write(start == 0 ? value : value.Substring(start));
                    return;
                }

                if (quote > start) writer.Write(value.Substring(start, quote - start));
                writer.Write("\"\"");
                start = quote + 1;
            }
        }

        private static void ValidateSemanticIdentities(MeasurementWorkItemCoverageMatrix matrix)
        {
            var provenance = matrix.Provenance;
            if (provenance != null)
            {
                RequireLiteralCsvIdentity(provenance.ProjectId, "source project id");
                RequireLiteralCsvIdentity(provenance.DrawingFingerprint, "source drawing fingerprint");
            }

            for (var i = 0; i < matrix.Cells.Count; i++)
            {
                var cell = matrix.Cells[i];
                if (cell == null)
                    throw new ArgumentException("Coverage matrix contains a null cell at index " + i + ".", nameof(matrix));

                RequireLiteralCsvIdentity(cell.MeasurementItemId, "measurement item id");
                RequireLiteralCsvIdentity(cell.MappingId, "mapping id");
                RequireLiteralCsvIdentity(cell.ClassificationId, "classification id");
                RequireLiteralCsvIdentity(cell.WorkItemId, "work-item id");
                for (var elementIndex = 0; elementIndex < cell.AffectedElementIds.Count; elementIndex++)
                    RequireLiteralCsvIdentity(cell.AffectedElementIds[elementIndex], "affected element id");
            }
        }

        private static void RequireLiteralCsvIdentity(string? value, string label)
        {
            if (RequiresSpreadsheetFormulaEscape(value))
                throw new InvalidDataException(
                    "Coverage CSV " + label + " cannot begin with a spreadsheet formula prefix because semantic identity must be preserved exactly.");
        }

        private static bool RequiresSpreadsheetFormulaEscape(string? value)
        {
            var probe = (value ?? string.Empty).TrimStart();
            return probe.Length > 0 && (probe[0] == '=' || probe[0] == '+' || probe[0] == '-' || probe[0] == '@');
        }

        private sealed class BoundedUtf8TextWriter : TextWriter
        {
            private readonly TextWriter inner;
            private readonly long maxBytes;
            private long bytesWritten;

            public BoundedUtf8TextWriter(TextWriter inner, long maxBytes, long initialBytes)
            {
                this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
                if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
                if (initialBytes < 0 || initialBytes > maxBytes) throw new ArgumentOutOfRangeException(nameof(initialBytes));
                this.maxBytes = maxBytes;
                bytesWritten = initialBytes;
            }

            public override Encoding Encoding => StrictUtf8WithBom;

            public override void Write(char value)
            {
                Write(value.ToString());
            }

            public override void Write(string? value)
            {
                if (string.IsNullOrEmpty(value)) return;
                var byteCount = StrictUtf8WithBom.GetByteCount(value);
                var next = checked(bytesWritten + byteCount);
                if (next > maxBytes)
                    throw new InvalidDataException(
                        "Measurement work-item coverage CSV exceeds the bounded output limit of " +
                        maxBytes.ToString(CultureInfo.InvariantCulture) + " UTF-8 bytes.");
                inner.Write(value);
                bytesWritten = next;
            }

            public override void Flush()
            {
                inner.Flush();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) inner.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
