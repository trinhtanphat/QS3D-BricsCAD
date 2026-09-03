# Project catalog persistence freshness

## Scope

This REMOTE_SAFE Core contract keeps persisted Zone, Floor, Family scalar edits, and Family property-map edits visible to project-level freshness consumers. It does not change ProjectElement relation dirty tracking, native BricsCAD behavior, collection add/remove semantics, or licensed-runtime acceptance.

## Contract

An already-owned `ZoneDefinition`, `FloorDefinition`, or `ProjectFamily` requests one owning `ProjectState.Touch()` before a real persisted scalar change is committed. `ProjectFamily.Properties` keeps its public `IDictionary<string,string>` surface and `OrdinalIgnoreCase` key identity, but real Add/indexer/Remove/Clear mutations request the same owning freshness before the backing map changes. Identical replacements, missing removes, and empty clears are persistence no-ops; a non-empty clear is one logical mutation.

Project catalog materialization (`Add`) only attaches ownership and does not itself change the project persistence stamp. Ownership follows the public `IList` lifecycle: remove/clear/replacement detaches the old object, insertion/addition attaches the new object, and duplicate object references keep exactly one owning subscription until the last reference is removed. Detached snapshot copies own their own catalog listeners and cannot advance the source project's freshness.

The mutation request occurs after input/no-op determination but before persisted state changes, so `ChangeVersion` overflow fails closed before partially mutating the catalog record or Family property map. Existing `ProjectFamily.PropertyChanged` behavior for Name/Category remains preserved after successful scalar assignment; direct property-map edits do not synthesize unrelated `PropertyChanged` events.

Service APIs preserve one logical revision per successful logical edit. `ProjectFamilyService.Rename` and `ProjectZoneService.Update` rely on the owned scalar setter rather than pre-touching. `ProjectFloorService.Update` batches a simultaneous name/elevation change behind one internal persistence mutation request while retaining elevation tolerance, vertical-reference validation, dirty propagation, and no-op behavior. `ProjectFamilyService.SetProperty` and `RemoveProperty` rely on the owned property store instead of pre-touching. `ProjectFamilyService.Duplicate` validates and copies properties while the clone is detached, then touches the project once and attaches the fully initialized clone.

Snapshot restore remains compatible with the ownership contract: `ProjectStateSnapshot.CopyInto` detaches the current Family collection before copying preserved Family state and reattaches restored objects afterward. Snapshot exception-safety/non-notifying restore is a separate persistence concern and must not be conflated with this freshness carrier.

## Deterministic validation

Run:

```text
python scripts/preflight-project-catalog-persistence-freshness.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused smoke covers Zone name, Floor name/elevation, Family name/category, direct Family property Add/replace/remove/clear, case-insensitive property identity, exact no-ops, remove/replacement ownership, duplicate-reference ownership, snapshot restore identity/stamp restoration, detached-copy isolation, and exactly-one revision semantics for Family rename/property services/duplicate, Zone update, and Floor name/elevation/combined updates.

## Runtime boundary

No licensed BricsCAD result is required or claimed. Protected Shared CI `preflight` + `core` is authoritative for this deterministic Core-only package.
