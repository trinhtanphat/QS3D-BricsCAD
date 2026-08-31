# Project interchange element-array bound

## Scope

This Core-only contract protects `QS3D.SemanticSnapshot` export from unbounded per-element materialization of drawing-local `sourceHandles` and semantic `dependencies`.

## Contract

- `ProjectInterchangeJsonExporter.MaxElementStringArrayItems` is 4096.
- The same ceiling applies to both `ProjectElement.SourceHandles` and `ProjectElement.DependsOn` through the shared `AppendStringArray` path.
- The first item beyond the ceiling fails closed before it is validated, retained, sorted, or serialized.
- Existing rejection of empty, padded, and case-insensitive duplicate values is preserved.
- Accepted arrays retain deterministic `OrdinalIgnoreCase` sorting.
- Existing project-level collection limits, semantic-reference validation, strict UTF-16 handling, canonical snapshot validation, and atomic file publication remain authoritative.

## Deterministic validation

Run:

```text
python scripts/preflight-project-interchange-element-array-bound.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

`ProjectInterchangeElementArrayBoundSmoke` proves exact-limit source handles remain exportable, the first over-limit source handle is rejected, and ordinary source-handle/dependency serialization remains stable.

## Runtime boundary

`NOT_APPLICABLE`: this is deterministic Core serialization/export correctness. Hosted protected Core/static evidence is authoritative; no licensed BricsCAD `LOCAL_PASS` is required or implied.
