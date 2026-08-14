using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Mapping
{
    public sealed class MeasurementWorkItemCoverageMatrixCell
    {
        internal MeasurementWorkItemCoverageMatrixCell(
            Domain.ElementCategory category,
            string? measurementItemId,
            string? mappingId,
            string? classificationId,
            string? workItemId,
            bool isReady,
            IReadOnlyList<MeasurementWorkItemCoverageIssue> issues,
            int findingCount,
            IReadOnlyList<string> affectedElementIds)
        {
            Category = category;
            MeasurementItemId = measurementItemId;
            MappingId = mappingId;
            ClassificationId = classificationId;
            WorkItemId = workItemId;
            IsReady = isReady;
            Issues = issues ?? throw new ArgumentNullException(nameof(issues));
            FindingCount = findingCount;
            AffectedElementIds = affectedElementIds ?? throw new ArgumentNullException(nameof(affectedElementIds));
        }

        public Domain.ElementCategory Category { get; }
        public string? MeasurementItemId { get; }
        public string? MappingId { get; }
        public string? ClassificationId { get; }
        public string? WorkItemId { get; }
        public bool IsReady { get; }
        public IReadOnlyList<MeasurementWorkItemCoverageIssue> Issues { get; }
        public int FindingCount { get; }
        public IReadOnlyList<string> AffectedElementIds { get; }
        public int AffectedElementCount => AffectedElementIds.Count;
    }

    public sealed class MeasurementWorkItemCoverageMatrix
    {
        private MeasurementWorkItemCoverageMatrix(
            IReadOnlyList<MeasurementWorkItemCoverageMatrixCell> cells,
            MeasurementWorkItemCoverageReport report)
        {
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            CellCount = cells.Count;
            TotalFindingCount = report.TotalCount;
            ReadyFindingCount = report.ReadyCount;
            NotReadyFindingCount = report.NotReadyCount;
            MissingQuantityFindingCount = report.MissingQuantityCount;
            StaleQuantityFindingCount = report.StaleQuantityCount;
            UnmappedWorkItemFindingCount = report.UnmappedWorkItemCount;
        }

        public IReadOnlyList<MeasurementWorkItemCoverageMatrixCell> Cells { get; }
        public int CellCount { get; }
        public int TotalFindingCount { get; }
        public int ReadyFindingCount { get; }
        public int NotReadyFindingCount { get; }
        public int MissingQuantityFindingCount { get; }
        public int StaleQuantityFindingCount { get; }
        public int UnmappedWorkItemFindingCount { get; }

        public static MeasurementWorkItemCoverageMatrix Create(MeasurementWorkItemCoverageReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            var rows = new List<MeasurementWorkItemCoverageReportRow>(report.Rows.Count);
            for (var i = 0; i < report.Rows.Count; i++)
            {
                var row = report.Rows[i];
                if (row == null)
                    throw new ArgumentException("Coverage report contains a null row at index " + i + ".", nameof(report));
                rows.Add(row);
            }
            rows.Sort(CompareRowsForMatrix);

            var cells = new List<MeasurementWorkItemCoverageMatrixCell>();
            var index = 0;
            while (index < rows.Count)
            {
                var first = rows[index];
                var end = index + 1;
                while (end < rows.Count && SameCell(first, rows[end])) end++;

                var issues = new MeasurementWorkItemCoverageIssue[first.Issues.Count];
                for (var issueIndex = 0; issueIndex < first.Issues.Count; issueIndex++)
                    issues[issueIndex] = first.Issues[issueIndex];

                var elementIds = new List<string>();
                var seenElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var rowIndex = index; rowIndex < end; rowIndex++)
                {
                    var elementId = rows[rowIndex].ElementId;
                    if (seenElementIds.Add(elementId)) elementIds.Add(elementId);
                }
                elementIds.Sort(CompareTokens);

                cells.Add(new MeasurementWorkItemCoverageMatrixCell(
                    first.Category,
                    first.QuantityKey,
                    first.MappingId,
                    first.ClassificationId,
                    first.WorkItemId,
                    first.IsReady,
                    new ReadOnlyCollection<MeasurementWorkItemCoverageIssue>(issues),
                    end - index,
                    new ReadOnlyCollection<string>(elementIds.ToArray())));

                index = end;
            }

            return new MeasurementWorkItemCoverageMatrix(
                new ReadOnlyCollection<MeasurementWorkItemCoverageMatrixCell>(cells.ToArray()),
                report);
        }

        private static bool SameCell(
            MeasurementWorkItemCoverageReportRow left,
            MeasurementWorkItemCoverageReportRow right) =>
            left.Category == right.Category &&
            SameToken(left.QuantityKey, right.QuantityKey) &&
            SameToken(left.MappingId, right.MappingId) &&
            SameToken(left.ClassificationId, right.ClassificationId) &&
            SameToken(left.WorkItemId, right.WorkItemId) &&
            left.IsReady == right.IsReady &&
            CompareIssues(left.Issues, right.Issues) == 0;

        private static int CompareRowsForMatrix(
            MeasurementWorkItemCoverageReportRow left,
            MeasurementWorkItemCoverageReportRow right)
        {
            var compare = ((int)left.Category).CompareTo((int)right.Category);
            if (compare != 0) return compare;
            compare = CompareNullableToken(left.QuantityKey, right.QuantityKey);
            if (compare != 0) return compare;
            compare = CompareNullableToken(left.MappingId, right.MappingId);
            if (compare != 0) return compare;
            compare = CompareNullableToken(left.ClassificationId, right.ClassificationId);
            if (compare != 0) return compare;
            compare = CompareNullableToken(left.WorkItemId, right.WorkItemId);
            if (compare != 0) return compare;
            compare = left.IsReady.CompareTo(right.IsReady);
            if (compare != 0) return compare;
            compare = CompareIssues(left.Issues, right.Issues);
            if (compare != 0) return compare;
            return CompareTokens(left.ElementId, right.ElementId);
        }

        private static int CompareNullableToken(string? left, string? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return CompareTokens(left, right);
        }

        private static int CompareTokens(string left, string right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left, right);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left, right);
        }

        private static bool SameToken(string? left, string? right) =>
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

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
