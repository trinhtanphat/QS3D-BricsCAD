# Door/Opening schedule semantic generation fence

## Problem

`DoorOpeningScheduleBuilder.Build` previously froze Floor/Family lookup dictionaries but continued to enumerate and read live mutable project state. Direct element replacement and some in-place Element/Family/provenance mutations can occur without `ProjectState.ChangeVersion` providing sufficient generation evidence. A schedule could therefore publish grouping, quantities, host identity, catalog text, provenance, or project identity assembled from different project generations.

## Contract

The builder captures an immutable semantic snapshot before aggregation. The snapshot contains project identity/fingerprint, Floor id/name, Family id/name/category plus schedule-relevant properties, Element identity/category/relations plus schedule-relevant properties, stored OpeningArea, and copied source handles. Host validation resolves against the frozen element set.

Rows are constructed only from frozen values. The live project is revalidated against the snapshot before aggregation, during each row aggregation, after aggregation, and before publication. Any drift fails closed with a stable recompute diagnostic.

The generation fence is intentionally stronger than `ProjectState.ChangeVersion`: direct list replacement, in-place quantity/property/catalog/provenance changes, host category drift, project identity/fingerprint drift, or list/order/identity drift are rejected even if the project version does not help.

## Preserved behavior

The change preserves Door/WallOpening grouping/order, Family/category checks, default dimensions, OpeningUsage-to-Window semantics, canonical HostWallId validation, compensated OpeningArea aggregation, checked Count, distinct HostCount, ElementIds/HostIds/SourceHandles provenance, and existing overflow/non-finite/underflow fail-closed behavior.

## Validation

Run:

```text
python scripts/preflight-door-opening-schedule-generation-fence.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Protected PR `preflight` and `core` must both succeed on the exact candidate head before merge. No licensed BricsCAD runtime evidence is required for this managed Core reporting correction.
