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
        InterchangeGeneratedOutputKind.Quantity |
        InterchangeGeneratedOutputKind.Workbook |
        InterchangeGeneratedOutputKind.Trace;

    private InterchangeFieldMergeGeneratedRebuildPlan(
        IReadOnlyList<Guid> elementIds,
        InterchangeGeneratedOutputKind outputKinds)
    {
        ElementIds = elementIds;
        OutputKinds = outputKinds;
    }

    public IReadOnlyList<Guid> ElementIds { get; }

    public InterchangeGeneratedOutputKind OutputKinds { get; }

    public bool IsNoOp => ElementIds.Count == 0 || OutputKinds == InterchangeGeneratedOutputKind.None;

    public static InterchangeFieldMergeGeneratedRebuildPlan Create(
        IEnumerable<Guid>? invalidatedElementIds,
        InterchangeGeneratedOutputKind requestedKinds)
    {
        if ((requestedKinds & ~SupportedKinds) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedKinds),
                requestedKinds,
                "FieldMerge rebuild contains an unsupported generated-output kind.");
        }

        Guid[] ids = (invalidatedElementIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        return new InterchangeFieldMergeGeneratedRebuildPlan(ids, requestedKinds);
    }
}
