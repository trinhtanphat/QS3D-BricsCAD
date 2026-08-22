using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Cost;

namespace QS3D.Core.Mep
{
    public sealed class MepTbqReportRow
    {
        internal MepTbqReportRow(MepQuantityGroup group)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));
            Kind = group.Kind;
            System = group.System;
            Specification = group.Specification;
            Region = group.Region;
            ElementCount = group.ElementCount;
            QuantityCount = group.QuantityCount;
            LengthM = ToDecimal(group.LengthM, "MEP report length");
            AreaM2 = ToDecimal(group.AreaM2, "MEP report area");
            VolumeM3 = ToDecimal(group.VolumeM3, "MEP report volume");
        }

        public MepElementKind Kind { get; }
        public string System { get; }
        public string Specification { get; }
        public string Region { get; }
        public int ElementCount { get; }
        public int QuantityCount { get; }
        public decimal LengthM { get; }
        public decimal AreaM2 { get; }
        public decimal VolumeM3 { get; }

        private static decimal ToDecimal(double value, string label)
        {
            if (value == 0d) return 0m;

            decimal converted;
            try { converted = checked((decimal)value); }
            catch (OverflowException ex) { throw new OverflowException(label + " cannot be represented by TBQ decimal arithmetic.", ex); }

            if (converted == 0m)
                throw new OverflowException(label + " cannot be represented by TBQ decimal arithmetic.");
            return converted;
        }
    }

    public sealed class MepTbqProjectionResult
    {
        internal MepTbqProjectionResult(
            TbqProjectWorkspaceState state,
            IReadOnlyList<MepTbqReportRow> reportRows,
            int projectedBillItemCount)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            ReportRows = reportRows ?? throw new ArgumentNullException(nameof(reportRows));
            ProjectedBillItemCount = projectedBillItemCount;
        }

        public TbqProjectWorkspaceState State { get; }
        public IReadOnlyList<MepTbqReportRow> ReportRows { get; }
        public int ProjectedBillItemCount { get; }
    }

    public sealed class MepTbqProjectionService
    {
        public const string OwnedItemPrefix = "QS3D.MEP.";

        public MepTbqProjectionResult Project(
            TbqProjectWorkspaceState current,
            IEnumerable<MepQuantityGroup> groups)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            var report = BuildReport(groups);
            var billItems = new List<TbqBillItem>();
            for (var i = 0; i < current.BillItems.Count; i++)
            {
                var item = current.BillItems[i];
                if (!IsOwnedItem(item.ItemCode)) billItems.Add(item);
            }

            var projectedCount = 0;
            for (var i = 0; i < report.Count; i++)
            {
                var row = report[i];
                projectedCount += AddBillRows(billItems, row);
            }

            var state = new TbqProjectWorkspaceState(
                current.Currency,
                current.CfaM2,
                billItems,
                current.BuildUpRates,
                current.RateReferences.Edges,
                current.LibraryId,
                current.Library.Entries,
                current.AdjustmentRatioPercent,
                current.MarkupRatioPercent);
            return new MepTbqProjectionResult(state, report, projectedCount);
        }

        public IReadOnlyList<MepTbqReportRow> BuildReport(IEnumerable<MepQuantityGroup> groups)
        {
            if (groups == null) throw new ArgumentNullException(nameof(groups));
            var rows = new List<MepTbqReportRow>();
            var index = 0;
            foreach (var group in groups)
            {
                if (group == null)
                    throw new ArgumentException("MEP/TBQ report contains a null quantity group at index " + index + ".", nameof(groups));
                rows.Add(new MepTbqReportRow(group));
                index++;
            }
            rows.Sort(CompareRows);
            return new ReadOnlyCollection<MepTbqReportRow>(rows.ToArray());
        }

        public string SerializeCsv(IEnumerable<MepQuantityGroup> groups)
        {
            return SerializeCsv(BuildReport(groups));
        }

        public string SerializeCsv(IReadOnlyList<MepTbqReportRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var builder = new StringBuilder();
            builder.Append("Region,System,Specification,Kind,ElementCount,QuantityCount,LengthM,AreaM2,VolumeM3\n");
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i] ?? throw new ArgumentException("MEP/TBQ report contains a null row at index " + i + ".", nameof(rows));
                AppendCsv(builder, row.Region);
                builder.Append(',');
                AppendCsv(builder, row.System);
                builder.Append(',');
                AppendCsv(builder, row.Specification);
                builder.Append(',').Append(row.Kind.ToString());
                builder.Append(',').Append(row.ElementCount.ToString(CultureInfo.InvariantCulture));
                builder.Append(',').Append(row.QuantityCount.ToString(CultureInfo.InvariantCulture));
                builder.Append(',').Append(Format(row.LengthM));
                builder.Append(',').Append(Format(row.AreaM2));
                builder.Append(',').Append(Format(row.VolumeM3));
                builder.Append('\n');
            }
            return builder.ToString();
        }

        public static bool IsOwnedItem(string itemCode) =>
            itemCode != null && itemCode.StartsWith(OwnedItemPrefix, StringComparison.OrdinalIgnoreCase);

        private static int AddBillRows(List<TbqBillItem> target, MepTbqReportRow row)
        {
            var count = 0;
            if (row.QuantityCount > 0)
            {
                target.Add(CreateBillItem(row, "COUNT", "ea", row.QuantityCount));
                count++;
            }
            if (row.LengthM > 0m)
            {
                target.Add(CreateBillItem(row, "LENGTH", "m", row.LengthM));
                count++;
            }
            if (row.AreaM2 > 0m)
            {
                target.Add(CreateBillItem(row, "AREA", "m2", row.AreaM2));
                count++;
            }
            if (row.VolumeM3 > 0m)
            {
                target.Add(CreateBillItem(row, "VOLUME", "m3", row.VolumeM3));
                count++;
            }
            return count;
        }

        private static TbqBillItem CreateBillItem(MepTbqReportRow row, string metric, string unit, decimal quantity)
        {
            var identity = row.Region + "\u001f" + row.System + "\u001f" + row.Specification + "\u001f" +
                ((int)row.Kind).ToString(CultureInfo.InvariantCulture);
            var itemCode = OwnedItemPrefix + StableHash(identity) + "." + metric;
            var description = "MEP " + row.Kind + " | " + row.System + " | " + row.Specification + " | " + row.Region + " | " + metric;
            return new TbqBillItem(itemCode, description, unit, "MEP", quantity, 0m);
        }

        private static string StableHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(bytes.Length * 2);
                for (var i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static int CompareRows(MepTbqReportRow left, MepTbqReportRow right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.Region, right.Region);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Region, right.Region);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.System, right.System);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.System, right.System);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.Specification, right.Specification);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Specification, right.Specification);
            if (compare != 0) return compare;
            return left.Kind.CompareTo(right.Kind);
        }

        private static void AppendCsv(StringBuilder builder, string value)
        {
            var quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!quote)
            {
                builder.Append(value);
                return;
            }
            builder.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '"') builder.Append("\"\"");
                else builder.Append(value[i]);
            }
            builder.Append('"');
        }

        private static string Format(decimal value) =>
            value.ToString("0.############################", CultureInfo.InvariantCulture);
    }
}
