using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    public enum ComponentRecognitionStatus { Proposed, Accepted, Corrected, Rejected }
    public enum ComponentSourceKind { DrawingRecognition, IfcModel, Manual }

    public sealed class QuantityEvidence
    {
        public QuantityEvidence(string sourceId, string sourceReference, string revision, string method, double confidence)
        {
            SourceId = QsModelElementSnapshot.Require(sourceId, "sourceId");
            SourceReference = QsModelElementSnapshot.Require(sourceReference, "sourceReference");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Method = QsModelElementSnapshot.Require(method, "method");
            if (double.IsNaN(confidence) || double.IsInfinity(confidence) || confidence < 0d || confidence > 1d) throw new ArgumentOutOfRangeException("confidence");
            Confidence = confidence;
        }
        public string SourceId { get; private set; }
        public string SourceReference { get; private set; }
        public string Revision { get; private set; }
        public string Method { get; private set; }
        public double Confidence { get; private set; }
    }

    public sealed class RecognizedQsComponent
    {
        public RecognizedQsComponent(string id, string componentType, string classification, string storey, double length, double width, double height, ComponentSourceKind sourceKind, QuantityEvidence evidence)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            ComponentType = QsModelElementSnapshot.Require(componentType, "componentType");
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Storey = QsModelElementSnapshot.Optional(storey);
            Length = DrawingCalibration.Positive(length, "length");
            Width = DrawingCalibration.Positive(width, "width");
            Height = DrawingCalibration.Positive(height, "height");
            SourceKind = sourceKind;
            Evidence = evidence ?? throw new ArgumentNullException("evidence");
        }
        public string Id { get; private set; }
        public string ComponentType { get; private set; }
        public string Classification { get; private set; }
        public string Storey { get; private set; }
        public double Length { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public ComponentSourceKind SourceKind { get; private set; }
        public QuantityEvidence Evidence { get; private set; }
    }

    public sealed class ComponentReviewDecision
    {
        public ComponentReviewDecision(string componentId, ComponentRecognitionStatus status, string reviewer, string reason, double? correctedLength, double? correctedWidth, double? correctedHeight)
        {
            ComponentId = QsModelElementSnapshot.Require(componentId, "componentId");
            Status = status;
            Reviewer = QsModelElementSnapshot.Require(reviewer, "reviewer");
            Reason = QsModelElementSnapshot.Require(reason, "reason");
            CorrectedLength = ValidateCorrection(correctedLength, "correctedLength");
            CorrectedWidth = ValidateCorrection(correctedWidth, "correctedWidth");
            CorrectedHeight = ValidateCorrection(correctedHeight, "correctedHeight");
            if (status == ComponentRecognitionStatus.Corrected && (!CorrectedLength.HasValue || !CorrectedWidth.HasValue || !CorrectedHeight.HasValue)) throw new ArgumentException("Corrected review requires all dimensions.");
        }
        private static double? ValidateCorrection(double? value, string name)
        {
            if (!value.HasValue) return null;
            return DrawingCalibration.Positive(value.Value, name);
        }
        public string ComponentId { get; private set; }
        public ComponentRecognitionStatus Status { get; private set; }
        public string Reviewer { get; private set; }
        public string Reason { get; private set; }
        public double? CorrectedLength { get; private set; }
        public double? CorrectedWidth { get; private set; }
        public double? CorrectedHeight { get; private set; }
    }

    public sealed class CubicostQuantityLine
    {
        public CubicostQuantityLine(string componentId, string classification, string storey, double concreteVolume, double formworkArea, ComponentRecognitionStatus reviewStatus, QuantityEvidence evidence)
        {
            ComponentId = QsModelElementSnapshot.Require(componentId, "componentId");
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Storey = QsModelElementSnapshot.Optional(storey);
            ConcreteVolume = QsModelElementSnapshot.Finite(concreteVolume, "concreteVolume");
            FormworkArea = QsModelElementSnapshot.Finite(formworkArea, "formworkArea");
            ReviewStatus = reviewStatus;
            Evidence = evidence ?? throw new ArgumentNullException("evidence");
        }
        public string ComponentId { get; private set; }
        public string Classification { get; private set; }
        public string Storey { get; private set; }
        public double ConcreteVolume { get; private set; }
        public double FormworkArea { get; private set; }
        public ComponentRecognitionStatus ReviewStatus { get; private set; }
        public QuantityEvidence Evidence { get; private set; }
    }

    public sealed class CubicostConcreteFormworkWorkflow
    {
        public IReadOnlyList<CubicostQuantityLine> Quantify(IEnumerable<RecognizedQsComponent> components, IEnumerable<ComponentReviewDecision> reviews, bool includeEnds)
        {
            if (components == null) throw new ArgumentNullException("components");
            var reviewById = (reviews ?? Enumerable.Empty<ComponentReviewDecision>()).GroupBy(x => x.ComponentId, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);
            var result = new List<CubicostQuantityLine>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var component in components)
            {
                if (component == null) throw new ArgumentException("Component collection contains null.", "components");
                if (!ids.Add(component.Id)) throw new InvalidOperationException("Duplicate recognized component id: " + component.Id + ".");
                ComponentReviewDecision? review;
                reviewById.TryGetValue(component.Id, out review);
                var status = review == null ? ComponentRecognitionStatus.Proposed : review.Status;
                if (status == ComponentRecognitionStatus.Rejected) continue;
                var corrected = review != null && status == ComponentRecognitionStatus.Corrected;
                var length = corrected ? review!.CorrectedLength.GetValueOrDefault(component.Length) : component.Length;
                var width = corrected ? review!.CorrectedWidth.GetValueOrDefault(component.Width) : component.Width;
                var height = corrected ? review!.CorrectedHeight.GetValueOrDefault(component.Height) : component.Height;
                var quantity = new ConcreteFormworkCalculator().RectangularMember(length, width, height, includeEnds);
                result.Add(new CubicostQuantityLine(component.Id, component.Classification, component.Storey, quantity.ConcreteVolume, quantity.FormworkArea, status, component.Evidence));
            }
            return new ReadOnlyCollection<CubicostQuantityLine>(result.OrderBy(x => x.Storey, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.ComponentId, StringComparer.OrdinalIgnoreCase).ToList());
        }

        public IReadOnlyList<TakeoffInventoryLine> BuildInventory(IEnumerable<CubicostQuantityLine> lines)
        {
            if (lines == null) throw new ArgumentNullException("lines");
            var snapshot = lines.ToList();
            var inventory = new List<TakeoffInventoryLine>();
            inventory.AddRange(snapshot.GroupBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).Select(g => new TakeoffInventoryLine(g.Key + ".CONCRETE", "m3", g.Sum(x => x.ConcreteVolume), g.Count())));
            inventory.AddRange(snapshot.GroupBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).Select(g => new TakeoffInventoryLine(g.Key + ".FORMWORK", "m2", g.Sum(x => x.FormworkArea), g.Count())));
            return new ReadOnlyCollection<TakeoffInventoryLine>(inventory.OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).ToList());
        }
    }

    public sealed class IfcPropertyNode
    {
        public IfcPropertyNode(string name, string value) { Name = QsModelElementSnapshot.Require(name, "name"); Value = QsModelElementSnapshot.Optional(value); }
        public string Name { get; private set; }
        public string Value { get; private set; }
    }

    public sealed class IfcStandaloneElement
    {
        public IfcStandaloneElement(string guid, string entity, string name, string storey, string type, string classification, IEnumerable<IfcPropertyNode> properties, IEnumerable<IfcQtoItem> quantities, string geometryReference)
        {
            Guid = QsModelElementSnapshot.Require(guid, "guid");
            Entity = QsModelElementSnapshot.Require(entity, "entity");
            Name = QsModelElementSnapshot.Optional(name);
            Storey = QsModelElementSnapshot.Optional(storey);
            Type = QsModelElementSnapshot.Optional(type);
            Classification = QsModelElementSnapshot.Optional(classification);
            Properties = new ReadOnlyCollection<IfcPropertyNode>((properties ?? Enumerable.Empty<IfcPropertyNode>()).ToList());
            Quantities = new ReadOnlyCollection<IfcQtoItem>((quantities ?? Enumerable.Empty<IfcQtoItem>()).ToList());
            GeometryReference = QsModelElementSnapshot.Optional(geometryReference);
        }
        public string Guid { get; private set; }
        public string Entity { get; private set; }
        public string Name { get; private set; }
        public string Storey { get; private set; }
        public string Type { get; private set; }
        public string Classification { get; private set; }
        public IReadOnlyList<IfcPropertyNode> Properties { get; private set; }
        public IReadOnlyList<IfcQtoItem> Quantities { get; private set; }
        public string GeometryReference { get; private set; }
    }

    public sealed class IfcStandaloneDocument
    {
        public IfcStandaloneDocument(string path, string revision, IEnumerable<IfcStandaloneElement> elements)
        {
            Path = QsModelElementSnapshot.Require(path, "path");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Elements = new ReadOnlyCollection<IfcStandaloneElement>((elements ?? throw new ArgumentNullException("elements")).ToList());
        }
        public string Path { get; private set; }
        public string Revision { get; private set; }
        public IReadOnlyList<IfcStandaloneElement> Elements { get; private set; }
    }

    public interface IIfcStandaloneSource
    {
        IfcStandaloneDocument Open(string path);
    }

    public sealed class IfcWorkbenchFilter
    {
        public IfcWorkbenchFilter(string entity, string storey, string type, string classification)
        {
            Entity = QsModelElementSnapshot.Optional(entity);
            Storey = QsModelElementSnapshot.Optional(storey);
            Type = QsModelElementSnapshot.Optional(type);
            Classification = QsModelElementSnapshot.Optional(classification);
        }
        public string Entity { get; private set; }
        public string Storey { get; private set; }
        public string Type { get; private set; }
        public string Classification { get; private set; }
    }

    public sealed class IfcSelectionSet
    {
        public IfcSelectionSet(string name, IEnumerable<string> guids)
        {
            Name = QsModelElementSnapshot.Require(name, "name");
            Guids = new ReadOnlyCollection<string>((guids ?? Enumerable.Empty<string>()).Select(x => QsModelElementSnapshot.Require(x, "guid")).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList());
        }
        public string Name { get; private set; }
        public IReadOnlyList<string> Guids { get; private set; }
    }

    public enum IfcViewCommandKind { FitAll, FocusSelection, Orbit, Pan, Zoom }

    public sealed class IfcViewCommand
    {
        public IfcViewCommand(IfcViewCommandKind kind, double x, double y, double z) { Kind = kind; X = QsModelElementSnapshot.Finite(x, "x"); Y = QsModelElementSnapshot.Finite(y, "y"); Z = QsModelElementSnapshot.Finite(z, "z"); }
        public IfcViewCommandKind Kind { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
    }

    public sealed class QuantBimStandaloneWorkbench
    {
        private readonly IIfcStandaloneSource _source;
        public QuantBimStandaloneWorkbench(IIfcStandaloneSource source) { _source = source ?? throw new ArgumentNullException("source"); }
        public IfcStandaloneDocument Open(string path) { return _source.Open(QsModelElementSnapshot.Require(path, "path")); }

        public IReadOnlyList<IfcStandaloneElement> Filter(IfcStandaloneDocument document, IfcWorkbenchFilter filter)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (filter == null) throw new ArgumentNullException("filter");
            return new ReadOnlyCollection<IfcStandaloneElement>(document.Elements.Where(x => Matches(filter.Entity, x.Entity) && Matches(filter.Storey, x.Storey) && Matches(filter.Type, x.Type) && Matches(filter.Classification, x.Classification)).OrderBy(x => x.Storey, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Entity, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Guid, StringComparer.OrdinalIgnoreCase).ToList());
        }

        public IReadOnlyList<IfcPropertyNode> PropertyTree(IfcStandaloneElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            var nodes = new List<IfcPropertyNode>
            {
                new IfcPropertyNode("Identity.Guid", element.Guid),
                new IfcPropertyNode("Identity.Entity", element.Entity),
                new IfcPropertyNode("Identity.Name", element.Name),
                new IfcPropertyNode("Spatial.Storey", element.Storey),
                new IfcPropertyNode("Type.Name", element.Type),
                new IfcPropertyNode("Classification.Code", element.Classification),
                new IfcPropertyNode("Geometry.Reference", element.GeometryReference)
            };
            nodes.AddRange(element.Properties.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
            return new ReadOnlyCollection<IfcPropertyNode>(nodes);
        }

        public IReadOnlyList<IfcQtoItem> Takeoff(IfcStandaloneDocument document, IfcSelectionSet selection)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (selection == null) throw new ArgumentNullException("selection");
            var selected = new HashSet<string>(selection.Guids, StringComparer.OrdinalIgnoreCase);
            return new ReadOnlyCollection<IfcQtoItem>(document.Elements.Where(x => selected.Contains(x.Guid)).SelectMany(x => x.Quantities).OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Guid, StringComparer.OrdinalIgnoreCase).ToList());
        }

        public IReadOnlyList<TakeoffInventoryLine> BuildBoq(IEnumerable<IfcQtoItem> takeoff)
        {
            return new IfcQtoWorkbench().Aggregate(takeoff ?? throw new ArgumentNullException("takeoff"));
        }

        public string ExportCsv(IEnumerable<TakeoffInventoryLine> boq)
        {
            if (boq == null) throw new ArgumentNullException("boq");
            var builder = new StringBuilder("Classification,Unit,Quantity,SourceCount\r\n");
            foreach (var line in boq.OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(Csv(line.Classification)).Append(',').Append(Csv(line.Unit)).Append(',').Append(line.Quantity.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(line.SourceCount.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            }
            return builder.ToString();
        }

        public IReadOnlyList<IfcViewCommand> DefaultNavigation(IfcSelectionSet selection)
        {
            if (selection == null) throw new ArgumentNullException("selection");
            return new ReadOnlyCollection<IfcViewCommand>(new[] { new IfcViewCommand(IfcViewCommandKind.FitAll, 0d, 0d, 0d), new IfcViewCommand(selection.Guids.Count == 0 ? IfcViewCommandKind.FitAll : IfcViewCommandKind.FocusSelection, 0d, 0d, 0d) });
        }

        private static bool Matches(string filter, string value) { return filter.Length == 0 || string.Equals(filter, value, StringComparison.OrdinalIgnoreCase); }
        private static string Csv(string value) { value = value ?? string.Empty; return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\""; }
    }
}
