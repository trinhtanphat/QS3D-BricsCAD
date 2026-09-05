# Curtain Wall schedule generation fence

## Scope

`CurtainWallScheduleBuilder.Build` is a deterministic managed-Core reporting boundary. It must never publish a row assembled from multiple project generations.

## Failure mode

The schedule historically captured Floor and Family dictionaries, then enumerated live `project.Elements`. A concurrent or re-entrant project mutation could therefore combine catalog identity from one state with Curtain Wall quantities/provenance from another state. `ProjectState.Elements` also permits element replacement without incrementing `ProjectState.ChangeVersion`, and element quantity mutation has its own `ProjectElement.UpdatedUtc` revision evidence.

## Contract

At admission, the builder freezes:

- `ProjectState.ChangeVersion`;
- exact Element instances plus their `UpdatedUtc` values;
- exact Floor instances;
- exact Family instances;
- DrawingFingerprint.

The builder enumerates only the frozen Element snapshot. It revalidates the frozen generation before/during element aggregation and again before materialization/publication. Any project/catalog/element drift fails closed with a recompute diagnostic.

This fence does not change Curtain Wall grouping, compensated numeric aggregation, count arithmetic, clear-panel envelope validation, source-handle provenance, or stable ordering.

## Deterministic validation

`CurtainWallScheduleGenerationFenceSmoke` covers:

1. stable-generation schedule output remains accepted;
2. direct Element replacement is rejected even though that mutation does not advance `ProjectState.ChangeVersion`;
3. in-place Curtain quantity mutation is rejected through element revision evidence;
4. catalog generation mutation is rejected.

`preflight-curtain-wall-schedule-generation-fence.py` pins the source ordering: frozen snapshots first, frozen Element enumeration, repeated generation checks, and fail-closed publication.

## Runtime boundary

`REMOTE_SAFE / NOT_APPLICABLE` for licensed BricsCAD runtime. Hosted deterministic Core smoke and protected source/preflight checks are authoritative for this package. Do not report a licensed `LOCAL_PASS` from this runbook.