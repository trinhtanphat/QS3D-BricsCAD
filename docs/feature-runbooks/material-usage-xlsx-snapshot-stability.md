# Material Usage XLSX snapshot stability

## Contract

Material Usage XLSX export snapshots caller-owned rows before any filesystem publication. The exporter must preserve the existing single-read outer-row contract while rejecting count-stable mutation of scalar values or provenance after a row has been snapshotted.

## Failure behavior

If any snapshotted scalar changes, or if `ElementIds` / `SourceHandles` count or entry values change before publication, export fails closed with `InvalidOperationException`. No stale workbook may be published.

## Deterministic regression

`MaterialUsageXlsxSnapshotStabilitySmoke` uses a count-stable `IReadOnlyList<MaterialUsageRow>` whose second `Count` observation mutates either `MaterialName` or an `ElementIds` entry. This exercises mutation after the detached snapshot without re-reading the caller-owned row index.

## Source-safe validation

Run:

```text
python scripts/preflight-material-usage-xlsx-snapshot-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj
```

Shared branch CI and protected PR `preflight` / `core` remain authoritative. No BricsCAD, Excel UI, or other LOCAL_ONLY evidence is required for this Core exporter change.
