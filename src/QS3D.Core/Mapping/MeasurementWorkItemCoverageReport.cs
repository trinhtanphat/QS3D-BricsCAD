using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Mapping
{
    public sealed class MeasurementWorkItemCoverageReportRow
    {
        internal MeasurementWorkItemCoverageReportRow(MeasurementWorkItemCoverageFinding finding)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));

            ElementId = finding.ElementId;
            Category = finding.Category;
            QuantityKey = finding.QuantityKey;
            QuantityValue = finding.QuantityValue;
            IsReady = finding.IsReady;

            var mapping = finding.Mapping;
            MappingId = mapping?.MappingId;
            ClassificationId = mapping?.ClassificationId;
            WorkItemId = mapping?.WorkItemId;

            var issues = new MeasurementWorkItemCoverageIssue[finding.Issues.Count];
            for (var i = 0; i < finding.Issues.Count; i++)
                issues[i] = finding.Issues[i];
            Issues = new ReadOnlyCollection<MeasurementWorkItemCoverageIssue>(issues);
        }

        public string ElementId { get; }
        public Domain.ElementCategory Category { get; }
        public string? QuantityKey { get; }
        public double? QuantityValue { get; }
        public string? MappingId { get; }
        public string? ClassificationId { get; }
        public string? WorkItemId { get; }
        public bool IsReady { get; }
        public IReadOnlyList<MeasurementWorkItemCoverageIssue> Issues { get; }
    }

    public sealed class MeasurementWorkItemCoverageReport
    {
        private const int MaximumFindingCount = 10000;

        private MeasurementWorkItemCoverageReport(
            IReadOnlyList<MeasurementWorkItemCoverageReportRow> rows,
            int readyCount,
            int missingQuantityCount,
            int staleQuantityCount,
            int unmappedWorkItemCount)
        {
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            TotalCount = rows.Count;
            ReadyCount = readyCount;
            NotReadyCount = TotalCount - readyCount;
            MissingQuantityCount = missingQuantityCount;
            StaleQuantityCount = staleQuantityCount;
            UnmappedWorkItemCount = unmappedWorkItemCount;
        }

        public IReadOnlyList<MeasurementWorkItemCoverageReportRow> Rows { get; }
        public int TotalCount { get; }
        public int ReadyCount { get; }
        public int NotReadyCount { get; }
        public int MissingQuantityCount { get; }
        public int StaleQuantityCount { get; }
        public int UnmappedWorkItemCount { get; }

        public static MeasurementWorkItemCoverageReport Create(
            IEnumerable<MeasurementWorkItemCoverageFinding> findings)
        {
            if (findings == null) throw new ArgumentNullException(nameof(findings));

            RejectKnownOversize(findings);

            var rows = new List<MeasurementWorkItemCoverageReportRow>();
            var index = 0;
            foreach (var finding in findings)
            {
                if (index >= MaximumFindingCount)
                    throw new InvalidOperationException(
                        "Coverage report input must contain at most " + MaximumFindingCount + " findings.");
                if (finding == null)
                    throw new ArgumentException("Coverage report input contains a null finding at index " + index + ".", nameof(findings));
                rows.Add(new MeasurementWorkItemCoverageReportRow(finding));
                index++;
            }

            rows.Sort(CompareRows);

            var readyCount = 0;
            var missingQuantityCount = 0;
            var staleQuantityCount = 0;
            var unmappedWorkItemCount = 0;

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (row.IsReady)
                    readyCount++;

                for (var issueIndex = 0; issueIndex < row.Issues.Count; issueIndex++)
                {
                    switch (row.Issues[issueIndex])
                    {
                        case MeasurementWorkItemCoverageIssue.MissingQuantity:
                            missingQuantityCount++;
                            break;
                        case MeasurementWorkItemCoverageIssue.StaleQuantity:
                            staleQuantityCount++;
                            break;
                        case MeasurementWorkItemCoverageIssue.UnmappedWorkItem:
                            unmappedWorkItemCount++;
                            break;
                    }
                }
            }

            return new MeasurementWorkItemCoverageReport(
                new ReadOnlyCollection<MeasurementWorkItemCoverageReportRow>(rows.ToArray()),
                readyCount,
                missingQuantityCount,
                staleQuantityCount,
                unmappedWorkItemCount);
        }

        private static void RejectKnownOversize(IEnumerable<MeasurementWorkItemCoverageFinding> findings)
        {
            if (findings is ICollection<MeasurementWorkItemCoverageFinding> collection &&
                collection.Count > MaximumFindingCount)
            {
                throw new InvalidOperationException(
                    "Coverage report input must contain at most " + MaximumFindingCount + " findings.");
            }

            if (findings is IReadOnlyCollection<MeasurementWorkItemCoverageFinding> readOnlyCollection &&
                readOnlyCollection.Count > MaximumFindingCount)
            {
                throw new InvalidOperationException(
                    "Coverage report input must contain at most " + MaximumFindingCount + " findings.");
            }
        }

        private static int CompareRows(
            MeasurementWorkItemCoverageReportRow left,
            MeasurementWorkItemCoverageReportRow right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.ElementId, right.ElementId);
            if (compare != 0) return compare;
            compare = ((int)left.Category).CompareTo((int)right.Category);
            if (compare != 0) return compare;
            compare = CompareNullableToken(left.QuantityKey, right.QuantityKey);
            if (compare != 0) return compare;
            compare = CompareNullableToken(left.MappingId, right.MappingId);
            if (compare != 0) return compare;
            return CompareIssues(left.Issues, right.Issues);
        }

        private static int CompareNullableToken(string? left, string? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;

            var compare = StringComparer.OrdinalIgnoreCase.Compare(left, right);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left, right);
        }

        private static int CompareIssues(
            IReadOnlyList<MeasurementWorkItemCoverageIssue> left,
            IReadOnlyList<MeasurementWorkItemCoverageIssue> right)
        {
            var count = Math.Min(left.Count, right.Count);
            for (var i = 0; i < count; i++)
            {
                var compare = ((int)left[i]).CompareTo((int)right[i]);
                if (compare != 0) return compare;
            }
            return left.Count.CompareTo(right.Count);
        }
    }
}
