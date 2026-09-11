using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public enum DrawingSheetSourceKind { Pdf, RasterImage }
    public enum RevisionMarkupChangeKind { Added, Removed, Changed, Unchanged }

    public sealed class DrawingSheet2D
    {
        public DrawingSheet2D(string id, string name, DrawingSheetSourceKind sourceKind, string sourceReference, string revision, DrawingCalibration calibration)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            Name = QsModelElementSnapshot.Require(name, "name");
            SourceKind = sourceKind;
            SourceReference = QsModelElementSnapshot.Require(sourceReference, "sourceReference");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Calibration = calibration ?? throw new ArgumentNullException("calibration");
        }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public DrawingSheetSourceKind SourceKind { get; private set; }
        public string SourceReference { get; private set; }
        public string Revision { get; private set; }
        public DrawingCalibration Calibration { get; private set; }
    }

    public sealed class TakeoffMarkup2D
    {
        public TakeoffMarkup2D(string id, string sheetId, TakeoffMeasurementKind kind, double rawValue, string classification, string zone, string layer, string sourceHandle)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            SheetId = QsModelElementSnapshot.Require(sheetId, "sheetId");
            Kind = kind;
            RawValue = DrawingCalibration.Positive(rawValue, "rawValue");
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Zone = QsModelElementSnapshot.Optional(zone);
            Layer = QsModelElementSnapshot.Optional(layer);
            SourceHandle = QsModelElementSnapshot.Require(sourceHandle, "sourceHandle");
        }
        public string Id { get; private set; }
        public string SheetId { get; private set; }
        public TakeoffMeasurementKind Kind { get; private set; }
        public double RawValue { get; private set; }
        public string Classification { get; private set; }
        public string Zone { get; private set; }
        public string Layer { get; private set; }
        public string SourceHandle { get; private set; }
    }

    public sealed class TakeoffQuantityEvidence2D
    {
        public TakeoffQuantityEvidence2D(string markupId, string sheetId, string revision, string sourceReference, string sourceHandle, string classification, string zone, string layer, double quantity, string unit)
        {
            MarkupId = QsModelElementSnapshot.Require(markupId, "markupId");
            SheetId = QsModelElementSnapshot.Require(sheetId, "sheetId");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            SourceReference = QsModelElementSnapshot.Require(sourceReference, "sourceReference");
            SourceHandle = QsModelElementSnapshot.Require(sourceHandle, "sourceHandle");
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Zone = QsModelElementSnapshot.Optional(zone);
            Layer = QsModelElementSnapshot.Optional(layer);
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            Unit = QsModelElementSnapshot.Require(unit, "unit");
        }
        public string MarkupId { get; private set; }
        public string SheetId { get; private set; }
        public string Revision { get; private set; }
        public string SourceReference { get; private set; }
        public string SourceHandle { get; private set; }
        public string Classification { get; private set; }
        public string Zone { get; private set; }
        public string Layer { get; private set; }
        public double Quantity { get; private set; }
        public string Unit { get; private set; }
    }

    public sealed class TakeoffSheetResult2D
    {
        public TakeoffSheetResult2D(DrawingSheet2D sheet, IReadOnlyList<TakeoffQuantityEvidence2D> evidence)
        {
            Sheet = sheet ?? throw new ArgumentNullException("sheet");
            Evidence = evidence ?? throw new ArgumentNullException("evidence");
        }
        public DrawingSheet2D Sheet { get; private set; }
        public IReadOnlyList<TakeoffQuantityEvidence2D> Evidence { get; private set; }
    }

    public sealed class CalibratedTakeoffEngine2D
    {
        public TakeoffSheetResult2D Extract(DrawingSheet2D sheet, IEnumerable<TakeoffMarkup2D> markups)
        {
            if (sheet == null) throw new ArgumentNullException("sheet");
            if (markups == null) throw new ArgumentNullException("markups");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<TakeoffQuantityEvidence2D>();
            foreach (var markup in markups)
            {
                if (markup == null) throw new ArgumentException("Markup collection contains null.", "markups");
                if (!string.Equals(markup.SheetId, sheet.Id, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Markup belongs to another sheet.");
                if (!ids.Add(markup.Id)) throw new InvalidOperationException("Duplicate markup id.");
                var quantity = markup.Kind == TakeoffMeasurementKind.Count ? markup.RawValue : markup.Kind == TakeoffMeasurementKind.Length ? markup.RawValue * sheet.Calibration.Scale : markup.RawValue * sheet.Calibration.Scale * sheet.Calibration.Scale;
                var unit = markup.Kind == TakeoffMeasurementKind.Count ? "ea" : markup.Kind == TakeoffMeasurementKind.Length ? sheet.Calibration.Unit : sheet.Calibration.Unit + "2";
                result.Add(new TakeoffQuantityEvidence2D(markup.Id, sheet.Id, sheet.Revision, sheet.SourceReference, markup.SourceHandle, markup.Classification, markup.Zone, markup.Layer, quantity, unit));
            }
            return new TakeoffSheetResult2D(sheet, new ReadOnlyCollection<TakeoffQuantityEvidence2D>(result));
        }
    }

    public sealed class RevisionMarkupDelta2D
    {
        public RevisionMarkupDelta2D(string markupId, RevisionMarkupChangeKind kind, TakeoffQuantityEvidence2D previous, TakeoffQuantityEvidence2D current)
        {
            MarkupId = QsModelElementSnapshot.Require(markupId, "markupId");
            Kind = kind;
            Previous = previous;
            Current = current;
        }
        public string MarkupId { get; private set; }
        public RevisionMarkupChangeKind Kind { get; private set; }
        public TakeoffQuantityEvidence2D Previous { get; private set; }
        public TakeoffQuantityEvidence2D Current { get; private set; }
        public double QuantityDelta { get { return (Current == null ? 0d : Current.Quantity) - (Previous == null ? 0d : Previous.Quantity); } }
    }

    public sealed class DrawingRevisionComparer2D
    {
        public IReadOnlyList<RevisionMarkupDelta2D> Compare(TakeoffSheetResult2D previous, TakeoffSheetResult2D current)
        {
            if (previous == null) throw new ArgumentNullException("previous");
            if (current == null) throw new ArgumentNullException("current");
            if (!string.Equals(previous.Sheet.Id, current.Sheet.Id, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Revision compare requires the same logical sheet id.");
            var oldById = previous.Evidence.ToDictionary(x => x.MarkupId, StringComparer.OrdinalIgnoreCase);
            var newById = current.Evidence.ToDictionary(x => x.MarkupId, StringComparer.OrdinalIgnoreCase);
            var ids = oldById.Keys.Union(newById.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
            var result = new List<RevisionMarkupDelta2D>();
            foreach (var id in ids)
            {
                TakeoffQuantityEvidence2D oldValue;
                TakeoffQuantityEvidence2D newValue;
                oldById.TryGetValue(id, out oldValue);
                newById.TryGetValue(id, out newValue);
                var kind = oldValue == null ? RevisionMarkupChangeKind.Added : newValue == null ? RevisionMarkupChangeKind.Removed : Equivalent(oldValue, newValue) ? RevisionMarkupChangeKind.Unchanged : RevisionMarkupChangeKind.Changed;
                result.Add(new RevisionMarkupDelta2D(id, kind, oldValue, newValue));
            }
            return new ReadOnlyCollection<RevisionMarkupDelta2D>(result);
        }
        private static bool Equivalent(TakeoffQuantityEvidence2D left, TakeoffQuantityEvidence2D right)
        {
            return string.Equals(left.Classification, right.Classification, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Zone, right.Zone, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Layer, right.Layer, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Unit, right.Unit, StringComparison.OrdinalIgnoreCase)
                && Math.Abs(left.Quantity - right.Quantity) < 1e-12;
        }
    }

    public sealed class TakeoffWorkflowLine
    {
        public TakeoffWorkflowLine(string classification, string zone, string unit, double measuredQuantity, double formulaQuantity, double unitRate, int evidenceCount)
        {
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Zone = QsModelElementSnapshot.Optional(zone);
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            MeasuredQuantity = QsModelElementSnapshot.Finite(measuredQuantity, "measuredQuantity");
            FormulaQuantity = QsModelElementSnapshot.Finite(formulaQuantity, "formulaQuantity");
            UnitRate = QsModelElementSnapshot.Finite(unitRate, "unitRate");
            if (evidenceCount < 1) throw new ArgumentOutOfRangeException("evidenceCount");
            EvidenceCount = evidenceCount;
        }
        public string Classification { get; private set; }
        public string Zone { get; private set; }
        public string Unit { get; private set; }
        public double MeasuredQuantity { get; private set; }
        public double FormulaQuantity { get; private set; }
        public double UnitRate { get; private set; }
        public int EvidenceCount { get; private set; }
        public double EstimatedCost { get { return FormulaQuantity * UnitRate; } }
    }

    public sealed class AutodeskTakeoffWorkflow
    {
        private sealed class WorkflowRow
        {
            public WorkflowRow(string classification, string zone, string unit, double quantity) { Classification = classification; Zone = zone ?? string.Empty; Unit = unit; Quantity = quantity; }
            public string Classification { get; private set; }
            public string Zone { get; private set; }
            public string Unit { get; private set; }
            public double Quantity { get; private set; }
        }

        private sealed class WorkflowKey : IEquatable<WorkflowKey>
        {
            public WorkflowKey(string classification, string zone, string unit) { Classification = classification; Zone = zone ?? string.Empty; Unit = unit; }
            public string Classification { get; private set; }
            public string Zone { get; private set; }
            public string Unit { get; private set; }
            public bool Equals(WorkflowKey other)
            {
                return other != null && string.Equals(Classification, other.Classification, StringComparison.OrdinalIgnoreCase) && string.Equals(Zone, other.Zone, StringComparison.OrdinalIgnoreCase) && string.Equals(Unit, other.Unit, StringComparison.OrdinalIgnoreCase);
            }
            public override bool Equals(object obj) { return Equals(obj as WorkflowKey); }
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(Classification);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Zone);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Unit);
                    return hash;
                }
            }
        }

        public IReadOnlyList<TakeoffWorkflowLine> BuildInventoryAndEstimate(IEnumerable<TakeoffQuantityEvidence2D> drawingEvidence, IEnumerable<IfcQtoItem> bimQuantities, Func<string, double, double> formula, Func<string, string, double> rateProvider)
        {
            if (drawingEvidence == null) throw new ArgumentNullException("drawingEvidence");
            if (bimQuantities == null) throw new ArgumentNullException("bimQuantities");
            if (formula == null) throw new ArgumentNullException("formula");
            if (rateProvider == null) throw new ArgumentNullException("rateProvider");
            var rows = new List<WorkflowRow>();
            rows.AddRange(drawingEvidence.Select(x => new WorkflowRow(x.Classification, x.Zone, x.Unit, x.Quantity)));
            rows.AddRange(bimQuantities.Select(x => new WorkflowRow(QsModelElementSnapshot.Require(x.Classification, "classification"), x.Storey, x.Unit, x.Quantity)));
            return rows
                .GroupBy(x => new WorkflowKey(x.Classification, x.Zone, x.Unit))
                .Select(g =>
                {
                    var measured = g.Sum(x => x.Quantity);
                    var adjusted = QsModelElementSnapshot.Finite(formula(g.Key.Classification, measured), "formulaQuantity");
                    var rate = QsModelElementSnapshot.Finite(rateProvider(g.Key.Classification, g.Key.Unit), "unitRate");
                    return new TakeoffWorkflowLine(g.Key.Classification, g.Key.Zone, g.Key.Unit, measured, adjusted, rate, g.Count());
                })
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Zone, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
