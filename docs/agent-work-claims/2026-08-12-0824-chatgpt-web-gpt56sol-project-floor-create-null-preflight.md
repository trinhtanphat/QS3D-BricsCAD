# Work claim — ProjectFloorService.Create null-floor preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-floor-create-null-preflight-20260812-0824`
- Registered: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `4e74d36cbfcec75998cbca55f14fc6a858aea7b1`
- Priority: evidence-driven Core mutation integrity during owner-requested `continue all`

## Confirmed defect

`ProjectFloorService.Create(...)` traversed `project.Floors` directly in the duplicate-id/name preflight. A malformed persisted project whose floor collection contains a null entry could therefore leak a raw `NullReferenceException` before mutation, while the existing floor lookup/mutation contract elsewhere fails closed with `InvalidOperationException("Project floor collection contains a null floor.")`.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs` — `Create(...)` malformed-floor preflight only
- `tests/QS3D.Core.SmokeTests/ProjectFloorCreateNullPreflightSmoke.cs`
- this claim file for close-out

## Integrated changes

- Source fix: `0fd7642ea1e24f7f83a7fbdd114eb8f693c4b8f4` — reject any null floor entry before max-count, duplicate-id/name traversal, `Touch()`, collection mutation, or active-floor mutation.
- Regression smoke: `c026cba9d6e39889b269797ae7e2c2db4c55c086` — assert canonical `InvalidOperationException` and no change to `Floors`, `ActiveFloorId`, `ChangeVersion`, or `UpdatedUtc`.

## Contract preserved

- Established floor-integrity exception contract is now used by Create: `InvalidOperationException("Project floor collection contains a null floor.")`.
- Existing valid Create semantics, duplicate-id/name checks, maximum-floor guard, active-floor behavior, and timestamps remain unchanged.
- Unicode/case identity policy, duplicate existing floor-id corruption semantics, Update/Delete/Assign behavior, UI/BricsCAD runtime, and unrelated floor generation were not changed.

## Validation evidence

- Re-read `ProjectFloorService.Create(...)` from `main` after integration and confirmed the null-floor guard executes before every `project.Floors` dereference that assumes a non-null entry and before mutation.
- Re-read the isolated smoke from `main`; it covers canonical exception text plus mutation/timestamp/version atomicity.
- Smoke project targets `net8.0`; no project configuration edit was required.
- No GitHub Actions/build/release was dispatched from this lane.
- No local .NET execution or BricsCAD V25/V26 runtime PASS is claimed.
