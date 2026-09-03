# Project catalog persistence freshness

## Scope

This REMOTE_SAFE Core contract keeps persisted Zone, Floor, and Family scalar edits visible to project-level freshness consumers. It does not change ProjectElement relation dirty tracking, native BricsCAD behavior, collection add/remove semantics, or licensed-runtime acceptance.

## Contract

An already-owned `ZoneDefinition`, `FloorDefinition`, or `ProjectFamily` requests one owning `ProjectState.Touch()` before a real persisted scalar change is committed. Canonicalized/no-op assignments do not request a touch. Project catalog materialization (`Add`) only attaches ownership and does not itself change the project persistence stamp.

Ownership follows the public `IList` lifecycle: remove/clear/replacement detaches the old object, insertion/addition attaches the new object, and snapshot restore reattaches the preserved captured catalog objects. Detached snapshot copies own their own catalog listeners and cannot advance the source project's freshness.

The mutation request occurs after value validation and before scalar assignment so `ChangeVersion` overflow fails closed before partially mutating the catalog record. Existing `ProjectFamily.PropertyChanged` behavior is preserved after successful assignment.

Service APIs preserve one logical revision per successful logical edit. `ProjectFamilyService.Rename` and `ProjectZoneService.Update` rely on the owned scalar setter rather than pre-touching. `ProjectFloorService.Update` batches a simultaneous name/elevation change behind one internal persistence mutation request, while retaining the existing elevation tolerance, vertical-reference validation, dirty propagation and no-op behavior.

## Deterministic validation

Run:

```text
python scripts/preflight-project-catalog-persistence-freshness.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused smoke covers Zone name, Floor name/elevation, Family name/category, normalized no-ops, remove/replacement ownership, snapshot restore identity/stamp restoration, detached-copy isolation, and exactly-one revision semantics for Family rename, Zone update, and Floor name/elevation/combined updates.

## Runtime boundary

No licensed BricsCAD result is required or claimed. Protected Shared CI `preflight` + `core` is authoritative for this deterministic Core-only package.
