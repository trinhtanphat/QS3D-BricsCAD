using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Revision-aware package boundary for CostX/Autodesk-style 2D takeoff workflows.
    /// It exposes validated revision UX data while guaranteeing downstream estimate
    /// generation consumes only canonical evidence from the current drawing revision.
    /// </summary>
    public sealed class RevisionTakeoffPackage2D
    {
        private readonly AutodeskTakeoffWorkflow workflow = new AutodeskTakeoffWorkflow();

        public RevisionTakeoffPackage2D(TakeoffSheetResult2D previous, TakeoffSheetResult2D current)
        {
            if (previous == null) throw new ArgumentNullException("previous");
            if (current == null) throw new ArgumentNullException("current");

            Previous = previous;
            Current = current;
            Overlay = new DrawingRevisionOverlay2D().Build(previous, current);
            CurrentEvidence = new ReadOnlyCollection<TakeoffQuantityEvidence2D>(
                current.Evidence.OrderBy(x => x.MarkupId, StringComparer.OrdinalIgnoreCase).ToList());

            AddedCount = Overlay.Count(x => x.Kind == RevisionMarkupChangeKind.Added);
            RemovedCount = Overlay.Count(x => x.Kind == RevisionMarkupChangeKind.Removed);
            ChangedCount = Overlay.Count(x => x.Kind == RevisionMarkupChangeKind.Changed);
            UnchangedCount = Overlay.Count(x => x.Kind == RevisionMarkupChangeKind.Unchanged);
        }

        public TakeoffSheetResult2D Previous { get; private set; }
        public TakeoffSheetResult2D Current { get; private set; }
        public IReadOnlyList<RevisionOverlayItem2D> Overlay { get; private set; }
        public IReadOnlyList<TakeoffQuantityEvidence2D> CurrentEvidence { get; private set; }
        public int AddedCount { get; private set; }
        public int RemovedCount { get; private set; }
        public int ChangedCount { get; private set; }
        public int UnchangedCount { get; private set; }

        public IReadOnlyList<TakeoffWorkflowLine> BuildInventoryAndEstimate(
            IEnumerable<IfcQtoItem> bimQuantities,
            Func<string, double, double> formula,
            Func<string, string, double> rateProvider)
        {
            if (bimQuantities == null) throw new ArgumentNullException("bimQuantities");
            if (formula == null) throw new ArgumentNullException("formula");
            if (rateProvider == null) throw new ArgumentNullException("rateProvider");

            // Deliberately source drawing quantities from CurrentEvidence instead of
            // overlay Previous records so Removed evidence cannot leak into estimate.
            return workflow.BuildInventoryAndEstimate(CurrentEvidence, bimQuantities, formula, rateProvider);
        }
    }
}