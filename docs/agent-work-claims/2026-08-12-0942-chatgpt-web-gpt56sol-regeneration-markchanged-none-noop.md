# Work claim — Regeneration MarkChanged None no-op

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-markchanged-none-noop-20260812-0942`
- Registered: `2026-08-12T09:42:00+07:00`
- Baseline main SHA: `50d107a57dfb0bb69200389337af30e97ca39d88`
- Priority: P1 — a semantic no-op dirty flag must not dirty dependents or advance project persistence state.
- Task Key: `CORE-REGENERATION-MARKCHANGED-NONE-NOOP`

## Confirmed defect

`ProjectElement.MarkDirty(ElementDirtyFlags.None)` is explicitly a no-op, but `RegenerationEngine.MarkChanged(project, elementId, ElementDirtyFlags.None)` currently rebuilds/resolves the graph, then enters `ProjectSemanticMutationExecutor`, marks every transitive dependent with `Relations | Quantity`, calls `project.Touch()`, and reports a committed mutation. A caller that has no dirty flags can therefore cause unrelated downstream regeneration work and ChangeVersion/UpdatedUtc churn.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationMarkChangedNoneNoopSmoke.cs`
- this claim file

## Intended contract

- Preserve existing project/null/duplicate/dependency graph validation and normal non-zero propagation semantics.
- `ElementDirtyFlags.None` returns without source/dependent dirty mutation, generated-stale mutation, audit/mutation-journal work, ProjectState.Touch(), ChangeVersion changes, or UpdatedUtc changes.
- Undefined dirty bits remain rejected by the existing `ProjectElement.MarkDirty` domain guard; this lane does not change that policy.
- Do not alter subset regeneration, preview/profile, export regeneration, regenerator catalog, native BricsCAD or UI behavior.

## Validation plan

Focused auto-registered Core smoke uses a clean source with a transitive dependent, records source/dependent dirty state and project persistence state, calls `MarkChanged(..., None)`, and requires exact non-mutation. A control call with a real flag proves dependency propagation still occurs. Re-fetch exact source/claim before writes. No force-push, GitHub Actions dispatch, executable smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
