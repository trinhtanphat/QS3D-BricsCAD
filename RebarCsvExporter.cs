using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using QS3D.Core.Rebar;

namespace QS3D.Core.Export
{
    public static class RebarCsvExporter
    {
        public static void Export(string path, IEnumerable<RebarScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path)); var full = Path.GetFullPath(path); var directory = Path.GetDirectoryName(full); if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory); File.WriteAllText(full, ToCsv(rows), new UTF8Encoding(true));
        }
        public static string ToCsv(IEnumerable<RebarScheduleRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows)); var sb = new StringBuilder(); sb.AppendLine("ElementId,BarMark,ShapeCode,Notation,DiameterMm,Quantity,CuttingLengthM,TotalLengthM,UnitWeightKgM,NetWeightKg,WastePercent,TotalWeightKg");
            foreach (var row in rows) sb.Append(Q(row.ElementId)).Append(',').Append(Q(row.BarMark)).Append(',').Append(Q(row.ShapeCode)).Append(',').Append(Q(row.Notation)).Append(',').Append(F(row.DiameterMm)).Append(',').Append(row.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',').Append(F(row.CuttingLengthM)).Append(',').Append(F(row.TotalLengthM)).Append(',').Append(F(row.UnitWeightKgM)).Append(',').Append(F(row.NetWeightKg)).Append(',').Append(F(row.WastePercent)).Append(',').Append(F(row.TotalWeightKg)).AppendLine();
            return sb.ToString();
        }
        private static string F(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        private static string Q(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
    }
}
