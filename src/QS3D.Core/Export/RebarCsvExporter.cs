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
    public static class RebarCsvExporter
    {
        private const int MaxRowCount = 10000;
        private const long MaxCsvBytes = 16L * 1024L * 1024L;
        private static readonly UTF8Encoding StrictUtf8WithBom = CreateStrictUtf8WithBom();

        public static void Export(string path, IEnumerable<RebarScheduleRow> rows)
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

        public static string ToCsv(IEnumerable<RebarScheduleRow> rows)
        {
            var snapshots = SnapshotRows(rows);
            ValidateCsvByteCount(snapshots);
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
                WriteCsv(writer, snapshots);
            return sb.ToString();
        }

        private static List<RebarScheduleRow> SnapshotRows(IEnumerable<RebarScheduleRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var admittedCount = ReadKnownCount(rows);
            if (admittedCount.HasValue)
            {
                ValidateKnownCount(admittedCount.Value);
                if (admittedCount.Value > MaxRowCount)
                    throw new ArgumentOutOfRangeException(nameof(rows), "BBS CSV exceeds the supported row bound of " + MaxRowCount + ".");
            }

            var sourceRows = new List<RebarScheduleRow>();
            var snapshots = new List<RebarScheduleRow>();
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
                        throw new ArgumentOutOfRangeException(nameof(rows), "BBS CSV exceeds the supported row bound of " + MaxRowCount + ".");
                    if (admittedCount.HasValue && rowCount >= admittedCount.Value)
                        throw new InvalidOperationException("BBS CSV row Count grew beyond the admitted Count during serialization.");

                    var sourceRow = enumerator.Current;
                    ValidateKnownCount(rows, admittedCount);
                    if (sourceRow == null) throw new ArgumentException("BBS row cannot be null.", nameof(rows));
                    var snapshot = SnapshotRow(sourceRow);
                    ValidateRow(snapshot);
                    sourceRows.Add(sourceRow);
                    snapshots.Add(snapshot);
                    rowCount++;
                }
            }

            ValidateKnownCount(rows, admittedCount);
            if (admittedCount.HasValue && rowCount != admittedCount.Value)
                throw new InvalidOperationException("BBS CSV row Count did not match the admitted Count during serialization.");
            for (var index = 0; index < snapshots.Count; index++)
                EnsureRowStable(sourceRows[index], snapshots[index], index);
            return snapshots;
        }

        private static void ValidateCsvByteCount(IReadOnlyList<RebarScheduleRow> snapshots)
        {
            long byteCount = StrictUtf8WithBom.GetPreamble().Length;
            AddCsvBytes(ref byteCount, "ElementId,BarMark,ShapeCode,Notation,DiameterMm,Quantity,CuttingLengthM,TotalLengthM,UnitWeightKgM,NetWeightKg,WastePercent,TotalWeightKg,FabricationStatus,FabricationStandardCode,FabricationDetailingRevision\r\n");
            foreach (var row in snapshots)
            {
                AddIdentityCsvFieldBytes(ref byteCount, row.ElementId, "element id"); AddCsvBytes(ref byteCount, ",");
                AddEscapedCsvFieldBytes(ref byteCount, row.BarMark); AddCsvBytes(ref byteCount, ",");
                AddEscapedCsvFieldBytes(ref byteCount, row.ShapeCode); AddCsvBytes(ref byteCount, ",");
                AddEscapedCsvFieldBytes(ref byteCount, row.Notation); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.DiameterMm)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, row.Quantity.ToString(CultureInfo.InvariantCulture)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.CuttingLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.TotalLengthM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.UnitWeightKgM)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.NetWeightKg)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.WastePercent)); AddCsvBytes(ref byteCount, ",");
                AddCsvBytes(ref byteCount, F(row.TotalWeightKg)); AddCsvBytes(ref byteCount, ",");
                AddEscapedCsvFieldBytes(ref byteCount, row.FabricationStatus); AddCsvBytes(ref byteCount, ",");
                AddEscapedCsvFieldBytes(ref byteCount, row.FabricationStandardCode); AddCsvBytes(ref byteCount, ",");
                AddEscapedCsvFieldBytes(ref byteCount, row.FabricationDetailingRevision); AddCsvBytes(ref byteCount, "\r\n");
            }
        }

        private static void AddCsvBytes(ref long byteCount, string value)
        {
            AddCsvByteCount(ref byteCount, StrictUtf8WithBom.GetByteCount(value));
        }

        private static void AddIdentityCsvFieldBytes(ref long byteCount, string value, string label)
        {
            var safe = RequireCanonicalIdentity(value, label);
            var probe = safe.TrimStart();
            if (probe.Length > 0 && IsFormulaPrefix(probe[0]))
                throw new InvalidDataException("BBS CSV " + label + " cannot begin with a spreadsheet formula prefix because semantic identity must be preserved exactly.");
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

        private static void WriteCsv(TextWriter writer, IReadOnlyList<RebarScheduleRow> snapshots)
        {
            writer.Write("ElementId,BarMark,ShapeCode,Notation,DiameterMm,Quantity,CuttingLengthM,TotalLengthM,UnitWeightKgM,NetWeightKg,WastePercent,TotalWeightKg,FabricationStatus,FabricationStandardCode,FabricationDetailingRevision\r\n");
            foreach (var row in snapshots)
            {
                writer.Write(QIdentity(row.ElementId, "element id")); writer.Write(',');
                writer.Write(Q(row.BarMark)); writer.Write(',');
                writer.Write(Q(row.ShapeCode)); writer.Write(',');
                writer.Write(Q(row.Notation)); writer.Write(',');
                writer.Write(F(row.DiameterMm)); writer.Write(',');
                writer.Write(row.Quantity.ToString(CultureInfo.InvariantCulture)); writer.Write(',');
                writer.Write(F(row.CuttingLengthM)); writer.Write(',');
                writer.Write(F(row.TotalLengthM)); writer.Write(',');
                writer.Write(F(row.UnitWeightKgM)); writer.Write(',');
                writer.Write(F(row.NetWeightKg)); writer.Write(',');
                writer.Write(F(row.WastePercent)); writer.Write(',');
                writer.Write(F(row.TotalWeightKg)); writer.Write(',');
                writer.Write(Q(row.FabricationStatus)); writer.Write(',');
                writer.Write(Q(row.FabricationStandardCode)); writer.Write(',');
                writer.Write(Q(row.FabricationDetailingRevision)); writer.Write("\r\n");
            }
        }

        private static RebarScheduleRow SnapshotRow(RebarScheduleRow source)
        {
            return new RebarScheduleRow
            {
                ElementId = source.ElementId ?? string.Empty,
                BarMark = source.BarMark ?? string.Empty,
                ShapeCode = source.ShapeCode ?? string.Empty,
                Notation = source.Notation ?? string.Empty,
                DiameterMm = source.DiameterMm,
                Quantity = source.Quantity,
                CuttingLengthM = source.CuttingLengthM,
                TotalLengthM = source.TotalLengthM,
                UnitWeightKgM = source.UnitWeightKgM,
                NetWeightKg = source.NetWeightKg,
                WastePercent = source.WastePercent,
                TotalWeightKg = source.TotalWeightKg,
                FabricationStatus = source.FabricationStatus ?? string.Empty,
                FabricationStandardCode = source.FabricationStandardCode ?? string.Empty,
                FabricationDetailingRevision = source.FabricationDetailingRevision ?? string.Empty
            };
        }

        private static void EnsureRowStable(RebarScheduleRow source, RebarScheduleRow snapshot, int rowIndex)
        {
            if (source == null ||
                !string.Equals(source.ElementId ?? string.Empty, snapshot.ElementId, StringComparison.Ordinal) ||
                !string.Equals(source.BarMark ?? string.Empty, snapshot.BarMark, StringComparison.Ordinal) ||
                !string.Equals(source.ShapeCode ?? string.Empty, snapshot.ShapeCode, StringComparison.Ordinal) ||
                !string.Equals(source.Notation ?? string.Empty, snapshot.Notation, StringComparison.Ordinal) ||
                source.DiameterMm != snapshot.DiameterMm ||
                source.Quantity != snapshot.Quantity ||
                source.CuttingLengthM != snapshot.CuttingLengthM ||
                source.TotalLengthM != snapshot.TotalLengthM ||
                source.UnitWeightKgM != snapshot.UnitWeightKgM ||
                source.NetWeightKg != snapshot.NetWeightKg ||
                source.WastePercent != snapshot.WastePercent ||
                source.TotalWeightKg != snapshot.TotalWeightKg ||
                !string.Equals(source.FabricationStatus ?? string.Empty, snapshot.FabricationStatus, StringComparison.Ordinal) ||
                !string.Equals(source.FabricationStandardCode ?? string.Empty, snapshot.FabricationStandardCode, StringComparison.Ordinal) ||
                !string.Equals(source.FabricationDetailingRevision ?? string.Empty, snapshot.FabricationDetailingRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("BBS CSV row values changed during serialization. Invalid row index: " + rowIndex + ".");
        }

        private static int? ReadKnownCount(IEnumerable<RebarScheduleRow> rows)
        {
            int? count = null;
            if (rows is ICollection<RebarScheduleRow> genericCollection)
                BindKnownCount(ref count, genericCollection.Count);
            if (rows is IReadOnlyCollection<RebarScheduleRow> readOnlyCollection)
                BindKnownCount(ref count, readOnlyCollection.Count);
            if (rows is ICollection nonGenericCollection)
                BindKnownCount(ref count, nonGenericCollection.Count);
            return count;
        }

        private static void BindKnownCount(ref int? bound, int candidate)
        {
            ValidateKnownCount(candidate);
            if (bound.HasValue && bound.Value != candidate)
                throw new InvalidOperationException("BBS CSV exposes conflicting row Count evidence.");
            bound = candidate;
        }

        private static void ValidateKnownCount(int count)
        {
            if (count < 0)
                throw new InvalidOperationException("BBS CSV row Count cannot be negative.");
        }

        private static void ValidateKnownCount(IEnumerable<RebarScheduleRow> rows, int? admittedCount)
        {
            if (!admittedCount.HasValue) return;
            var current = ReadKnownCount(rows);
            if (!current.HasValue || current.Value != admittedCount.Value)
                throw new InvalidOperationException("BBS CSV row Count changed during serialization.");
        }

        private static UTF8Encoding CreateStrictUtf8WithBom()
        {
            var encoding = (UTF8Encoding)new UTF8Encoding(true).Clone();
            encoding.EncoderFallback = EncoderFallback.ExceptionFallback;
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            return encoding;
        }

        private static void ValidateRow(RebarScheduleRow row)
        {
            if (row.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(row.Quantity), "BBS quantity must be greater than zero.");
            Positive(row.DiameterMm, nameof(row.DiameterMm));
            Positive(row.CuttingLengthM, nameof(row.CuttingLengthM));
            Positive(row.TotalLengthM, nameof(row.TotalLengthM));
            Positive(row.UnitWeightKgM, nameof(row.UnitWeightKgM));
            NonNegative(row.NetWeightKg, nameof(row.NetWeightKg));
            NonNegative(row.WastePercent, nameof(row.WastePercent));
            NonNegative(row.TotalWeightKg, nameof(row.TotalWeightKg));
        }

        private static void Positive(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d) throw new ArgumentOutOfRangeException(name, "BBS CSV numeric value must be finite and greater than zero.");
        }

        private static void NonNegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(name, "BBS CSV numeric value must be finite and non-negative.");
        }

        private static string F(double value)
        {
            return value == 0d ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string QIdentity(string value, string label)
        {
            var safe = RequireCanonicalIdentity(value, label);
            var probe = safe.TrimStart();
            if (probe.Length > 0 && (probe[0] == '=' || probe[0] == '+' || probe[0] == '-' || probe[0] == '@'))
                throw new InvalidDataException("BBS CSV " + label + " cannot begin with a spreadsheet formula prefix because semantic identity must be preserved exactly.");
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }

        private static string RequireCanonicalIdentity(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("BBS CSV " + label + " is required.");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("BBS CSV " + label + " must not contain leading or trailing whitespace.");
            foreach (var ch in value)
            {
                if (char.IsControl(ch))
                    throw new InvalidDataException("BBS CSV " + label + " must not contain control characters.");
            }
            return value;
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
