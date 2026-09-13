using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class RevisionPackageReviewRow2D
    {
        internal RevisionPackageReviewRow2D(RevisionOverlayItem2D item)
        {
            if (item == null) throw new ArgumentNullException("item");
            var evidence = item.Current ?? item.Previous ?? throw new InvalidOperationException("Revision review row requires evidence.");
            MarkupId = item.MarkupId;
            Kind = item.Kind;
            Classification = evidence.Classification;
            Zone = evidence.Zone;
            Layer = evidence.Layer;
            Unit = evidence.Unit;
            PreviousSourceReference = item.Previous == null ? string.Empty : item.Previous.SourceReference;
            CurrentSourceReference = item.Current == null ? string.Empty : item.Current.SourceReference;
            PreviousSourceHandle = item.Previous == null ? string.Empty : item.Previous.SourceHandle;
            CurrentSourceHandle = item.Current == null ? string.Empty : item.Current.SourceHandle;
            PreviousQuantity = item.Previous == null ? 0d : item.Previous.Quantity;
            CurrentQuantity = item.Current == null ? 0d : item.Current.Quantity;
            QuantityDelta = item.QuantityDelta;
            EstimateEligible = item.Current != null;
            ValidateRetainedMetadata(item);
        }

        public string MarkupId { get; private set; }
        public RevisionMarkupChangeKind Kind { get; private set; }
        public string Classification { get; private set; }
        public string Zone { get; private set; }
        public string Layer { get; private set; }
        public string Unit { get; private set; }
        public string PreviousSourceReference { get; private set; }
        public string CurrentSourceReference { get; private set; }
        public string PreviousSourceHandle { get; private set; }
        public string CurrentSourceHandle { get; private set; }
        public double PreviousQuantity { get; private set; }
        public double CurrentQuantity { get; private set; }
        public double QuantityDelta { get; private set; }
        public bool EstimateEligible { get; private set; }

        private static void ValidateRetainedMetadata(RevisionOverlayItem2D item)
        {
            if (item.Previous == null || item.Current == null) return;
            if (!string.Equals(item.Previous.Classification, item.Current.Classification, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(item.Previous.Zone, item.Current.Zone, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(item.Previous.Layer, item.Current.Layer, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(item.Previous.Unit, item.Current.Unit, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Retained revision markup changed package grouping metadata; review as remove/add instead of one ambiguous row.");
        }
    }

    public sealed class RevisionPackageQuantitySummary2D
    {
        internal RevisionPackageQuantitySummary2D(string unit, double previousQuantity, double currentQuantity, double quantityDelta, double estimateEligibleCurrentQuantity)
        {
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            PreviousQuantity = QsModelElementSnapshot.Finite(previousQuantity, "previousQuantity");
            CurrentQuantity = QsModelElementSnapshot.Finite(currentQuantity, "currentQuantity");
            QuantityDelta = QsModelElementSnapshot.Finite(quantityDelta, "quantityDelta");
            EstimateEligibleCurrentQuantity = QsModelElementSnapshot.Finite(estimateEligibleCurrentQuantity, "estimateEligibleCurrentQuantity");
            if (PreviousQuantity < 0d || CurrentQuantity < 0d || EstimateEligibleCurrentQuantity < 0d)
                throw new InvalidOperationException("Revision package quantity summary cannot contain negative absolute quantities.");
        }

        public string Unit { get; private set; }
        public double PreviousQuantity { get; private set; }
        public double CurrentQuantity { get; private set; }
        public double QuantityDelta { get; private set; }
        public double EstimateEligibleCurrentQuantity { get; private set; }
    }

    public sealed class RevisionPackageGroupSummary2D
    {
        internal RevisionPackageGroupSummary2D(string classification, string zone, string layer, string unit, double previousQuantity, double currentQuantity, double quantityDelta, double estimateEligibleCurrentQuantity)
        {
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Zone = QsModelElementSnapshot.Optional(zone);
            Layer = QsModelElementSnapshot.Optional(layer);
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            PreviousQuantity = QsModelElementSnapshot.Finite(previousQuantity, "previousQuantity");
            CurrentQuantity = QsModelElementSnapshot.Finite(currentQuantity, "currentQuantity");
            QuantityDelta = QsModelElementSnapshot.Finite(quantityDelta, "quantityDelta");
            EstimateEligibleCurrentQuantity = QsModelElementSnapshot.Finite(estimateEligibleCurrentQuantity, "estimateEligibleCurrentQuantity");
            if (PreviousQuantity < 0d || CurrentQuantity < 0d || EstimateEligibleCurrentQuantity < 0d)
                throw new InvalidOperationException("Revision package group summary cannot contain negative absolute quantities.");
        }

        public string Classification { get; private set; }
        public string Zone { get; private set; }
        public string Layer { get; private set; }
        public string Unit { get; private set; }
        public double PreviousQuantity { get; private set; }
        public double CurrentQuantity { get; private set; }
        public double QuantityDelta { get; private set; }
        public double EstimateEligibleCurrentQuantity { get; private set; }
    }

    public sealed class RevisionPackageReviewResult2D
    {
        private sealed class GroupSummaryKey : IEquatable<GroupSummaryKey>
        {
            public GroupSummaryKey(string classification, string zone, string layer, string unit)
            {
                Classification = classification;
                Zone = zone ?? string.Empty;
                Layer = layer ?? string.Empty;
                Unit = unit;
            }

            public string Classification { get; private set; }
            public string Zone { get; private set; }
            public string Layer { get; private set; }
            public string Unit { get; private set; }

            public bool Equals(GroupSummaryKey? other)
            {
                return other != null
                    && string.Equals(Classification, other.Classification, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Zone, other.Zone, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Layer, other.Layer, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Unit, other.Unit, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object? obj) { return Equals(obj as GroupSummaryKey); }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(Classification);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Zone);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Layer);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Unit);
                    return hash;
                }
            }
        }

        internal RevisionPackageReviewResult2D(RevisionTakeoffPackage2D package, IReadOnlyList<RevisionPackageReviewRow2D> rows)
        {
            if (package == null) throw new ArgumentNullException("package");
            if (rows == null) throw new ArgumentNullException("rows");

            PreviousRevision = package.Previous.Sheet.Revision;
            CurrentRevision = package.Current.Sheet.Revision;
            AddedCount = package.AddedCount;
            RemovedCount = package.RemovedCount;
            ChangedCount = package.ChangedCount;
            UnchangedCount = package.UnchangedCount;
            Rows = rows;

            var summaries = rows
                .GroupBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => new RevisionPackageQuantitySummary2D(
                    x.Key,
                    SumFinite(x.Select(r => r.PreviousQuantity)),
                    SumFinite(x.Select(r => r.CurrentQuantity)),
                    SumFinite(x.Select(r => r.QuantityDelta)),
                    SumFinite(x.Where(r => r.EstimateEligible).Select(r => r.CurrentQuantity))))
                .ToList();
            QuantitySummaries = new ReadOnlyCollection<RevisionPackageQuantitySummary2D>(summaries);

            var groupSummaries = rows
                .GroupBy(x => new GroupSummaryKey(x.Classification, x.Zone, x.Layer, x.Unit))
                .Select(x => new RevisionPackageGroupSummary2D(
                    x.Key.Classification,
                    x.Key.Zone,
                    x.Key.Layer,
                    x.Key.Unit,
                    SumFinite(x.Select(r => r.PreviousQuantity)),
                    SumFinite(x.Select(r => r.CurrentQuantity)),
                    SumFinite(x.Select(r => r.QuantityDelta)),
                    SumFinite(x.Where(r => r.EstimateEligible).Select(r => r.CurrentQuantity))))
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Zone, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Layer, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                .ToList();
            GroupSummaries = new ReadOnlyCollection<RevisionPackageGroupSummary2D>(groupSummaries);
        }

        public string PreviousRevision { get; private set; }
        public string CurrentRevision { get; private set; }
        public int AddedCount { get; private set; }
        public int RemovedCount { get; private set; }
        public int ChangedCount { get; private set; }
        public int UnchangedCount { get; private set; }
        public IReadOnlyList<RevisionPackageReviewRow2D> Rows { get; private set; }
        public IReadOnlyList<RevisionPackageQuantitySummary2D> QuantitySummaries { get; private set; }
        public IReadOnlyList<RevisionPackageGroupSummary2D> GroupSummaries { get; private set; }

        private static double SumFinite(IEnumerable<double> values)
        {
            var sum = 0d;
            var compensation = 0d;
            foreach (var value in values)
            {
                var finite = QsModelElementSnapshot.Finite(value, "quantity");
                var adjusted = finite - compensation;
                var next = sum + adjusted;
                compensation = (next - sum) - adjusted;
                sum = QsModelElementSnapshot.Finite(next, "quantityTotal");
            }
            return sum;
        }
    }

    public sealed class RevisionTakeoffPackageReview2D
    {
        public IReadOnlyList<RevisionPackageReviewRow2D> Build(RevisionTakeoffPackage2D package)
        {
            return BuildResult(package).Rows;
        }

        public RevisionPackageReviewResult2D BuildResult(RevisionTakeoffPackage2D package)
        {
            if (package == null) throw new ArgumentNullException("package");
            var rows = package.Overlay
                .Select(x => new RevisionPackageReviewRow2D(x))
                .OrderBy(x => x.MarkupId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var readOnlyRows = new ReadOnlyCollection<RevisionPackageReviewRow2D>(rows);
            return new RevisionPackageReviewResult2D(package, readOnlyRows);
        }
    }
}
