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

    public sealed class RevisionTakeoffPackageReview2D
    {
        public IReadOnlyList<RevisionPackageReviewRow2D> Build(RevisionTakeoffPackage2D package)
        {
            if (package == null) throw new ArgumentNullException("package");
            var rows = package.Overlay
                .Select(x => new RevisionPackageReviewRow2D(x))
                .OrderBy(x => x.MarkupId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new ReadOnlyCollection<RevisionPackageReviewRow2D>(rows);
        }
    }
}
