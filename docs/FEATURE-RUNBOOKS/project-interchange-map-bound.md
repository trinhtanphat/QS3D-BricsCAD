# Project interchange map materialization bound

## Scope

This Core-only contract prevents unbounded pre-validation materialization of portable Family/Element property maps and Element quantity maps while building `QS3D.SemanticSnapshot` JSON.

## Contract

- `ProjectInterchangeJsonExporter.MaxInterchangeMapItems` is 4096.
- The ceiling is shared by portable Family properties, portable Element properties, and Element quantities.
- String-map filtering happens before the ceiling is consumed, so non-portable/generated properties do not consume portable interchange capacity.
- The first portable string-map member beyond the ceiling fails before retention and sorting.
- Quantity dictionaries are rejected by `Count` before `OrderBy(...).ToList()` materialization when over-bound.
- Existing canonical key/value validation, finite-number checks, deterministic case-insensitive key ordering, semantic-reference validation, per-element string-array bound, top-level collection bounds, strict UTF-16 handling, canonical snapshot validation, and atomic publication remain authoritative.

## Deterministic validation

Run:

```text
python scripts/preflight-project-interchange-map-bound.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

`ProjectInterchangeMapBoundSmoke` proves exact-limit portable properties remain exportable, the first portable property above the limit fails closed, and normal property/quantity serialization remains stable.

## Runtime boundary

`NOT_APPLICABLE`: this is deterministic Core serialization/export correctness. Hosted protected Core/static evidence is authoritative; no licensed BricsCAD `LOCAL_PASS` is required or implied.
