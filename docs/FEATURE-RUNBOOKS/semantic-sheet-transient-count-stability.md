# Semantic Sheet transient known-Count stability

Canonical carrier: Issue #4955 / Lane-Key `issue-4955`.

Runtime: `NOT_APPLICABLE`. This is deterministic Core documentation/model-planning integrity and does not establish licensed BricsCAD `LOCAL_PASS`.

## Boundary

`SemanticSheetPlanner.cs` consumes caller-controlled enumerable data at three bounded Semantic Sheet boundaries:

- `SemanticSheetDefinition.SnapshotPlacements` — maximum 128 placements;
- `SemanticSheetPlanner.MaterializeCatalogBounded` — maximum 10,000 sheet definitions;
- `SemanticSheetPlanner.MaterializeAvailableViewsBounded` — maximum 10,000 available views.

Supported generic, read-only and non-generic collection Count surfaces are integrity evidence. Once admitted, the exact Count must remain stable across every caller-controlled traversal boundary, not only at admission and final publication.

## Required traversal ordering

For each counted source the implementation must:

1. bind supported Count evidence at admission and reject negative, conflicting or oversized evidence;
2. revalidate the admitted Count immediately before caller-controlled `MoveNext()`;
3. execute `MoveNext()`;
4. revalidate Count immediately after `MoveNext()` before using its result;
5. on a successful move, reject the hard ceiling and known-count N+1 before `Current`;
6. read caller-controlled `Current` exactly once for the admitted item;
7. revalidate Count immediately after `Current` before retaining the item;
8. after traversal, require exact observed cardinality and perform a final Count rebound.

Pure streaming enumerables without supported Count evidence remain one-pass and are governed by the independent hard ceilings.

## Threat model

A hostile collection can mutate its Count from inside `MoveNext()` and restore it when `Current` is read, or mutate Count from inside `Current` and restore it on the next `MoveNext()`. Admission-plus-final-only validation misses both transient windows. The #4955 contract rejects the first case before `Current` and the second immediately after `Current` before retention.

## Deterministic evidence

`SemanticSheetTransientCountStabilitySmoke` covers both transient windows for placements, catalog definitions and available views, plus stable counted controls. `SemanticSheetKnownCountIntegritySmoke` remains authoritative for historical N+1 no-overread and post-traversal rebound behavior.

Source-safe validation:

```text
python scripts/preflight-semantic-sheet-known-count-integrity.py
python scripts/preflight-semantic-sheet-transient-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Hosted CI/Core/V25 compile checks are repository integration evidence only. They must not be described as licensed BricsCAD runtime PASS.
