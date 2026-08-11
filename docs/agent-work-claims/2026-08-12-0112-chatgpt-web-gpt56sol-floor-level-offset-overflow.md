# Work claim — floor/level assignment offset overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-floor-level-offset-overflow`
- Registered: `2026-08-12T01:12:00+07:00`
- Baseline main SHA: `4ba60c002c51d1d154e3cd8f49e4c8d88a657527`
- Priority: deterministic CAD-independent numeric preflight defect found during owner-requested continue-all audit

## Confirmed defect

`ProjectFloorService.AssignBottomLevel(...)` and `AssignTopLevel(...)` validate each floor elevation and configured level offset as finite, but compare effective elevations with raw `ElevationM + Offset` arithmetic. Two individually finite doubles can overflow to `+Infinity` / `-Infinity`. The assignment preflight can therefore accept and persist a Bottom/Top level relation whose effective elevation is non-finite, while `ElementVerticalPlacementService.Resolve(...)` later fails closed on the same configuration through its guarded `Add(...)` helper.

This is a validate-before-mutate inconsistency: invalid vertical placement can be admitted by the authoring service and rejected only downstream.

## Reserved scope

- Replace raw level-elevation + offset preflight arithmetic in `AssignBottomLevel` / `AssignTopLevel` with one finite-add helper local to `ProjectFloorService`.
- Throw before `project.Touch()` or element mutation if either effective elevation overflows/non-finite.
- Preserve all existing ordering (`top > bottom`), ownership, no-op, dirty/stale and relation semantics.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFloorLevelOffsetOverflowSmoke.cs`
- module-initializer registration in the new smoke file
- this claim file

## Excluded scope

- No `ElementVerticalPlacementService`, FloorDefinition, Level UI/native/V25/V26, persistence schema or engineering policy changes.
- No new bounds on otherwise finite elevations/offsets; only arithmetic closure is enforced.
- No GitHub Actions dispatch.

## Validation plan

- AssignBottomLevel rejects top effective elevation overflow before touching project/element.
- AssignBottomLevel rejects candidate bottom effective elevation overflow before mutation.
- AssignTopLevel rejects existing bottom effective elevation overflow before mutation.
- AssignTopLevel rejects candidate top effective elevation overflow before mutation.
- Valid finite effective elevations continue assigning and marking relation/geometry/quantity dirty as before.
- Failure preserves relation properties, project ChangeVersion/UpdatedUtc and element UpdatedUtc/Dirty.
- Inspect exact implementation diff and re-fetch source/test from moving `main` before close-out.

## Coordination

Recent vertical placement work already hardened `ElementVerticalPlacement`/resolver finite height arithmetic, but recent commit search found no active/recent claim for overflow in `ProjectFloorService` assignment preflight arithmetic. Current unrelated agent work is on materials, release/V26, polygon, documentation and other surfaces.

## Completion condition

Current `main` rejects non-finite effective level elevations before Bottom/Top assignment mutation, valid behavior remains unchanged, focused deterministic smoke coverage is present, and this claim is closed `COMPLETED`.
