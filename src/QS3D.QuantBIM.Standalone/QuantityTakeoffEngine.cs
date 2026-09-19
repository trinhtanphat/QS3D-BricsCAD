namespace QS3D.QuantBIM.Standalone;

public sealed class QuantityTakeoffEngine : IQuantityTakeoffEngine
{
    public IReadOnlyList<IfcComponent> Filter(IfcDocument document, ComponentFilter filter) => document.Components
        .Where(c => filter.Types is null || filter.Types.Contains(c.Type))
        .Where(c => filter.SpatialPrefix is null || (c.SpatialPath?.StartsWith(filter.SpatialPrefix, StringComparison.Ordinal) ?? false))
        .Where(c => filter.Properties is null || filter.Properties.All(p => c.Properties.TryGetValue(p.Key, out var value) && StringComparer.Ordinal.Equals(value, p.Value)))
        .OrderBy(c => c.GlobalId, StringComparer.Ordinal)
        .ToArray();

    public SelectionSet CreateSelectionSet(string id, string name, IEnumerable<IfcComponent> components) =>
        new(id, name, components.Select(c => c.GlobalId).ToHashSet(StringComparer.Ordinal));

    public IReadOnlyList<BoqItem> BuildBoq(IEnumerable<IfcComponent> components, Func<IfcComponent, string> classifier) => components
        .SelectMany(c => c.Evidence.Values.Select(q => new { Component = c, Quantity = q, Code = classifier(c) }))
        .GroupBy(x => new { x.Code, x.Quantity.Name, x.Quantity.Unit })
        .OrderBy(g => g.Key.Code, StringComparer.Ordinal).ThenBy(g => g.Key.Name, StringComparer.Ordinal).ThenBy(g => g.Key.Unit, StringComparer.Ordinal)
        .Select(g => new BoqItem(g.Key.Code, g.Key.Name, g.Key.Unit, g.Sum(x => x.Quantity.Value), g.Select(x => x.Component.GlobalId).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray()))
        .ToArray();
}