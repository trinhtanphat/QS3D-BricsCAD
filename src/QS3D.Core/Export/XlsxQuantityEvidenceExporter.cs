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
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
                {
                    WriteEntry(archive, "[Content_Types].xml", ContentTypesXml());
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml());
                    WriteEntry(archive, "xl/workbook.xml", WorkbookXml());
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml());
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(rows));
                }

                commit(temporaryPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(temporaryPath);
            }
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(content);
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

        private static string WorksheetXml(IReadOnlyList<QuantityEvidenceExportRecord> rows)
        {
            var builder = new StringBuilder(4096 + rows.Count * 1024);
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            builder.Append("<row r=\"1\">");
            for (var column = 0; column < Headers.Length; column++)
                AppendTextCell(builder, CellReference(column, 1), Headers[column]);
            builder.Append("</row>");

            for (var index = 0; index < rows.Count; index++)
            {
                var rowNumber = index + 2;
                var row = rows[index];
                builder.Append("<row r=\"").Append(rowNumber.ToString(CultureInfo.InvariantCulture)).Append("\">");
                AppendTextCell(builder, CellReference(0, rowNumber), row.EvidenceId);
                AppendTextCell(builder, CellReference(1, rowNumber), row.ParentEvidenceId);
                AppendTextCell(builder, CellReference(2, rowNumber), row.RecordKind);
                AppendTextCell(builder, CellReference(3, rowNumber), row.SubjectKey);
                AppendTextCell(builder, CellReference(4, rowNumber), row.Category);
                AppendTextCell(builder, CellReference(5, rowNumber), row.Metric);
                AppendTextCell(builder, CellReference(6, rowNumber), row.Unit);
                AppendNumberCell(builder, CellReference(7, rowNumber), row.GrossValue);
                AppendNumberCell(builder, CellReference(8, rowNumber), row.NetValue);
                AppendNumberCell(builder, CellReference(9, rowNumber), row.Value);
                AppendTextCell(builder, CellReference(10, rowNumber), row.Operation);
                AppendTextCell(builder, CellReference(11, rowNumber), row.SemanticKey);
                AppendTextCell(builder, CellReference(12, rowNumber), row.FormulaOrReason);
                AppendTextCell(builder, CellReference(13, rowNumber), row.SelectorKind);
                AppendTextCell(builder, CellReference(14, rowNumber), row.SelectorKey);
                AppendTextCell(builder, CellReference(15, rowNumber), row.SourceReference);
                AppendTextCell(builder, CellReference(16, rowNumber), row.TargetReference);
                AppendTextCell(builder, CellReference(17, rowNumber), row.Operands);
                builder.Append("</row>");
            }

            builder.Append("</sheetData></worksheet>");
            return builder.ToString();
        }

        private static void AppendTextCell(StringBuilder builder, string reference, string value)
        {
            builder.Append("<c r=\"").Append(reference).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">");
            builder.Append(Escape(value ?? string.Empty));
            builder.Append("</t></is></c>");
        }

        private static void AppendNumberCell(StringBuilder builder, string reference, decimal value)
        {
            builder.Append("<c r=\"").Append(reference).Append("\"><v>");
            builder.Append(value.ToString("G29", CultureInfo.InvariantCulture));
            builder.Append("</v></c>");
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
    }
}
