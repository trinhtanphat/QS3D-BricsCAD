using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using QS3D.Core.Reporting;

namespace QS3D.Core.BenchmarkParity
{
    public enum TakeoffPackageSourceKind
    {
        Drawing2D,
        Bim3D
    }

    public enum TakeoffPackageReadiness
    {
        Draft,
        Ready,
        Blocked
    }

    public enum TakeoffPackageValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class TakeoffPackageDefinition
    {
        public TakeoffPackageDefinition(string id, string name, string revision, string classificationProfile, string formulaProfile)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            Name = QsModelElementSnapshot.Require(name, "name");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            ClassificationProfile = QsModelElementSnapshot.Require(classificationProfile, "classificationProfile");
            FormulaProfile = QsModelElementSnapshot.Require(formulaProfile, "formulaProfile");
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Revision { get; private set; }
        public string ClassificationProfile { get; private set; }
        public string FormulaProfile { get; private set; }
    }

    public sealed class TakeoffPackageSource
    {
        public TakeoffPackageSource(string id, TakeoffPackageSourceKind kind, string sourceReference, string revision)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            Kind = kind;
            SourceReference = QsModelElementSnapshot.Require(sourceReference, "sourceReference");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
        }

        public string Id { get; private set; }
        public TakeoffPackageSourceKind Kind { get; private set; }
        public string SourceReference { get; private set; }
        public string Revision { get; private set; }
    }

    public sealed class TakeoffPackageValidationIssue
    {
        public TakeoffPackageValidationIssue(string code, TakeoffPackageValidationSeverity severity, string sourceId, string message)
        {
            Code = QsModelElementSnapshot.Require(code, "code");
            Severity = severity;
            SourceId = QsModelElementSnapshot.Optional(sourceId);
            Message = QsModelElementSnapshot.Require(message, "message");
        }

        public string Code { get; private set; }
        public TakeoffPackageValidationSeverity Severity { get; private set; }
        public string SourceId { get; private set; }
        public string Message { get; private set; }
    }

    public sealed class TakeoffPackageBuildResult
    {
        public TakeoffPackageBuildResult(
            TakeoffPackageDefinition package,
            TakeoffPackageReadiness readiness,
            IEnumerable<TakeoffPackageSource> sources,
            IEnumerable<TakeoffPackageValidationIssue> issues,
            IEnumerable<TakeoffWorkflowLine> inventory)
        {
            Package = package ?? throw new ArgumentNullException("package");
            Readiness = readiness;
            Sources = new ReadOnlyCollection<TakeoffPackageSource>((sources ?? Enumerable.Empty<TakeoffPackageSource>()).ToList());
            Issues = new ReadOnlyCollection<TakeoffPackageValidationIssue>((issues ?? Enumerable.Empty<TakeoffPackageValidationIssue>()).ToList());
            Inventory = new ReadOnlyCollection<TakeoffWorkflowLine>((inventory ?? Enumerable.Empty<TakeoffWorkflowLine>()).ToList());
        }

        public TakeoffPackageDefinition Package { get; private set; }
        public TakeoffPackageReadiness Readiness { get; private set; }
        public IReadOnlyList<TakeoffPackageSource> Sources { get; private set; }
        public IReadOnlyList<TakeoffPackageValidationIssue> Issues { get; private set; }
        public IReadOnlyList<TakeoffWorkflowLine> Inventory { get; private set; }
        public bool CanEstimate { get { return Readiness == TakeoffPackageReadiness.Ready; } }
        public double EstimatedCost { get { return SumEstimatedCosts(Inventory); } }

        private static double SumEstimatedCosts(IEnumerable<TakeoffWorkflowLine> inventory)
        {
            var accumulator = new QuantityReportMath.FiniteAccumulator();
            foreach (var line in inventory)
            {
                try
                {
                    accumulator.Add(line.EstimatedCost, "estimatedCost");
                }
                catch (OverflowException)
                {
                    throw new ArgumentOutOfRangeException("estimatedCost", "must be finite");
                }
            }

            return accumulator.Value("estimatedCost");
        }
    }

    public sealed class AutodeskTakeoffPackageCoordinator
    {
        public TakeoffPackageBuildResult Build(
            TakeoffPackageDefinition package,
            IEnumerable<DrawingSheet2D> drawingSheets,
            IEnumerable<TakeoffQuantityEvidence2D> drawingEvidence,
            IEnumerable<IfcQtoItem> bimQuantities,
            Func<string, double, double> formula,
            Func<string, string, double> rateProvider)
        {
            if (package == null) throw new ArgumentNullException("package");
            if (drawingSheets == null) throw new ArgumentNullException("drawingSheets");
            if (drawingEvidence == null) throw new ArgumentNullException("drawingEvidence");
            if (bimQuantities == null) throw new ArgumentNullException("bimQuantities");
            if (formula == null) throw new ArgumentNullException("formula");
            if (rateProvider == null) throw new ArgumentNullException("rateProvider");

            var sheets = drawingSheets.ToList();
            var evidence = drawingEvidence.ToList();
            var bim = bimQuantities.ToList();
            var issues = new List<TakeoffPackageValidationIssue>();
            var sources = new List<TakeoffPackageSource>();

            var sheetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sheetsById = new Dictionary<string, DrawingSheet2D>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in sheets)
            {
                if (sheet == null)
                {
                    issues.Add(new TakeoffPackageValidationIssue("PKG.NULL_SHEET", TakeoffPackageValidationSeverity.Error, string.Empty, "Drawing package contains a null sheet."));
                    continue;
                }

                if (!sheetIds.Add(sheet.Id))
                    issues.Add(new TakeoffPackageValidationIssue("PKG.DUPLICATE_SHEET", TakeoffPackageValidationSeverity.Error, sheet.Id, "Drawing sheet id is duplicated in the package."));
                else
                    sheetsById.Add(sheet.Id, sheet);

                if (!string.Equals(sheet.Revision, package.Revision, StringComparison.OrdinalIgnoreCase))
                    issues.Add(new TakeoffPackageValidationIssue("PKG.STALE_DRAWING_REVISION", TakeoffPackageValidationSeverity.Error, sheet.Id, "Drawing sheet revision does not match the package revision."));

                sources.Add(new TakeoffPackageSource(sheet.Id, TakeoffPackageSourceKind.Drawing2D, sheet.SourceReference, sheet.Revision));
            }

            var evidenceIdsBySheet = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in evidence)
            {
                if (item == null)
                {
                    issues.Add(new TakeoffPackageValidationIssue("PKG.NULL_EVIDENCE", TakeoffPackageValidationSeverity.Error, string.Empty, "Drawing quantity evidence contains a null item."));
                    continue;
                }

                HashSet<string>? markupIds;
                if (!evidenceIdsBySheet.TryGetValue(item.SheetId, out markupIds))
                {
                    markupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    evidenceIdsBySheet.Add(item.SheetId, markupIds);
                }
                if (!markupIds.Add(item.MarkupId))
                    issues.Add(new TakeoffPackageValidationIssue("PKG.DUPLICATE_EVIDENCE", TakeoffPackageValidationSeverity.Error, item.MarkupId, "Drawing evidence markup id is duplicated for the same sheet."));

                DrawingSheet2D? sourceSheet;
                if (!sheetsById.TryGetValue(item.SheetId, out sourceSheet))
                {
                    issues.Add(new TakeoffPackageValidationIssue("PKG.ORPHAN_EVIDENCE", TakeoffPackageValidationSeverity.Error, item.MarkupId, "Drawing evidence references a sheet that is not part of the package."));
                }
                else if (!string.Equals(item.SourceReference, sourceSheet.SourceReference, StringComparison.Ordinal))
                {
                    issues.Add(new TakeoffPackageValidationIssue("PKG.STALE_EVIDENCE_SOURCE", TakeoffPackageValidationSeverity.Error, item.MarkupId, "Drawing evidence source does not match the package sheet source."));
                }

                if (!string.Equals(item.Revision, package.Revision, StringComparison.OrdinalIgnoreCase))
                    issues.Add(new TakeoffPackageValidationIssue("PKG.STALE_EVIDENCE_REVISION", TakeoffPackageValidationSeverity.Error, item.MarkupId, "Drawing evidence revision does not match the package revision."));
            }

            var bimSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var bimQuantityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in bim)
            {
                if (item == null)
                {
                    issues.Add(new TakeoffPackageValidationIssue("PKG.NULL_BIM_QUANTITY", TakeoffPackageValidationSeverity.Error, string.Empty, "BIM quantity input contains a null item."));
                    continue;
                }

                if (item.Quantity < 0d)
                    issues.Add(new TakeoffPackageValidationIssue("PKG.NEGATIVE_BIM_QUANTITY", TakeoffPackageValidationSeverity.Error, item.Guid, "BIM quantity evidence cannot be negative."));

                var quantityIdentity = item.Guid + "\u001f" + item.QuantityName + "\u001f" + item.Unit;
                if (!bimQuantityIds.Add(quantityIdentity))
                    issues.Add(new TakeoffPackageValidationIssue("PKG.DUPLICATE_BIM_QUANTITY", TakeoffPackageValidationSeverity.Error, item.Guid, "BIM quantity identity (Guid, QuantityName, Unit) is duplicated in the package."));

                if (bimSourceIds.Add(item.Guid))
                    sources.Add(new TakeoffPackageSource(item.Guid, TakeoffPackageSourceKind.Bim3D, item.Entity, package.Revision));
            }

            if (sources.Count == 0)
                issues.Add(new TakeoffPackageValidationIssue("PKG.NO_SOURCE", TakeoffPackageValidationSeverity.Error, package.Id, "Takeoff package must contain at least one drawing or BIM source."));

            if (evidence.Count == 0 && bim.Count == 0)
                issues.Add(new TakeoffPackageValidationIssue("PKG.NO_QUANTITY", TakeoffPackageValidationSeverity.Warning, package.Id, "Takeoff package contains no quantity evidence yet."));

            if (issues.Any(x => x.Severity == TakeoffPackageValidationSeverity.Error))
                return new TakeoffPackageBuildResult(package, TakeoffPackageReadiness.Blocked, sources, issues, Enumerable.Empty<TakeoffWorkflowLine>());

            IReadOnlyList<TakeoffWorkflowLine> inventory;
            try
            {
                inventory = new AutodeskTakeoffWorkflow().BuildInventoryAndEstimate(evidence, bim, formula, rateProvider);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is StackOverflowException))
            {
                issues.Add(new TakeoffPackageValidationIssue(
                    "PKG.EVALUATION_FAILED",
                    TakeoffPackageValidationSeverity.Error,
                    package.Id,
                    "Takeoff package formula/rate evaluation failed: " + ex.Message));
                return new TakeoffPackageBuildResult(package, TakeoffPackageReadiness.Blocked, sources, issues, Enumerable.Empty<TakeoffWorkflowLine>());
            }

            var readiness = inventory.Count == 0 ? TakeoffPackageReadiness.Draft : TakeoffPackageReadiness.Ready;
            return new TakeoffPackageBuildResult(package, readiness, sources, issues, inventory);
        }
    }
}
