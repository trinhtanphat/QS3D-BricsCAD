using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public enum QsQaSeverity { Info, Warning, Error, Critical }
    public enum QsQaGateStatus { Pass, PassWithWarnings, Blocked }
    public enum TakeoffMeasurementKind { Count, Length, Area }
    public enum LiveLinkRefreshState { Current, Updated, MissingSource }

    public sealed class QsModelElementSnapshot
    {
        public QsModelElementSnapshot(string id, string type, string material, string classification, string storey, double length, double width, double height, IDictionary<string, string> properties)
        {
            Id = Require(id, "id");
            Type = Optional(type);
            Material = Optional(material);
            Classification = Optional(classification);
            Storey = Optional(storey);
            Length = Finite(length, "length");
            Width = Finite(width, "width");
            Height = Finite(height, "height");
            Properties = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(properties ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase));
        }

        public string Id { get; private set; }
        public string Type { get; private set; }
        public string Material { get; private set; }
        public string Classification { get; private set; }
        public string Storey { get; private set; }
        public double Length { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public IReadOnlyDictionary<string, string> Properties { get; private set; }

        internal static string Require(string value, string name)
        {
            if (value == null) throw new ArgumentNullException(name);
            var token = value.Trim();
            if (token.Length == 0) throw new ArgumentException("Value cannot be empty.", name);
            return token;
        }

        internal static string Optional(string value) { return value == null ? string.Empty : value.Trim(); }

        internal static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public sealed class QsQaFinding
    {
        public QsQaFinding(string ruleId, QsQaSeverity severity, string elementId, string message)
        {
            RuleId = QsModelElementSnapshot.Require(ruleId, "ruleId");
            Severity = severity;
            ElementId = QsModelElementSnapshot.Require(elementId, "elementId");
            Message = QsModelElementSnapshot.Require(message, "message");
        }
        public string RuleId { get; private set; }
        public QsQaSeverity Severity { get; private set; }
        public string ElementId { get; private set; }
        public string Message { get; private set; }
    }

    public sealed class QsQaProfile
    {
        public QsQaProfile(IEnumerable<string> requiredProperties, bool requireMaterial, bool requireType, bool requireClassification, bool requireStorey, bool requirePositiveDimensions)
        {
            RequiredProperties = new ReadOnlyCollection<string>((requiredProperties ?? Enumerable.Empty<string>()).Select(x => QsModelElementSnapshot.Require(x, "requiredProperties")).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            RequireMaterial = requireMaterial;
            RequireType = requireType;
            RequireClassification = requireClassification;
            RequireStorey = requireStorey;
            RequirePositiveDimensions = requirePositiveDimensions;
        }
        public IReadOnlyList<string> RequiredProperties { get; private set; }
        public bool RequireMaterial { get; private set; }
        public bool RequireType { get; private set; }
        public bool RequireClassification { get; private set; }
        public bool RequireStorey { get; private set; }
        public bool RequirePositiveDimensions { get; private set; }

        public static QsQaProfile StrictIfcQuantity()
        {
            return new QsQaProfile(new[] { "IfcGuid", "IfcEntity", "QuantityUnit" }, true, true, true, true, true);
        }
    }

    public sealed class QsModelQaEngine
    {
        public IReadOnlyList<QsQaFinding> Analyze(IEnumerable<QsModelElementSnapshot> elements, QsQaProfile profile)
        {
            if (elements == null) throw new ArgumentNullException("elements");
            if (profile == null) throw new ArgumentNullException("profile");
            var result = new List<QsQaFinding>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (element == null) throw new ArgumentException("Element collection contains null.", "elements");
                if (!ids.Add(element.Id)) result.Add(new QsQaFinding("QA.DUPLICATE_ID", QsQaSeverity.Critical, element.Id, "Duplicate element identity."));
                if (profile.RequireMaterial && element.Material.Length == 0) result.Add(new QsQaFinding("QA.MISSING_MATERIAL", QsQaSeverity.Error, element.Id, "Material is required before quantity takeoff."));
                if (profile.RequireType && element.Type.Length == 0) result.Add(new QsQaFinding("QA.MISSING_TYPE", QsQaSeverity.Error, element.Id, "Element type is required."));
                if (profile.RequireClassification && element.Classification.Length == 0) result.Add(new QsQaFinding("QA.MISSING_CLASSIFICATION", QsQaSeverity.Error, element.Id, "Classification is required."));
                if (profile.RequireStorey && element.Storey.Length == 0) result.Add(new QsQaFinding("QA.MISSING_STOREY", QsQaSeverity.Error, element.Id, "Spatial containment/storey is required."));
                if (profile.RequirePositiveDimensions && (element.Length <= 0d || element.Width <= 0d || element.Height <= 0d)) result.Add(new QsQaFinding("QA.INVALID_DIMENSIONS", QsQaSeverity.Error, element.Id, "Positive dimensions are required."));
                foreach (var key in profile.RequiredProperties)
                {
                    string value;
                    if (!element.Properties.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value)) result.Add(new QsQaFinding("QA.MISSING_PROPERTY", QsQaSeverity.Error, element.Id, "Required property is missing: " + key + "."));
                }
            }
            return new ReadOnlyCollection<QsQaFinding>(result);
        }
    }

    public sealed class QsQaGateDecision
    {
        public QsQaGateDecision(QsQaGateStatus status, IReadOnlyList<QsQaFinding> findings) { Status = status; Findings = findings; }
        public QsQaGateStatus Status { get; private set; }
        public IReadOnlyList<QsQaFinding> Findings { get; private set; }
        public bool CanTakeoff { get { return Status != QsQaGateStatus.Blocked; } }
    }

    public sealed class QsQaGate
    {
        public QsQaGateDecision Evaluate(IEnumerable<QsModelElementSnapshot> elements, QsQaProfile profile)
        {
            var findings = new QsModelQaEngine().Analyze(elements, profile);
            var blocked = findings.Any(x => x.Severity == QsQaSeverity.Error || x.Severity == QsQaSeverity.Critical);
            var warnings = findings.Any(x => x.Severity == QsQaSeverity.Warning);
            return new QsQaGateDecision(blocked ? QsQaGateStatus.Blocked : warnings ? QsQaGateStatus.PassWithWarnings : QsQaGateStatus.Pass, findings);
        }
    }

    public sealed class DrawingCalibration
    {
        public DrawingCalibration(double drawingDistance, double realDistance, string unit)
        {
            DrawingDistance = Positive(drawingDistance, "drawingDistance");
            RealDistance = Positive(realDistance, "realDistance");
            Unit = QsModelElementSnapshot.Require(unit, "unit");
        }
        public double DrawingDistance { get; private set; }
        public double RealDistance { get; private set; }
        public string Unit { get; private set; }
        public double Scale { get { return RealDistance / DrawingDistance; } }
        internal static double Positive(double value, string name) { QsModelElementSnapshot.Finite(value, name); if (value <= 0d) throw new ArgumentOutOfRangeException(name); return value; }
    }

    public sealed class TakeoffMeasurement2D
    {
        public TakeoffMeasurement2D(string id, string sheetId, TakeoffMeasurementKind kind, double rawValue, DrawingCalibration calibration, string classification)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            SheetId = QsModelElementSnapshot.Require(sheetId, "sheetId");
            Kind = kind;
            RawValue = DrawingCalibration.Positive(rawValue, "rawValue");
            Calibration = calibration ?? throw new ArgumentNullException("calibration");
            Classification = QsModelElementSnapshot.Require(classification, "classification");
        }
        public string Id { get; private set; }
        public string SheetId { get; private set; }
        public TakeoffMeasurementKind Kind { get; private set; }
        public double RawValue { get; private set; }
        public DrawingCalibration Calibration { get; private set; }
        public string Classification { get; private set; }
        public double Quantity { get { return Kind == TakeoffMeasurementKind.Count ? RawValue : Kind == TakeoffMeasurementKind.Length ? RawValue * Calibration.Scale : RawValue * Calibration.Scale * Calibration.Scale; } }
        public string Unit { get { return Kind == TakeoffMeasurementKind.Count ? "ea" : Kind == TakeoffMeasurementKind.Length ? Calibration.Unit : Calibration.Unit + "2"; } }
    }

    public sealed class TakeoffPackage
    {
        private readonly List<TakeoffMeasurement2D> _measurements = new List<TakeoffMeasurement2D>();
        public TakeoffPackage(string id, string name, string revision) { Id = QsModelElementSnapshot.Require(id, "id"); Name = QsModelElementSnapshot.Require(name, "name"); Revision = QsModelElementSnapshot.Require(revision, "revision"); }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Revision { get; private set; }
        public IReadOnlyList<TakeoffMeasurement2D> Measurements { get { return new ReadOnlyCollection<TakeoffMeasurement2D>(_measurements); } }
        public void Add(TakeoffMeasurement2D measurement) { if (measurement == null) throw new ArgumentNullException("measurement"); if (_measurements.Any(x => string.Equals(x.Id, measurement.Id, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Duplicate measurement id."); _measurements.Add(measurement); }
        public IReadOnlyList<TakeoffInventoryLine> BuildInventory() { return _measurements.GroupBy(x => new { x.Classification, x.Unit }).Select(g => new TakeoffInventoryLine(g.Key.Classification, g.Key.Unit, g.Sum(x => x.Quantity), g.Count())).OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).ToList(); }
    }

    public sealed class TakeoffInventoryLine
    {
        public TakeoffInventoryLine(string classification, string unit, double quantity, int sourceCount) { Classification = classification; Unit = unit; Quantity = quantity; SourceCount = sourceCount; }
        public string Classification { get; private set; }
        public string Unit { get; private set; }
        public double Quantity { get; private set; }
        public int SourceCount { get; private set; }
    }

    public sealed class WorkbookLiveLink
    {
        public WorkbookLiveLink(string workbookId, string sheet, string cell, string sourceId, string revision, double quantity)
        {
            WorkbookId = QsModelElementSnapshot.Require(workbookId, "workbookId"); Sheet = QsModelElementSnapshot.Require(sheet, "sheet"); Cell = QsModelElementSnapshot.Require(cell, "cell"); SourceId = QsModelElementSnapshot.Require(sourceId, "sourceId"); Revision = QsModelElementSnapshot.Require(revision, "revision"); Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
        }
        public string WorkbookId { get; private set; }
        public string Sheet { get; private set; }
        public string Cell { get; private set; }
        public string SourceId { get; private set; }
        public string Revision { get; private set; }
        public double Quantity { get; private set; }
    }

    public sealed class LiveLinkRefreshResult
    {
        public LiveLinkRefreshResult(WorkbookLiveLink link, LiveLinkRefreshState state, double quantity) { Link = link; State = state; Quantity = quantity; }
        public WorkbookLiveLink Link { get; private set; }
        public LiveLinkRefreshState State { get; private set; }
        public double Quantity { get; private set; }
    }

    public sealed class WorkbookLiveLinkEngine
    {
        public IReadOnlyList<LiveLinkRefreshResult> Refresh(IEnumerable<WorkbookLiveLink> links, IDictionary<string, double> currentQuantities, string currentRevision)
        {
            if (links == null) throw new ArgumentNullException("links"); if (currentQuantities == null) throw new ArgumentNullException("currentQuantities"); currentRevision = QsModelElementSnapshot.Require(currentRevision, "currentRevision");
            var result = new List<LiveLinkRefreshResult>();
            foreach (var link in links)
            {
                double value;
                if (!currentQuantities.TryGetValue(link.SourceId, out value)) result.Add(new LiveLinkRefreshResult(link, LiveLinkRefreshState.MissingSource, link.Quantity));
                else result.Add(new LiveLinkRefreshResult(link, string.Equals(link.Revision, currentRevision, StringComparison.OrdinalIgnoreCase) && Math.Abs(value - link.Quantity) < 1e-12 ? LiveLinkRefreshState.Current : LiveLinkRefreshState.Updated, value));
            }
            return new ReadOnlyCollection<LiveLinkRefreshResult>(result);
        }
    }

    public sealed class QsIntegrationResource
    {
        public QsIntegrationResource(string projectId, string revision, IReadOnlyList<TakeoffInventoryLine> quantities, DateTime generatedUtc)
        {
            ProjectId = QsModelElementSnapshot.Require(projectId, "projectId"); Revision = QsModelElementSnapshot.Require(revision, "revision"); Quantities = quantities ?? throw new ArgumentNullException("quantities"); GeneratedUtc = generatedUtc.Kind == DateTimeKind.Utc ? generatedUtc : generatedUtc.ToUniversalTime();
        }
        public string ProjectId { get; private set; }
        public string Revision { get; private set; }
        public IReadOnlyList<TakeoffInventoryLine> Quantities { get; private set; }
        public DateTime GeneratedUtc { get; private set; }
    }

    public sealed class QsIntegrationRouteCatalog
    {
        public IReadOnlyList<string> Routes { get { return new[] { "/api/v1/projects/{projectId}/quantities", "/api/v1/projects/{projectId}/boq", "/api/v1/projects/{projectId}/cost", "/api/v1/projects/{projectId}/revisions" }; } }
    }

    public sealed class ConcreteFormworkResult
    {
        public ConcreteFormworkResult(double concreteVolume, double formworkArea) { ConcreteVolume = concreteVolume; FormworkArea = formworkArea; }
        public double ConcreteVolume { get; private set; }
        public double FormworkArea { get; private set; }
    }

    public sealed class ConcreteFormworkCalculator
    {
        public ConcreteFormworkResult RectangularMember(double length, double width, double height, bool includeEnds)
        {
            length = DrawingCalibration.Positive(length, "length"); width = DrawingCalibration.Positive(width, "width"); height = DrawingCalibration.Positive(height, "height");
            var volume = length * width * height;
            var area = 2d * length * (width + height) + (includeEnds ? 2d * width * height : 0d);
            return new ConcreteFormworkResult(volume, area);
        }
    }

    public sealed class IfcQtoItem
    {
        public IfcQtoItem(string guid, string entity, string storey, string classification, string quantityName, double quantity, string unit)
        {
            Guid = QsModelElementSnapshot.Require(guid, "guid"); Entity = QsModelElementSnapshot.Require(entity, "entity"); Storey = QsModelElementSnapshot.Optional(storey); Classification = QsModelElementSnapshot.Optional(classification); QuantityName = QsModelElementSnapshot.Require(quantityName, "quantityName"); Quantity = QsModelElementSnapshot.Finite(quantity, "quantity"); Unit = QsModelElementSnapshot.Require(unit, "unit");
        }
        public string Guid { get; private set; } public string Entity { get; private set; } public string Storey { get; private set; } public string Classification { get; private set; } public string QuantityName { get; private set; } public double Quantity { get; private set; } public string Unit { get; private set; }
    }

    public sealed class IfcQtoWorkbench
    {
        public IReadOnlyList<IfcQtoItem> Filter(IEnumerable<IfcQtoItem> items, string entity, string storey)
        {
            if (items == null) throw new ArgumentNullException("items");
            var snapshot = items.ToList();
            entity = QsModelElementSnapshot.Optional(entity);
            storey = QsModelElementSnapshot.Optional(storey);
            return snapshot.Where(x => (entity.Length == 0 || string.Equals(x.Entity, entity, StringComparison.OrdinalIgnoreCase)) && (storey.Length == 0 || string.Equals(x.Storey, storey, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        public IReadOnlyList<TakeoffInventoryLine> Aggregate(IEnumerable<IfcQtoItem> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            var snapshot = items.ToList();
            return snapshot.GroupBy(x => new { Key = x.Classification.Length == 0 ? x.Entity : x.Classification, x.Unit }).Select(g => new TakeoffInventoryLine(g.Key.Key, g.Key.Unit, g.Sum(x => x.Quantity), g.Count())).ToList();
        }
    }

    public sealed class ConstructionCommitment
    {
        public ConstructionCommitment(string id, string supplier, decimal committed, decimal invoiced, decimal paid, double progress)
        {
            Id = QsModelElementSnapshot.Require(id, "id"); Supplier = QsModelElementSnapshot.Require(supplier, "supplier"); if (committed < 0m || invoiced < 0m || paid < 0m) throw new ArgumentOutOfRangeException("financial values"); if (progress < 0d || progress > 1d) throw new ArgumentOutOfRangeException("progress"); Committed = committed; Invoiced = invoiced; Paid = paid; Progress = progress;
        }
        public string Id { get; private set; } public string Supplier { get; private set; } public decimal Committed { get; private set; } public decimal Invoiced { get; private set; } public decimal Paid { get; private set; } public double Progress { get; private set; }
        public decimal OutstandingCommitment { get { return Committed - Invoiced; } }
        public decimal OutstandingPayment { get { return Invoiced - Paid; } }
    }

    public sealed class ConstructionCostControlSummary
    {
        public ConstructionCostControlSummary(decimal committed, decimal invoiced, decimal paid, decimal forecastAtCompletion, double weightedProgress) { Committed = committed; Invoiced = invoiced; Paid = paid; ForecastAtCompletion = forecastAtCompletion; WeightedProgress = weightedProgress; }
        public decimal Committed { get; private set; } public decimal Invoiced { get; private set; } public decimal Paid { get; private set; } public decimal ForecastAtCompletion { get; private set; } public double WeightedProgress { get; private set; }
    }

    public sealed class ConstructionLifecycleEngine
    {
        public ConstructionCostControlSummary Summarize(IEnumerable<ConstructionCommitment> commitments, decimal approvedVariations)
        {
            if (commitments == null) throw new ArgumentNullException("commitments"); if (approvedVariations < 0m) throw new ArgumentOutOfRangeException("approvedVariations"); var list = commitments.ToList();
            var total = list.Sum(x => x.Committed); var invoiced = list.Sum(x => x.Invoiced); var paid = list.Sum(x => x.Paid); var progress = total == 0m ? 0d : list.Sum(x => (double)x.Committed * x.Progress) / (double)total;
            return new ConstructionCostControlSummary(total, invoiced, paid, total + approvedVariations, progress);
        }
    }
}
