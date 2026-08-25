using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.BricsCAD.V25.Services;

[Flags]
internal enum InterchangeGeneratedOutputKind
{
    None = 0,
    NativeGeometry = 1 << 0,
    Quantity = 1 << 1,
    // Reserved output classes. They are not persisted generated-owner artifacts
    // that FieldMerge can atomically rebuild today, so Create must fail closed.
    Workbook = 1 << 2,
    Trace = 1 << 3,
}

/// <summary>
/// Immutable, explicit post-FieldMerge rebuild request. The importer must never
/// infer project-wide regeneration from invalidation: only semantic elements
/// invalidated by the reviewed merge may be included in this plan.
/// </summary>
internal sealed class InterchangeFieldMergeGeneratedRebuildPlan
{
    private static readonly InterchangeGeneratedOutputKind SupportedKinds =
        InterchangeGeneratedOutputKind.NativeGeometry |
        InterchangeGeneratedOutputKind.Quantity;

    private InterchangeFieldMergeGeneratedRebuildPlan(
        IReadOnlyList<string> elementIds,
        InterchangeGeneratedOutputKind outputKinds)
    {
        ElementIds = elementIds;
        OutputKinds = outputKinds;
    }

    public IReadOnlyList<string> ElementIds { get; }

    public InterchangeGeneratedOutputKind OutputKinds { get; }

    public bool Includes(InterchangeGeneratedOutputKind kind) => (OutputKinds & kind) == kind;

    public bool IsNoOp => ElementIds.Count == 0 || OutputKinds == InterchangeGeneratedOutputKind.None;

    public static InterchangeFieldMergeGeneratedRebuildPlan Create(
        IEnumerable<string>? invalidatedElementIds,
        InterchangeGeneratedOutputKind requestedKinds)
    {
        if ((requestedKinds & ~SupportedKinds) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedKinds),
                requestedKinds,
                "FieldMerge rebuild requested an unsupported generated-output kind. " +
                "Only atomic NativeGeometry and Quantity rebuilds are supported; Workbook/Trace remain explicit external outputs.");
        }

        string[] ids = (invalidatedElementIds ?? Enumerable.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new InterchangeFieldMergeGeneratedRebuildPlan(ids, requestedKinds);
    }
}
