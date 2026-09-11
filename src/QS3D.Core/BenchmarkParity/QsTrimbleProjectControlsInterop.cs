using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class TrimbleProjectControlExportRow
    {
        public TrimbleProjectControlExportRow(
            string schemaVersion,
            string sourceRevision,
            string packageId,
            decimal commitment,
            decimal ordered,
            decimal delivered,
            decimal actualCost,
            decimal orderedToCommitment,
            decimal deliveredToOrdered,
            decimal actualToCommitment,
            decimal commitmentVariance,
            decimal costToComplete,
            double plannedProgress,
            double actualProgress,
            double scheduleVariance)
        {
            SchemaVersion = QsModelElementSnapshot.Require(schemaVersion, "schemaVersion");
            SourceRevision = QsModelElementSnapshot.Require(sourceRevision, "sourceRevision");
            PackageId = QsModelElementSnapshot.Require(packageId, "packageId");
            Commitment = commitment;
            Ordered = ordered;
            Delivered = delivered;
            ActualCost = actualCost;
            OrderedToCommitment = orderedToCommitment;
            DeliveredToOrdered = deliveredToOrdered;
            ActualToCommitment = actualToCommitment;
            CommitmentVariance = commitmentVariance;
            CostToComplete = costToComplete;
            PlannedProgress = plannedProgress;
            ActualProgress = actualProgress;
            ScheduleVariance = scheduleVariance;
        }

        public string SchemaVersion { get; private set; }
        public string SourceRevision { get; private set; }
        public string PackageId { get; private set; }
        public decimal Commitment { get; private set; }
        public decimal Ordered { get; private set; }
        public decimal Delivered { get; private set; }
        public decimal ActualCost { get; private set; }
        public decimal OrderedToCommitment { get; private set; }
        public decimal DeliveredToOrdered { get; private set; }
        public decimal ActualToCommitment { get; private set; }
        public decimal CommitmentVariance { get; private set; }
        public decimal CostToComplete { get; private set; }
        public double PlannedProgress { get; private set; }
        public double ActualProgress { get; private set; }
        public double ScheduleVariance { get; private set; }
    }

    public sealed class TrimbleProjectControlsInterop
    {
        public const string CurrentSchemaVersion = "qs3d.trimble.project-controls.v1";

        public IReadOnlyList<TrimbleProjectControlExportRow> BuildRows(IEnumerable<ProjectControlLine> controls, string sourceRevision)
        {
            if (controls == null) throw new ArgumentNullException("controls");
            sourceRevision = QsModelElementSnapshot.Require(sourceRevision, "sourceRevision");

            var result = new List<TrimbleProjectControlExportRow>();
            foreach (var control in controls.OrderBy(x => x.PackageId, StringComparer.OrdinalIgnoreCase))
            {
                if (control == null) throw new InvalidOperationException("Project control row cannot be null.");
                Validate(control);

                var commitmentVariance = control.Commitment - control.ActualCost;
                result.Add(new TrimbleProjectControlExportRow(
                    CurrentSchemaVersion,
                    sourceRevision,
                    control.PackageId,
                    control.Commitment,
                    control.Ordered,
                    control.Delivered,
                    control.ActualCost,
                    Ratio(control.Ordered, control.Commitment, "ordered/commitment", control.PackageId),
                    Ratio(control.Delivered, control.Ordered, "delivered/ordered", control.PackageId),
                    Ratio(control.ActualCost, control.Commitment, "actual/commitment", control.PackageId),
                    commitmentVariance,
                    commitmentVariance < 0m ? 0m : commitmentVariance,
                    control.PlannedProgress,
                    control.ActualProgress,
                    control.ScheduleVariance));
            }

            return new ReadOnlyCollection<TrimbleProjectControlExportRow>(result);
        }

        public string ToCsv(IEnumerable<TrimbleProjectControlExportRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            var builder = new StringBuilder();
            builder.AppendLine("schemaVersion,sourceRevision,packageId,commitment,ordered,delivered,actualCost,orderedToCommitment,deliveredToOrdered,actualToCommitment,commitmentVariance,costToComplete,plannedProgress,actualProgress,scheduleVariance");
            foreach (var row in rows.OrderBy(x => x.PackageId, StringComparer.OrdinalIgnoreCase))
            {
                if (row == null) throw new InvalidOperationException("Export row cannot be null.");
                builder.Append(Escape(row.SchemaVersion)).Append(',')
                    .Append(Escape(row.SourceRevision)).Append(',')
                    .Append(Escape(row.PackageId)).Append(',')
                    .Append(DecimalText(row.Commitment)).Append(',')
                    .Append(DecimalText(row.Ordered)).Append(',')
                    .Append(DecimalText(row.Delivered)).Append(',')
                    .Append(DecimalText(row.ActualCost)).Append(',')
                    .Append(DecimalText(row.OrderedToCommitment)).Append(',')
                    .Append(DecimalText(row.DeliveredToOrdered)).Append(',')
                    .Append(DecimalText(row.ActualToCommitment)).Append(',')
                    .Append(DecimalText(row.CommitmentVariance)).Append(',')
                    .Append(DecimalText(row.CostToComplete)).Append(',')
                    .Append(DoubleText(row.PlannedProgress)).Append(',')
                    .Append(DoubleText(row.ActualProgress)).Append(',')
                    .Append(DoubleText(row.ScheduleVariance)).AppendLine();
            }
            return builder.ToString();
        }

        private static void Validate(ProjectControlLine control)
        {
            if (string.IsNullOrWhiteSpace(control.PackageId)) throw new InvalidOperationException("Project control package id is required.");
            if (control.Commitment < 0m || control.Ordered < 0m || control.Delivered < 0m || control.ActualCost < 0m)
                throw new InvalidOperationException("Project control financial values cannot be negative for package " + control.PackageId + ".");
            if (control.Delivered > control.Ordered)
                throw new InvalidOperationException("Delivered value cannot exceed ordered value for package " + control.PackageId + ".");
            if (control.PlannedProgress < 0d || control.PlannedProgress > 1d || control.ActualProgress < 0d || control.ActualProgress > 1d)
                throw new InvalidOperationException("Project control progress must be between zero and one for package " + control.PackageId + ".");
        }

        private static decimal Ratio(decimal numerator, decimal denominator, string label, string packageId)
        {
            if (denominator == 0m)
            {
                if (numerator == 0m) return 0m;
                throw new InvalidOperationException("Cannot calculate " + label + " with a zero denominator for package " + packageId + ".");
            }
            return numerator / denominator;
        }

        private static string DecimalText(decimal value)
        {
            return value.ToString("0.############################", CultureInfo.InvariantCulture);
        }

        private static string DoubleText(double value)
        {
            return value.ToString("0.################", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
