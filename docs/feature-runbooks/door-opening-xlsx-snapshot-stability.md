# Door/Opening XLSX snapshot stability

## Contract

Door/Opening XLSX export snapshots caller-owned schedule rows before any filesystem publication. The exporter must preserve the existing single-read outer-row contract while rejecting count-stable mutation of row scalar values or provenance after a row has been snapshotted.

The stability check covers all exported scalar fields plus `ElementIds`, `HostIds`, and `SourceHandles`. The detached snapshot remains the only input to workbook generation after validation.

## Failure behavior

If any snapshotted scalar changes, or if any provenance collection changes count or entry values before publication, export fails closed with `InvalidOperationException`. An existing destination workbook must remain untouched because validation happens before the temporary XLSX package is published.

## Deterministic regression

`DoorOpeningXlsxSnapshotStabilitySmoke` uses a count-stable `IReadOnlyList<DoorOpeningScheduleRow>` whose second `Count` observation mutates either `Material`, an `ElementIds` entry, a `HostIds` entry, or a `SourceHandles` entry. This proves the exporter detects post-snapshot mutation without re-reading the caller-owned outer row index and preserves an existing destination on rejection.

## Source-safe validation

Run:

```text
python scripts/preflight-door-opening-xlsx-snapshot-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Shared branch CI and protected PR `preflight` / `core` remain authoritative. No BricsCAD, Excel UI, or other LOCAL_ONLY evidence is required for this Core exporter change.
