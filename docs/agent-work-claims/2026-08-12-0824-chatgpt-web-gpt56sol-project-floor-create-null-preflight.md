# Work claim — ProjectFloorService.Create null-floor preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-floor-create-null-preflight-20260812-0824`
- Registered: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `4e74d36cbfcec75998cbca55f14fc6a858aea7b1`
- Priority: evidence-driven Core mutation integrity during owner-requested `continue all`

## Confirmed defect

`ProjectFloorService.Create(...)` traverses `project.Floors` directly in the duplicate-id/name preflight. A malformed persisted project whose floor collection contains a null entry can therefore leak a raw `NullReferenceException` before mutation, while the existing floor lookup/mutation contract elsewhere fails closed with `InvalidOperationException("Project floor collection contains a null floor.")`.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs` — `Create(...)` malformed-floor preflight only
- one isolated Core smoke file for this Create lifecycle
- this claim file for close-out

## Contract

- Reject a null floor entry before any duplicate-id/name traversal or project mutation.
- Use the established floor-integrity exception contract: `InvalidOperationException("Project floor collection contains a null floor.")`.
- Preserve existing valid Create semantics, duplicate-id/name checks, maximum-floor guard, active-floor behavior, and timestamps.
- Exclude Unicode/case identity policy, duplicate existing floor-id corruption semantics, Update/Delete/Assign behavior, UI/BricsCAD runtime, and unrelated floor generation.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source/test are re-read, and this claim is marked `COMPLETED` with exact integration SHAs/evidence.
