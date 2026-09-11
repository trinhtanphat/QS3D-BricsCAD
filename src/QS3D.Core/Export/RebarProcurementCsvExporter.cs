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
        private const long MaxCsvBytes = 16L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8WithBom = new UTF8Encoding(true, true);

        public static void Export(string path, IEnumerable<RebarProcurementSummary> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            var snapshots = SnapshotRows(rows);
            ValidateCsvByteCount(snapshots);
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, StrictUtf8WithBom))
                {
                    WriteCsv(writer, snapshots);
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
            var snapshots = SnapshotRows(rows);
            ValidateCsvByteCount(snapshots);
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
                WriteCsv(writer, snapshots);
            return sb.ToString();
        }

        private static List<RebarProcurementSummary> SnapshotRows(IEnumerable<RebarProcurementSummary> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var admittedCount = ReadKnownCount(rows);
            if (admittedCount.HasValue)
            {
                ValidateKnownCount(admittedCount.Value);
                if (admittedCount.Value > MaxRowCount)
                    throw new ArgumentOutOfRangeException(nameof(rows), "Rebar procurement CSV exceeds the supported row bound of " + MaxRowCount + ".");
            }

            var snapshots = new List<RebarProcurementSummary>();
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
                    ValidateKnownCount(rows, admittedCount);
                    if (row == null) throw new ArgumentException("Rebar procurement CSV cannot contain a null row.", nameof(rows));
                    snapshots.Add(row);
                    rowCount++;
                }
            }
            ValidateKnownCount(rows, admittedCount);
            if (admittedCount.HasValue && rowCount != admittedCount.Value)
                throw new InvalidOperationException("Rebar procurement CSV row Count did not match the admitted Count during serialization.");
            return snapshots;
        }

        private static void ValidateCsvByteCount(IReadOnlyList<RebarProcurementSummary> snapshots)
        {
            long byteCount = StrictUtf8WithBom.GetPreamble().Length;
            AddCsvBytes(ref byteCount, "AlgorithmId,GroupId,Grade,DiameterMm,StockLengthM,RequiredCutCount,RequiredLengthM,AllowanceLengthM,DemandBeforeKerfM,StockBarCount,KerfLengthM,OffCutLengthM,WasteLengthM,ProcurementLengthM,UnitWeightKgM,DemandWeightKg,ProcurementWeightKg,WasteWeightKg,WastePercent\r\n");
            foreach (var row in snapshots)
            {
                AddEscapedCsvFieldBytes(ref byteCount, row.AlgorithmId); AddCsvBytes(ref byteCount, ",");
                AddSemanticIdentityCsvFieldBytes(ref byteCount, row.GroupId, "group id"); AddCsvBytes(ref byteCount, ",");
                AddSemanticIdentityCsvFieldBytes(ref byteCount, row.Grade, "grade"); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.DiameterMm)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.StockLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, row.RequiredCutCount.ToString(CultureInfo.InvariantCulture)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.RequiredLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.AllowanceLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.DemandBeforeKerfM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, row.StockBarCount.ToString(CultureInfo.InvariantCulture)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.KerfLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.OffCutLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.WasteLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.ProcurementLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.UnitWeightKgM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.DemandWeightKg)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.ProcurementWeightKg)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.WasteWeightKg)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.WastePercent)); AddCsvBytes(ref byteCount, "\r\n");
            }
        }

        private static void AddCsvBytes(ref long byteCount, string value)
        {
            AddCsvByteCount(ref byteCount, StrictUtf8WithBom.GetByteCount(value));
        }

        private static void AddSemanticIdentityCsvFieldBytes(ref long byteCount, string value, string label)
        {
            var safe = value ?? string.Empty;
            var probe = safe.TrimStart();
            if (probe.Length > 0 && IsFormulaPrefix(probe[0]))
                throw new InvalidDataException("Rebar procurement CSV " + label + " cannot begin with a spreadsheet formula prefix because semantic identity must be preserved exactly.");
            AddQuotedCsvBytes(ref byteCount, safe, false);
        }

        private static void AddEscapedCsvFieldBytes(ref long byteCount, string value)
        {
            var safe = value ?? string.Empty;
            var probe = safe.TrimStart();
            AddQuotedCsvBytes(ref byteCount, safe, probe.Length > 0 && IsFormulaPrefix(probe[0]));
        }

        private static void AddQuotedCsvBytes(ref long byteCount, string value, bool formulaEscape)
        {
            long extraBytes = 2L + StrictUtf8WithBom.GetByteCount(value) + (formulaEscape ? 1L : 0L);
            foreach (var ch in value)
            {
                if (ch == '"') extraBytes = checked(extraBytes + 1L);
            }
            AddCsvByteCount(ref byteCount, extraBytes);
        }

        private static bool IsFormulaPrefix(char value)
        {
            return value == '=' || value == '+' || value == '-' || value == '@';
        }

        private static void AddCsvByteCount(ref long byteCount, long amount)
        {
            byteCount = checked(byteCount + amount);
            if (byteCount > MaxCsvBytes)
                throw new InvalidDataException("CSV output exceeds the bounded UTF-8 size contract of " + MaxCsvBytes.ToString(CultureInfo.InvariantCulture) + " bytes.");
        }

        private static void WriteCsv(TextWriter writer, IReadOnlyList<RebarProcurementSummary> snapshots)
        {
            writer.Write("AlgorithmId,GroupId,Grade,DiameterMm,StockLengthM,RequiredCutCount,RequiredLengthM,AllowanceLengthM,DemandBeforeKerfM,StockBarCount,KerfLengthM,OffCutLengthM,WasteLengthM,ProcurementLengthM,UnitWeightKgM,DemandWeightKg,ProcurementWeightKg,WasteWeightKg,WastePercent\r\n");
            foreach (var row in snapshots)
            {
                writer.Write(Q(row.AlgorithmId)); writer.Write(',');
                writer.Write(QSemanticIdentity(row.GroupId, "group id")); writer.Write(',');
                writer.Write(QSemanticIdentity(row.Grade, "grade")); writer.Write(',');
                writer.Write(F(row.DiameterMm)); writer.Write(',');
                writer.Write(F(row.StockLengthM)); writer.Write(',');
                writer.Write(row.RequiredCutCount.ToString(CultureInfo.InvariantCulture)); writer.Write(',');
                writer.Write(F(row.RequiredLengthM)); writer.Write(',');
                writer.Write(F(row.AllowanceLengthM)); writer.Write(',');
                writer.Write(F(row.DemandBeforeKerfM)); writer.Write(',');
                writer.Write(row.StockBarCount.ToString(CultureInfo.InvariantCulture)); writer.Write(',');
                writer.Write(F(row.KerfLengthM)); writer.Write(',');
                writer.Write(F(row.OffCutLengthM)); writer.Write(',');
                writer.Write(F(row.WasteLengthM)); writer.Write(',');
                writer.Write(F(row.ProcurementLengthM)); writer.Write(',');
                writer.Write(F(row.UnitWeightKgM)); writer.Write(',');
                writer.Write(F(row.DemandWeightKg)); writer.Write(',');
                writer.Write(F(row.ProcurementWeightKg)); writer.Write(',');
                writer.Write(F(row.WasteWeightKg)); writer.Write(',');
                writer.Write(F(row.WastePercent)); writer.Write("\r\n");
            }
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
