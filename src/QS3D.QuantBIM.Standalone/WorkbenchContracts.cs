namespace QS3D.QuantBIM.Standalone;

public sealed record IfcOpenRequest(string Path, string? ExpectedSha256 = null);
public sealed record IfcDocument(string DocumentId, string Schema, string SourcePath, string SourceSha256, IReadOnlyList<IfcComponent> Components);
public sealed record IfcComponent(string GlobalId, string Type, string Name, string? SpatialPath, IReadOnlyDictionary<string, string> Properties, QuantityEvidence Evidence);
public sealed record QuantityEvidence(string SourceSha256, string GlobalId, string Method, IReadOnlyList<QuantityValue> Values);
public sealed record QuantityValue(string Name, decimal Value, string Unit);
public sealed record ComponentFilter(IReadOnlySet<string>? Types = null, string? SpatialPrefix = null, IReadOnlyDictionary<string, string>? Properties = null);
public sealed record SelectionSet(string Id, string Name, IReadOnlySet<string> GlobalIds);
public sealed record BoqItem(string Code, string Description, string Unit, decimal Quantity, IReadOnlyList<string> EvidenceGlobalIds);

/// <summary>Parser boundary. Implementations may use an IFC library, but must not depend on BricsCAD.</summary>
public interface IIfcDocumentReader
{
    Task<IfcDocument> OpenAsync(IfcOpenRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Viewer boundary kept optional so headless QTO remains supported.</summary>
public interface IStandaloneModelViewer
{
    Task LoadAsync(IfcDocument document, CancellationToken cancellationToken = default);
    Task FocusAsync(IReadOnlyCollection<string> globalIds, CancellationToken cancellationToken = default);
    Task SetVisibleAsync(IReadOnlyCollection<string> globalIds, CancellationToken cancellationToken = default);
}

public interface IQuantityTakeoffEngine
{
    IReadOnlyList<IfcComponent> Filter(IfcDocument document, ComponentFilter filter);
    SelectionSet CreateSelectionSet(string id, string name, IEnumerable<IfcComponent> components);
    IReadOnlyList<BoqItem> BuildBoq(IEnumerable<IfcComponent> components, Func<IfcComponent, string> classifier);
}