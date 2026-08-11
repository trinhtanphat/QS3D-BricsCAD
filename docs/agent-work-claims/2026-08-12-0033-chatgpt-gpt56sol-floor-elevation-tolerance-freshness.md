# Work claim — Floor elevation tolerance freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-floor-elevation-tolerance-freshness`
- Registered: `2026-08-12T00:33:34+07:00`
- Last Updated: `2026-08-12T00:33:34+07:00`
- Baseline main SHA: `935bab2c0e2224429909a2838a83006cf215d29a`
- Priority: deterministic Core freshness leak found during owner-requested continue-all audit
- Task Key: `CORE-FLOOR-ELEVATION-TOLERANCE-FRESHNESS`

## Confirmed defect

`ProjectFloorService.Update(...)` intentionally treats tiny elevation deltas within `NearlyEqual(...)` as an elevation no-op. However, when a Floor name changes in the same call, the method still executes `floor.ElevationM = elevationM` even when `elevationChanged == false`. Referencing elements are then dirtied only for `Relations | Quantity`, not `Geometry`.

That creates an inconsistent branch: a numerical elevation change small enough to be classified as "no geometry change" is nevertheless persisted whenever a name change accompanies it. The Floor semantic value can therefore change without the geometry dirty path that a real elevation change requires.

## Reserved scope

Make the existing tolerance a true no-op threshold: only assign `FloorDefinition.ElevationM` when `elevationChanged` is true. A name-only update with a sub-tolerance requested elevation must preserve the exact stored elevation value. Preserve all current dirty-flag behavior for real elevation changes and name changes.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- one focused module-registered Core smoke for the tolerance/name-change edge case
- this claim file

## Coordination / exclusions

- The completed Floor/Zone mutation-integrity lane (`a19e4033...`, closeout `b83b3494...`) covered canonical activation/assignment no-ops and null target batches, not elevation tolerance semantics.
- Do not modify Zone service, persistence schema, BricsCAD adapter/UI, geometry planners or existing Floor/Zone smoke/preflight from the completed lane.
- Do not remove or retune the existing `NearlyEqual(...)` tolerance; this lane only makes mutation behavior consistent with that already-chosen threshold.
- No GitHub Actions/build/release dispatch and no LOCAL_ONLY runtime claim.

## Validation plan

- Rename + sub-tolerance elevation request changes the name but preserves the exact stored elevation.
- The same operation does not introduce Geometry dirty solely from the ignored elevation delta.
- A materially different elevation still updates the exact elevation and marks referenced geometry dirty through existing behavior.
- A pure sub-tolerance elevation request with unchanged name remains a complete no-op, preserving project `ChangeVersion`.
- Re-fetch current service after claim publication, review exact PR diff, and read back merged source/commit. Do not claim local smoke execution unless actually run.

## Completion condition

Current `main` no longer applies an elevation value that it simultaneously classified as non-changing for geometry freshness, and focused deterministic regression source is committed with exact evidence.
