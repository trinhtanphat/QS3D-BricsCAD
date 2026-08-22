using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QS3D.Core.Rebar;

namespace QS3D.Core.Export
{
    public static class RebarCsvExporter
    {
        public static void Export(string path, IEnumerable<RebarScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            var full = Path.GetFullPath(path); var dir = Path.GetDirectoryName(full); if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(full, ToCsv(rows), new UTF8Encoding(true));
        }

        public static string ToCsv(IEnumerable<RebarScheduleRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var builder = new StringBuilder();
            builder.AppendLine("Mark,Floor,Zone,Host,Grade,Shape,DiameterMm,Quantity,CutLengthM,TotalLengthM,UnitWeightKgM,TotalWeightKg");
            foreach (var row in rows)
            {
                builder.Append(Q(row.Mark)).Append(',').Append(Q(row.FloorId)).Append(',').Append(Q(row.ZoneId)).Append(',').Append(Q(row.HostElementId)).Append(',')
                    .Append(Q(row.Grade)).Append(',').Append(Q(row.Shape.ToString())).Append(',').Append(F(row.DiameterMm)).Append(',').Append(row.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(F(row.CutLengthM)).Append(',').Append(F(row.TotalLengthM)).Append(',').Append(F(row.UnitWeightKgPerM)).Append(',').Append(F(row.TotalWeightKg)).AppendLine();
            }
            return builder.ToString();
        }

        private static string F(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        private static string Q(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
    }
}
