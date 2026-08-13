# Work claim — first Zone/Floor create revision canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-zone-floor-first-create-revision-20260813`
- Registered: `2026-08-13T19:59:00+07:00`
- Baseline main SHA: `af9e216176691ffd0d8f6942489ab50f347bf24d`
- Priority: P0 deterministic persisted mutation revision semantics.

## Confirmed defect

`ProjectZoneService.Create()` and `ProjectFloorService.Create()` each called `project.Touch()` before adding the new definition, then auto-activated the first created item by assigning `ActiveZoneId` / `ActiveFloorId`. Those persisted scalar setters call `SetPersistedScalar()`, which increments `ChangeVersion` again. A first logical create therefore advanced the project revision twice while subsequent creates advanced once.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectZoneFloorCreateRevisionSmoke.cs` (focused new regression)
- existing `ProjectZoneServiceSmoke.cs` / `ProjectFloorServiceSmoke.cs` are read-only reference coverage for auto-activation and service behavior
- this claim file for closeout

## Intended bounded change

- keep first-created Zone/Floor auto-activation behavior;
- make one successful create advance `ChangeVersion` exactly once whether or not it also establishes the active id;
- preserve canonical active ids, validation/refusal behavior, subsequent-create semantics, assignment/update/delete behavior and all existing ownership checks;
- add focused regression assertions for first and subsequent create revision deltas.

## Excluded scope

- no changes to general `ProjectState` persisted-scalar semantics;
- no Zone/Floor naming, limits, assignment, deletion, vertical-placement, UI/native BricsCAD or persistence-schema changes;
- no GitHub Actions, packaging or licensed runtime qualification.

## Coordination

- exact commit searches for `zone create ChangeVersion`, `zone double Touch`, `floor create ChangeVersion`, and `first floor active revision` returned no competing lane immediately before claim;
- current `main` immediately before claim was `af9e216176691ffd0d8f6942489ab50f347bf24d` and IFC-01 had already closed;
- existing Zone/Floor smokes verify first-item auto-activation but do not assert revision delta;
- production commits landed as `ec0617d6a350315b8891bc175e54c863149b3e15` (Zone) and `ff09347e5b6400587112f68039b12cfa8c0187fa` (Floor);
- an attempted full replacement of the existing Zone smoke was blocked by the connector safety gate before write; no test commit resulted. Regression scope was therefore amended before creating the focused new smoke.

## Validation plan

Create the focused registered smoke, exact-readback both service blobs and regression, reconcile moving `main`, and close with managed/native execution marked `NOT_RUN` if unavailable.