using System;
using System.Collections;
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
        private static readonly UTF8Encoding StrictUtf8WithBom = new UTF8Encoding(true, true);

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

        public static string ToCsv(IEnumerable<RebarProcurementSummary> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var admittedCount = ReadKnownCount(rows);
            if (admittedCount.HasValue)
            {
                ValidateKnownCount(admittedCount.Value);
                if (admittedCount.Value > MaxRowCount)
                    throw new ArgumentOutOfRangeException(nameof(rows), "Rebar procurement CSV exceeds the supported row bound of " + MaxRowCount + ".");
            }

            var sb = new StringBuilder();
            sb.Append("AlgorithmId,GroupId,Grade,DiameterMm,StockLengthM,RequiredCutCount,RequiredLengthM,AllowanceLengthM,DemandBeforeKerfM,StockBarCount,KerfLengthM,OffCutLengthM,WasteLengthM,ProcurementLengthM,UnitWeightKgM,DemandWeightKg,ProcurementWeightKg,WasteWeightKg,WastePercent").Append("\r\n");
            var rowCount = 0;
            using (var enumerator = rows.GetEnumerator())
            {
                while (true)
                {
                    ValidateKnownCount(rows, admittedCount);
                    var moved = enumerator.MoveNext();
                    ValidateKnownCount(rows, admittedCount);
                    if (!moved) break;
                    if (rowCount >= MaxRowCount)
                        throw new ArgumentOutOfRangeException(nameof(rows), "Rebar procurement CSV exceeds the supported row bound of " + MaxRowCount + ".");
                    if (admittedCount.HasValue && rowCount >= admittedCount.Value)
                        throw new InvalidOperationException("Rebar procurement CSV row Count grew beyond the admitted Count during serialization.");

                    var row = enumerator.Current;
                    rowCount++;
                    if (row == null) throw new ArgumentException("Rebar procurement CSV cannot contain a null row.", nameof(rows));
                    sb.Append(Q(row.AlgorithmId)).Append(',')
                        .Append(QSemanticIdentity(row.GroupId, "group id")).Append(',')
                        .Append(QSemanticIdentity(row.Grade, "grade")).Append(',')
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
                        .Append(F(row.WastePercent)).Append("\r\n");
                }
            }

            ValidateKnownCount(rows, admittedCount);
            if (admittedCount.HasValue && rowCount != admittedCount.Value)
                throw new InvalidOperationException("Rebar procurement CSV row Count did not match the admitted Count during serialization.");
            var content = sb.ToString();
            StrictUtf8WithBom.GetByteCount(content);
            return content;
        }

        private static int? ReadKnownCount(IEnumerable<RebarProcurementSummary> rows)
        {
            int? count = null;
            if (rows is ICollection<RebarProcurementSummary> genericCollection)
                BindKnownCount(ref count, genericCollection.Count);
            if (rows is IReadOnlyCollection<RebarProcurementSummary> readOnlyCollection)
                BindKnownCount(ref count, readOnlyCollection.Count);
            if (rows is ICollection nonGenericCollection)
                BindKnownCount(ref count, nonGenericCollection.Count);
            return count;
        }

        private static void BindKnownCount(ref int? bound, int candidate)
        {
            ValidateKnownCount(candidate);
            if (bound.HasValue && bound.Value != candidate)
                throw new InvalidOperationException("Rebar procurement CSV exposes conflicting row Count evidence.");
            bound = candidate;
        }

        private static void ValidateKnownCount(int count)
        {
            if (count < 0)
                throw new InvalidOperationException("Rebar procurement CSV row Count cannot be negative.");
        }

        private static void ValidateKnownCount(IEnumerable<RebarProcurementSummary> rows, int? admittedCount)
        {
            if (!admittedCount.HasValue) return;
            var current = ReadKnownCount(rows);
            if (!current.HasValue || current.Value != admittedCount.Value)
                throw new InvalidOperationException("Rebar procurement CSV row Count changed during serialization.");
        }

        private static string F(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Rebar procurement CSV numeric value must be finite.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string QSemanticIdentity(string value, string label)
        {
            var safe = value ?? string.Empty;
            var probe = safe.TrimStart();
            if (probe.Length > 0 && (probe[0] == '=' || probe[0] == '+' || probe[0] == '-' || probe[0] == '@'))
                throw new InvalidDataException("Rebar procurement CSV " + label + " cannot begin with a spreadsheet formula prefix because semantic identity must be preserved exactly.");
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
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
