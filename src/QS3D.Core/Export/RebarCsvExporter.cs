using System;
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
        private static readonly UTF8Encoding StrictUtf8WithBom = CreateStrictUtf8WithBom();

        public static void Export(string path, IEnumerable<RebarScheduleRow> rows)
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

        public static string ToCsv(IEnumerable<RebarScheduleRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var sb = new StringBuilder();
            sb.AppendLine("ElementId,BarMark,ShapeCode,Notation,DiameterMm,Quantity,CuttingLengthM,TotalLengthM,UnitWeightKgM,NetWeightKg,WastePercent,TotalWeightKg,FabricationStatus,FabricationStandardCode,FabricationDetailingRevision");
            var rowCount = 0;
            foreach (var row in rows)
            {
                if (rowCount >= MaxRowCount)
                    throw new ArgumentOutOfRangeException(nameof(rows), "BBS CSV exceeds the supported row bound of " + MaxRowCount + ".");
                rowCount++;
                ValidateRow(row ?? throw new ArgumentException("BBS row cannot be null.", nameof(rows)));
                sb.Append(Q(row.ElementId)).Append(',')
                    .Append(Q(row.BarMark)).Append(',')
                    .Append(Q(row.ShapeCode)).Append(',')
                    .Append(Q(row.Notation)).Append(',')
                    .Append(F(row.DiameterMm)).Append(',')
                    .Append(row.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(F(row.CuttingLengthM)).Append(',')
                    .Append(F(row.TotalLengthM)).Append(',')
                    .Append(F(row.UnitWeightKgM)).Append(',')
                    .Append(F(row.NetWeightKg)).Append(',')
                    .Append(F(row.WastePercent)).Append(',')
                    .Append(F(row.TotalWeightKg)).Append(',')
                    .Append(Q(row.FabricationStatus)).Append(',')
                    .Append(Q(row.FabricationStandardCode)).Append(',')
                    .Append(Q(row.FabricationDetailingRevision)).AppendLine();
            }
            var content = sb.ToString();
            StrictUtf8WithBom.GetByteCount(content);
            return content;
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
            var formatted = value.ToString("0.######", CultureInfo.InvariantCulture);
            if (value != 0d && string.Equals(formatted, "0", StringComparison.Ordinal))
                return value.ToString("R", CultureInfo.InvariantCulture);
            return formatted;
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
