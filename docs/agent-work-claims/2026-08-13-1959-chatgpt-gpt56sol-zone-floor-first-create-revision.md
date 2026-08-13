# Work claim — first Zone/Floor create revision canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-zone-floor-first-create-revision-20260813`
- Registered: `2026-08-13T19:59:00+07:00`
- Baseline main SHA: `af9e216176691ffd0d8f6942489ab50f347bf24d`
- Priority: P0 deterministic persisted mutation revision semantics.

## Confirmed defect

`ProjectZoneService.Create()` and `ProjectFloorService.Create()` each call `project.Touch()` before adding the new definition, then auto-activate the first created item by assigning `ActiveZoneId` / `ActiveFloorId`. Those persisted scalar setters call `SetPersistedScalar()`, which increments `ChangeVersion` again. A first logical create therefore advances the project revision twice while subsequent creates advance once.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectZoneServiceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFloorServiceSmoke.cs`
- this claim file for closeout

## Intended bounded change

- keep first-created Zone/Floor auto-activation behavior;
- make one successful create advance `ChangeVersion` exactly once whether or not it also establishes the active id;
- preserve canonical active ids, validation/refusal behavior, subsequent-create semantics, assignment/update/delete behavior and all existing ownership checks;
- add focused regression assertions to the existing service smokes for first and subsequent create revision deltas.

## Excluded scope

- no changes to general `ProjectState` persisted-scalar semantics;
- no Zone/Floor naming, limits, assignment, deletion, vertical-placement, UI/native BricsCAD or persistence-schema changes;
- no GitHub Actions, packaging or licensed runtime qualification.

## Coordination

- exact commit searches for `zone create ChangeVersion`, `zone double Touch`, `floor create ChangeVersion`, and `first floor active revision` returned no competing lane immediately before claim;
- current `main` immediately before claim was `af9e216176691ffd0d8f6942489ab50f347bf24d` and IFC-01 had already closed;
- existing Zone/Floor smokes verify first-item auto-activation but do not assert revision delta.

## Validation plan

Refresh `main` after claim, verify reserved files were not touched concurrently, patch the two Create paths symmetrically, extend existing smokes, exact-readback source/test blobs and close with managed/native execution marked `NOT_RUN` if unavailable.