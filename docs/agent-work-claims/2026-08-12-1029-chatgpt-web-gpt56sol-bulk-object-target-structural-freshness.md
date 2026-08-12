# Work claim — Bulk object-target structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:29:00+07:00`
- Baseline main SHA: `e8d3b8d72c18bc6ed1b11345396ebdd8ae8bf6a7`
- Priority: P1 — object-based bulk edits must not mutate stale detached element instances after caller enumeration changes project membership without a version bump.
- Task Key: `CORE-BULK-OBJECT-TARGET-STRUCTURAL-FRESHNESS`

## Confirmed defect

`BulkEditService.SetProperty(ProjectState, IEnumerable<ProjectElement>, ...)` and `MultiplyNumericProperty(...)` call `OwnedDistinct(...)`. That helper snapshots the project's current element-id → instance map before enumerating caller-provided `elements`. A lazy enumerable can yield a canonical element, then remove or replace that element in the public `project.Elements` collection without calling `project.Touch()`. `OwnedDistinct(...)` still accepts the previously snapshotted instance; the existing post-enumeration `ChangeVersion` check cannot detect the structural change, so the operation can mutate a detached stale element and then advance the canonical project's version.

This differs from current Family/Zone assignment freshness contracts, which revalidate exact project ownership after caller enumeration.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs` — object-target structural freshness for `SetProperty(...)` and `MultiplyNumericProperty(...)`
- one focused Core smoke/regression for remove/replace during lazy object enumeration plus stable controls
- this claim file

## Intended contract

- preserve target count/null/id/duplicate checks and `ChangeVersion` freshness checks;
- after object-target enumeration, require every resolved element to still be the exact canonical project-owned instance before update planning/mutation;
- reject remove/replace-with-same-ID structural changes before service-owned property/timestamp/project mutation;
- preserve caller-side structural side effects, stable SetProperty/Multiply behavior, numeric parse/overflow semantics and `ProjectSemanticMutationExecutor` rollback behavior;
- do not modify bulk Family assignment, LOCAL-003 fixtures, selection/UI/native BricsCAD or persistence schema.

## Validation boundary

Source and focused regression will be committed/read back from `main`. No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.
