# Work claim — Regeneration MarkChanged None no-op

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-markchanged-none-noop-20260812-0942`
- Registered: `2026-08-12T09:42:00+07:00`
- Last Updated: `2026-08-12T09:44:00+07:00`
- Baseline main SHA: `50d107a57dfb0bb69200389337af30e97ca39d88`
- Source fix SHA: `c364c79b0c8d3f6bcc8c3f0b458559cf218195a6`
- Regression SHA: `e5ab99db0654ca04ba52e299c634417f15b3885a`
- Priority: P1 — a semantic no-op dirty flag must not dirty dependents or advance project persistence state.
- Task Key: `CORE-REGENERATION-MARKCHANGED-NONE-NOOP`

## Confirmed defect

`ProjectElement.MarkDirty(ElementDirtyFlags.None)` is explicitly a no-op, but `RegenerationEngine.MarkChanged(project, elementId, ElementDirtyFlags.None)` rebuilt/resolved the graph and then entered `ProjectSemanticMutationExecutor`, marked every transitive dependent with `Relations | Quantity`, called `project.Touch()`, and reported a committed mutation. A caller with no dirty flags could therefore cause unrelated downstream regeneration work and ChangeVersion/UpdatedUtc churn.

## Completed implementation

- Preserve graph rebuild and target resolution before deciding the no-op so malformed project state and unknown target ids still fail as before.
- Return immediately after a valid target resolves when `flags == ElementDirtyFlags.None`.
- Skip dependent collection, semantic mutation executor, generated-stale marking and `ProjectState.Touch()` for the no-op path.
- Preserve the existing non-zero propagation path and existing undefined-bit rejection in `ProjectElement.MarkDirty`.

## Regression evidence

`tests/QS3D.Core.SmokeTests/RegenerationMarkChangedNoneNoopSmoke.cs` is auto-registered and covers:

- exact source/dependent dirty-state and timestamp preservation for `None`;
- exact `ProjectState.ChangeVersion` / `UpdatedUtc` preservation for `None`;
- unknown target rejection remains fail-closed and non-mutating;
- a real `Properties` flag still dirties the source, propagates `Relations | Quantity` to dependents, and advances `ChangeVersion` exactly once.

Source and regression were read back from `main` after their commits.

## Validation boundary

No GitHub Actions were dispatched. No executable full smoke/build or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only session.

## Completion condition

Completed: `MarkChanged(..., None)` is a true semantic no-op after existing validation/target resolution, while real change propagation remains unchanged.
