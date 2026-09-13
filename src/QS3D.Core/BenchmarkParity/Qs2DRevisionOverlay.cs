using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class RevisionOverlayItem2D
    {
        public RevisionOverlayItem2D(RevisionMarkupDelta2D delta)
        {
            if (delta == null) throw new ArgumentNullException("delta");
            Validate(delta);
            MarkupId = delta.MarkupId;
            Kind = delta.Kind;
            Previous = delta.Previous;
            Current = delta.Current;
            QuantityDelta = delta.QuantityDelta;
        }

        public string MarkupId { get; private set; }
        public RevisionMarkupChangeKind Kind { get; private set; }
        public TakeoffQuantityEvidence2D? Previous { get; private set; }
        public TakeoffQuantityEvidence2D? Current { get; private set; }
        public double QuantityDelta { get; private set; }

        private static void Validate(RevisionMarkupDelta2D delta)
        {
            var previous = delta.Previous;
            var current = delta.Current;
            var validCardinality = delta.Kind == RevisionMarkupChangeKind.Added ? previous == null && current != null
                : delta.Kind == RevisionMarkupChangeKind.Removed ? previous != null && current == null
                : (delta.Kind == RevisionMarkupChangeKind.Changed || delta.Kind == RevisionMarkupChangeKind.Unchanged) && previous != null && current != null;
            if (!validCardinality) throw new InvalidOperationException("Revision overlay delta evidence does not match its change kind.");
            if (previous != null && !string.Equals(previous.MarkupId, delta.MarkupId, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Previous revision evidence markup id does not match the overlay delta.");
            if (current != null && !string.Equals(current.MarkupId, delta.MarkupId, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Current revision evidence markup id does not match the overlay delta.");
        }
    }

    public sealed class DrawingRevisionOverlay2D
    {
        private readonly DrawingRevisionComparer2D comparer = new DrawingRevisionComparer2D();

        public IReadOnlyList<RevisionOverlayItem2D> Build(TakeoffSheetResult2D previous, TakeoffSheetResult2D current)
        {
            var deltas = comparer.Compare(previous, current);
            var result = deltas.Select(x => new RevisionOverlayItem2D(x))
                .OrderBy(x => x.MarkupId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new ReadOnlyCollection<RevisionOverlayItem2D>(result);
        }
    }
}
