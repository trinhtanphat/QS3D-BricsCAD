using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;

namespace QS3D.Core.Export
{
    public static class RebarProcurementCsvExporter
    {
        private const int MaxRowCount = 10000;

        public static void Export(string path, IEnumerable<RebarProcurementSummary> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            var content = ToCsv(rows);
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
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

        public static string ToCsv(IEnumerable<RebarProcurementSummary> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var sb = new StringBuilder();
            sb.AppendLine("AlgorithmId,GroupId,Grade,DiameterMm,StockLengthM,RequiredCutCount,RequiredLengthM,AllowanceLengthM,DemandBeforeKerfM,StockBarCount,KerfLengthM,OffCutLengthM,WasteLengthM,ProcurementLengthM,UnitWeightKgM,DemandWeightKg,ProcurementWeightKg,WasteWeightKg,WastePercent");
            var rowCount = 0;
            foreach (var row in rows)
            {
                if (rowCount >= MaxRowCount)
                    throw new ArgumentOutOfRangeException(nameof(rows), "Rebar procurement CSV exceeds the supported row bound of " + MaxRowCount + ".");
                rowCount++;
                if (row == null) throw new ArgumentException("Rebar procurement CSV cannot contain a null row.", nameof(rows));
                sb.Append(Q(row.AlgorithmId)).Append(',')
                    .Append(Q(row.GroupId)).Append(',')
                    .Append(Q(row.Grade)).Append(',')
                    .Append(F(row.DiameterMm)).Append(',')
                    .Append(F(row.StockLengthM)).Append(',')
                    .Append(row.RequiredCutCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(F(row.RequiredLengthM)).Append(',')
                    .Append(F(row.AllowanceLengthM)).Append(',')
                    .Append(F(row.DemandBeforeKerfM)).Append(',')
                    .Append(row.StockBarCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(F(row.KerfLengthM)).Append(',')
                    .Append(F(row.OffCutLengthM)).Append(',')
                    .Append(F(row.WasteLengthM)).Append(',')
                    .Append(F(row.ProcurementLengthM)).Append(',')
                    .Append(F(row.UnitWeightKgM)).Append(',')
                    .Append(F(row.DemandWeightKg)).Append(',')
                    .Append(F(row.ProcurementWeightKg)).Append(',')
                    .Append(F(row.WasteWeightKg)).Append(',')
                    .Append(F(row.WastePercent)).AppendLine();
            }
            return sb.ToString();
        }

        private static string F(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Rebar procurement CSV numeric value must be finite.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Q(string value)
        {
            var safe = value ?? string.Empty;
            var probe = safe.TrimStart();
            if (probe.Length > 0 && (probe[0] == '=' || probe[0] == '+' || probe[0] == '-' || probe[0] == '@')) safe = "'" + safe;
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }
    }
}
