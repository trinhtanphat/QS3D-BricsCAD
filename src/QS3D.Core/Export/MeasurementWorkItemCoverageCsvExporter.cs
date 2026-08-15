using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QS3D.Core.Mapping;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public static class MeasurementWorkItemCoverageCsvExporter
    {
        private const string Header = "Category,MeasurementItemId,MappingId,ClassificationId,WorkItemId,IsReady,Issues,FindingCount,AffectedElementCount,AffectedElementIds";
        private static readonly UTF8Encoding StrictUtf8WithBom = new UTF8Encoding(true, true);

        public static void Export(string path, MeasurementWorkItemCoverageMatrix matrix)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            var content = ToCsv(matrix);
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, StrictUtf8WithBom))
                {
                    writer.Write(content);
                    writer.Flush();
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

            var sb = new StringBuilder();
            sb.Append(Header).Append("\r\n");
            for (var i = 0; i < matrix.Cells.Count; i++)
            {
                var cell = matrix.Cells[i];
                if (cell == null)
                    throw new ArgumentException("Coverage matrix contains a null cell at index " + i + ".", nameof(matrix));

                sb.Append(Q(cell.Category.ToString())).Append(',')
                    .Append(Q(cell.MeasurementItemId)).Append(',')
                    .Append(Q(cell.MappingId)).Append(',')
                    .Append(Q(cell.ClassificationId)).Append(',')
                    .Append(Q(cell.WorkItemId)).Append(',')
                    .Append(cell.IsReady ? "true" : "false").Append(',')
                    .Append(Q(string.Join("|", cell.Issues.Select(x => x.ToString())))).Append(',')
                    .Append(cell.FindingCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(cell.AffectedElementCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(Q(string.Join("|", cell.AffectedElementIds)))
                    .Append("\r\n");
            }

            var content = sb.ToString();
            StrictUtf8WithBom.GetByteCount(content);
            return content;
        }

        private static string Q(string? value)
        {
            var safe = value ?? string.Empty;
            var probe = safe.TrimStart();
            if (probe.Length > 0 && (probe[0] == '=' || probe[0] == '+' || probe[0] == '-' || probe[0] == '@'))
                safe = "'" + safe;
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }
    }
}
