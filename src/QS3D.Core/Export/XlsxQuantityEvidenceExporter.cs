using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Xml;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    /// <summary>
    /// Writes the deterministic quantity evidence projection to a compact XLSX
    /// workbook. All quantity values and operands are copied from QuantityExplanation;
    /// no takeoff formula or geometry is evaluated by this exporter.
    /// </summary>
    public static class XlsxQuantityEvidenceExporter
    {
        private const int MaxDataRows = 1048575;
        private const int MaxCellTextCharacters = 32767;
        private const long MaxWorksheetEntryBytes = 32L * 1024L * 1024L;
        private const long MaxTotalUncompressedBytes = 64L * 1024L * 1024L;
        private const long MaxArchiveBytes = 64L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly DateTimeOffset FixedZipTimestamp = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static readonly string[] Headers =
        {
            "EvidenceId", "ParentEvidenceId", "RecordKind", "SubjectKey", "Category", "Metric", "Unit",
            "GrossValue", "NetValue", "Value", "Operation", "SemanticKey", "FormulaOrReason", "SelectorKind",
            "SelectorKey", "SourceReference", "TargetReference", "Operands"
        };

        public static void Export(string path, IReadOnlyList<QuantityExplanation> explanations)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Export path is required.", nameof(path));
            if (explanations == null)
                throw new ArgumentNullException(nameof(explanations));

            var snapshot = SnapshotExplanations(explanations);
            ValidateProjectedRowCapacity(snapshot);
            var rows = QuantityEvidenceExportProjection.CreateMany(snapshot);
            if (rows.Count > MaxDataRows)
                throw new ArgumentOutOfRangeException(nameof(explanations), "Quantity evidence XLSX export supports at most " + MaxDataRows + " data rows.");

            ValidateRows(rows);
            WritePackage(path, rows);
        }

        private static IReadOnlyList<QuantityExplanation> SnapshotExplanations(
            IReadOnlyList<QuantityExplanation> explanations)
        {
            var count = explanations.Count;
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(explanations), "Quantity evidence XLSX explanation count must be non-negative.");
            if (count > MaxDataRows)
                throw new ArgumentOutOfRangeException(nameof(explanations), "Quantity evidence XLSX export supports at most " + MaxDataRows + " explanations.");

            var snapshot = new QuantityExplanation[count];
            for (var index = 0; index < count; index++)
            {
                if (explanations.Count != count)
                    throw new InvalidOperationException("Quantity evidence XLSX explanation count changed during snapshot.");

                var explanation = explanations[index];
                if (explanation == null)
                    throw new ArgumentException("Quantity explanations cannot contain null entries.", nameof(explanations));
                snapshot[index] = explanation;
            }

            if (explanations.Count != count)
                throw new InvalidOperationException("Quantity evidence XLSX explanation count changed during snapshot.");

            return snapshot;
        }

        private static void ValidateProjectedRowCapacity(IReadOnlyList<QuantityExplanation> snapshot)
        {
            long projectedRows = 0;
            for (var index = 0; index < snapshot.Count; index++)
            {
                var explanation = snapshot[index];
                if (explanation == null)
                    throw new ArgumentException("Quantity explanations cannot contain null entries.", nameof(snapshot));

                projectedRows = AddProjectedRows(
                    projectedRows,
                    explanation.Contributions.Count,
                    explanation.Adjustments.Count);
            }
        }

        private static long AddProjectedRows(long projectedRows, int contributionCount, int adjustmentCount)
        {
            if (projectedRows < 0 || projectedRows > MaxDataRows)
                throw new ArgumentOutOfRangeException(nameof(projectedRows));
            if (contributionCount < 0)
                throw new ArgumentOutOfRangeException(nameof(contributionCount));
            if (adjustmentCount < 0)
                throw new ArgumentOutOfRangeException(nameof(adjustmentCount));

            var additionalRows = 1L + contributionCount + adjustmentCount;
            if (additionalRows > MaxDataRows - projectedRows)
                throw new ArgumentOutOfRangeException(
                    "explanations",
                    "Quantity evidence XLSX export supports at most " + MaxDataRows + " data rows.");

            return projectedRows + additionalRows;
        }

        private static void ValidateRows(IReadOnlyList<QuantityEvidenceExportRecord> rows)
        {
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                ValidateText(row.EvidenceId, index, "EvidenceId");
                ValidateText(row.ParentEvidenceId, index, "ParentEvidenceId");
                ValidateText(row.RecordKind, index, "RecordKind");
                ValidateText(row.SubjectKey, index, "SubjectKey");
                ValidateText(row.Category, index, "Category");
                ValidateText(row.Metric, index, "Metric");
                ValidateText(row.Unit, index, "Unit");
                ValidateText(row.Operation, index, "Operation");
                ValidateText(row.SemanticKey, index, "SemanticKey");
                ValidateText(row.FormulaOrReason, index, "FormulaOrReason");
                ValidateText(row.SelectorKind, index, "SelectorKind");
                ValidateText(row.SelectorKey, index, "SelectorKey");
                ValidateText(row.SourceReference, index, "SourceReference");
                ValidateText(row.TargetReference, index, "TargetReference");
                ValidateText(row.Operands, index, "Operands");
            }
        }

        private static void ValidateText(string value, int rowIndex, string field)
        {
            var text = value ?? string.Empty;
            if (text.Length > MaxCellTextCharacters)
                throw new InvalidDataException(
                    "Quantity evidence XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) +
                    " field " + field + " exceeds Excel's cell text limit.");

            for (var index = 0; index < text.Length; index++)
            {
                var ch = text[index];
                if (ch == '\t' || ch == '\n' || ch == '\r') continue;
                if (ch < 0x20)
                    throw new InvalidDataException(
                        "Quantity evidence XLSX field " + field + " contains an invalid XML control character.");
            }

            try
            {
                XmlConvert.VerifyXmlChars(text);
            }
            catch (XmlException ex)
            {
                throw new InvalidDataException(
                    "Quantity evidence XLSX field " + field + " contains malformed XML text or UTF-16.",
                    ex);
            }
        }

        private static void WritePackage(string path, IReadOnlyList<QuantityEvidenceExportRecord> rows)
        {
            WritePackage(path, rows, AtomicFileCommit.ReplaceWithoutBackup);
        }
        private static void WritePackage(
            string path,
            IReadOnlyList<QuantityEvidenceExportRecord> rows,
            Action<string, string> commit)
        {
            if (commit == null) throw new ArgumentNullException(nameof(commit));
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temporaryPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                long totalUncompressedBytes = 0L;
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var boundedArchive = new BoundedArchiveWriteStream(stream, MaxArchiveBytes))
                using (var archive = new ZipArchive(boundedArchive, ZipArchiveMode.Create, true, Encoding.UTF8))
                {
                    WriteEntry(archive, "[Content_Types].xml", ContentTypesXml(), ref totalUncompressedBytes);
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml(), ref totalUncompressedBytes);
                    WriteEntry(archive, "xl/workbook.xml", WorkbookXml(), ref totalUncompressedBytes);
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml(), ref totalUncompressedBytes);
                    WriteWorksheet(archive, "xl/worksheets/sheet1.xml", rows, ref totalUncompressedBytes);
                }
                var archiveLength = new FileInfo(temporaryPath).Length;
                if (archiveLength <= 0L || archiveLength > MaxArchiveBytes)
                    throw new InvalidDataException("Quantity evidence XLSX archive exceeds the bounded output contract.");
                commit(temporaryPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(temporaryPath);
            }
        }
        private static void WriteEntry(
            ZipArchive archive,
            string name,
            string content,
            ref long totalUncompressedBytes)
        {
            var bytes = StrictUtf8.GetBytes(content);
            ReserveUncompressed(bytes.LongLength, ref totalUncompressedBytes, name);
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = FixedZipTimestamp;
            using (var target = entry.Open())
                target.Write(bytes, 0, bytes.Length);
        }
        private static void WriteWorksheet(
            ZipArchive archive,
            string name,
            IReadOnlyList<QuantityEvidenceExportRecord> rows,
            ref long totalUncompressedBytes)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = FixedZipTimestamp;
            long worksheetBytes;
            using (var target = entry.Open())
            using (var boundedEntry = new BoundedEntryWriteStream(target, MaxWorksheetEntryBytes))
            {
                using (var writer = new StreamWriter(boundedEntry, StrictUtf8, 4096, true))
                    WriteWorksheetXml(writer, rows);
                worksheetBytes = boundedEntry.BytesWritten;
            }
            ReserveUncompressed(worksheetBytes, ref totalUncompressedBytes, name);
        }
        private static void ReserveUncompressed(long entryBytes, ref long totalUncompressedBytes, string name)
        {
            if (entryBytes < 0L || entryBytes > MaxWorksheetEntryBytes)
                throw new InvalidDataException("Quantity evidence XLSX worksheet exceeds the bounded entry contract: " + name + ".");
            var projectedTotal = checked(totalUncompressedBytes + entryBytes);
            if (projectedTotal > MaxTotalUncompressedBytes)
                throw new InvalidDataException("Quantity evidence XLSX package exceeds the bounded uncompressed output contract.");
            totalUncompressedBytes = projectedTotal;
        }
        private static string ContentTypesXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                   "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                   "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                   "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                   "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                   "</Types>";
        }

        private static string RootRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                   "</Relationships>";
        }

        private static string WorkbookXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                   "<sheets><sheet name=\"EVIDENCE\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                   "</workbook>";
        }

        private static string WorkbookRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                   "</Relationships>";
        }

        private static void WriteWorksheetXml(TextWriter writer, IReadOnlyList<QuantityEvidenceExportRecord> rows)
        {
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            writer.Write("<row r=\"1\">");
            for (var column = 0; column < Headers.Length; column++)
                WriteTextCell(writer, CellReference(column, 1), Headers[column]);
            writer.Write("</row>");

            for (var index = 0; index < rows.Count; index++)
            {
                var rowNumber = index + 2;
                var row = rows[index];
                writer.Write("<row r=\"");
                writer.Write(rowNumber.ToString(CultureInfo.InvariantCulture));
                writer.Write("\">");
                WriteTextCell(writer, CellReference(0, rowNumber), row.EvidenceId);
                WriteTextCell(writer, CellReference(1, rowNumber), row.ParentEvidenceId);
                WriteTextCell(writer, CellReference(2, rowNumber), row.RecordKind);
                WriteTextCell(writer, CellReference(3, rowNumber), row.SubjectKey);
                WriteTextCell(writer, CellReference(4, rowNumber), row.Category);
                WriteTextCell(writer, CellReference(5, rowNumber), row.Metric);
                WriteTextCell(writer, CellReference(6, rowNumber), row.Unit);
                WriteNumberCell(writer, CellReference(7, rowNumber), row.GrossValue);
                WriteNumberCell(writer, CellReference(8, rowNumber), row.NetValue);
                WriteNumberCell(writer, CellReference(9, rowNumber), row.Value);
                WriteTextCell(writer, CellReference(10, rowNumber), row.Operation);
                WriteTextCell(writer, CellReference(11, rowNumber), row.SemanticKey);
                WriteTextCell(writer, CellReference(12, rowNumber), row.FormulaOrReason);
                WriteTextCell(writer, CellReference(13, rowNumber), row.SelectorKind);
                WriteTextCell(writer, CellReference(14, rowNumber), row.SelectorKey);
                WriteTextCell(writer, CellReference(15, rowNumber), row.SourceReference);
                WriteTextCell(writer, CellReference(16, rowNumber), row.TargetReference);
                WriteTextCell(writer, CellReference(17, rowNumber), row.Operands);
                writer.Write("</row>");
            }

            writer.Write("</sheetData></worksheet>");
        }

        private static void WriteTextCell(TextWriter writer, string reference, string value)
        {
            writer.Write("<c r=\"");
            writer.Write(reference);
            writer.Write("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">");
            writer.Write(Escape(value ?? string.Empty));
            writer.Write("</t></is></c>");
        }

        private static void WriteNumberCell(TextWriter writer, string reference, decimal value)
        {
            writer.Write("<c r=\"");
            writer.Write(reference);
            writer.Write("\"><v>");
            writer.Write(value.ToString("G29", CultureInfo.InvariantCulture));
            writer.Write("</v></c>");
        }

        private static string Escape(string value)
        {
            return SecurityElement.Escape(value) ?? string.Empty;
        }

        private static string CellReference(int zeroBasedColumn, int oneBasedRow)
        {
            var column = zeroBasedColumn + 1;
            var letters = string.Empty;
            while (column > 0)
            {
                column--;
                letters = (char)('A' + column % 26) + letters;
                column /= 26;
            }
            return letters + oneBasedRow.ToString(CultureInfo.InvariantCulture);
        }

        private sealed class BoundedEntryWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;
            private long _bytesWritten;

            internal BoundedEntryWriteStream(Stream inner, long maxLength)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                if (maxLength < 0L) throw new ArgumentOutOfRangeException(nameof(maxLength));
                _maxLength = maxLength;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public long BytesWritten => _bytesWritten;
            public override long Length => _bytesWritten;
            public override long Position
            {
                get => _bytesWritten;
                set => throw new NotSupportedException();
            }

            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                var projectedLength = checked(_bytesWritten + count);
                if (projectedLength > _maxLength) throw EntrySizeExceeded();
                _inner.Write(buffer, offset, count);
                _bytesWritten = projectedLength;
            }

            public override void WriteByte(byte value)
            {
                var projectedLength = checked(_bytesWritten + 1L);
                if (projectedLength > _maxLength) throw EntrySizeExceeded();
                _inner.WriteByte(value);
                _bytesWritten = projectedLength;
            }

            private static InvalidDataException EntrySizeExceeded()
                => new InvalidDataException("Quantity evidence XLSX worksheet exceeds the bounded entry contract.");
        }

        private sealed class BoundedArchiveWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;
            internal BoundedArchiveWriteStream(Stream inner, long maxLength)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                if (maxLength < 0L) throw new ArgumentOutOfRangeException(nameof(maxLength));
                _maxLength = maxLength;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position
            {
                get => _inner.Position;
                set
                {
                    if (value < 0L || value > _maxLength) throw ArchiveSizeExceeded();
                    _inner.Position = value;
                }
            }

            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

            public override long Seek(long offset, SeekOrigin origin)
            {
                long target;
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        target = offset;
                        break;
                    case SeekOrigin.Current:
                        target = checked(_inner.Position + offset);
                        break;
                    case SeekOrigin.End:
                        target = checked(_inner.Length + offset);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin));
                }
                if (target < 0L || target > _maxLength) throw ArchiveSizeExceeded();
                return _inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                if (value < 0L || value > _maxLength) throw ArchiveSizeExceeded();
                _inner.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                var projectedLength = Math.Max(_inner.Length, checked(_inner.Position + count));
                if (projectedLength > _maxLength) throw ArchiveSizeExceeded();
                _inner.Write(buffer, offset, count);
            }

            public override void WriteByte(byte value)
            {
                var projectedLength = Math.Max(_inner.Length, checked(_inner.Position + 1L));
                if (projectedLength > _maxLength) throw ArchiveSizeExceeded();
                _inner.WriteByte(value);
            }

            private static InvalidDataException ArchiveSizeExceeded()
                => new InvalidDataException("Quantity evidence XLSX archive exceeds the bounded output contract.");
        }
    }
}
