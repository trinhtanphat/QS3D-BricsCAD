# Deep-cost known-Count no-overread

## Scope

This source-safe Core contract covers caller-owned enumeration in `DeepCostWorkflows.cs` for:

- `RateReferenceGraph` edge snapshots;
- `BuildUpAnalysisService.Analyze` rate snapshots;
- `TradeCostAnalysisService.Analyze` item snapshots;
- `BqLibraryCatalog` construction;
- `BqLibraryCatalog.ImportFromProject` project-entry traversal.

Runtime is `NOT_APPLICABLE`; no licensed BricsCAD evidence is implied.

## Integrity contract

For any source exposing deterministic `Count`, an admitted `Count = N` is a hard semantic boundary. The implementation may call `MoveNext()` to discover whether item `N + 1` exists, but it must reject before observing `IEnumerator.Current` for that unexpected item. The independent streaming ceilings retain the same ordering requirement.

Each affected traversal therefore uses explicit enumeration in this order:

1. `MoveNext()`;
2. known-Count and applicable hard-ceiling checks;
3. `Current`;
4. semantic validation, deduplication and accumulation;
5. final exact traversal / Count-stability validation before returning or publishing results.

Negative, conflicting, oversized, under-yielding or post-traversal drifting Count evidence remains fail-closed. Stable counted inputs and ordinary streaming inputs remain accepted within existing bounds.

## Deterministic regression

`DeepCostKnownCountNoOverreadSmoke` supplies adversarial counted collections that advertise one item and yield two valid objects. It independently asserts all five public boundaries perform two `MoveNext()` calls but only one `Current` read. Stable controls preserve accepted behavior.

Run repository-safe validation with:

```text
python scripts/preflight-deep-cost-known-count-no-overread.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The preflight is auto-discovered by aggregate feature guards and pins explicit guard-before-`Current` ordering without weakening existing deep-cost validation.
