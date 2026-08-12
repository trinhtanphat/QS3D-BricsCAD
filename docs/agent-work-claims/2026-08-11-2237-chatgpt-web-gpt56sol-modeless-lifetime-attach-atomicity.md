# Work claim — document-bound modeless lifetime attachment atomicity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-modeless-lifetime-attach-atomicity`
- Registered: `2026-08-11T22:37:00+07:00`
- Completed: `2026-08-11T22:40:00+07:00`
- Baseline main SHA: `b07b02da76168b6f32231a3a4ccef1f8bdda66a2`
- Priority: P1 deterministic event-ownership hardening found during owner-requested `continue all` audit.

## Result

The partial-subscription retry defect in `DocumentBoundWindowLifetime.Registration.Attach(...)` is closed on `main`.

- `8d474795d98d49d61cf792a565917d9e891de76f` — `fix(ui): roll back partial modeless lifetime attach`
  - all existing event subscriptions remain in the same order and successful attach still sets `_attached = true` only after the full set is installed;
  - failure temporarily marks the partial attempt attached and routes rollback through the existing best-effort `Detach()` implementation, which removes every possibly-added manager/window handler without letting one remove failure block the rest;
  - rollback then clears `_projectAffinityBound` and `_projectId`, so a later retry binds against the then-current canonical project instead of inheriting a failed-attempt snapshot;
  - the original attachment exception is rethrown.
- `68a2ebf73aec03094f560b0f712be0eb768ebeb1` — `test(ui): guard modeless lifetime attach atomicity`
  - focused auto-discovered source gate requires the ordered attach/rollback contract, project-affinity reset, existing best-effort detach handlers, same-window idempotence, cross-document rebind rejection and source-DWG/project fail-closed behavior.

## Integration verification

The implementation commit diff was re-read and touches only `DocumentBoundWindowLifetime.cs`. Compare from `68a2ebf7...` to later `main` reported `behind_by: 0` with the regression commit as merge base; subsequent commits touched unrelated Family/Grid/Updater claim surfaces. No reset, rebase or force-push was used.

## Validation boundary

The focused source gate is committed but was not executed in a full repository checkout in this connector-only lane. No GitHub Actions, BricsCAD V25 event-add failure injection, build/NETLOAD, installer, signing or release was run. Native event-subscription failure/retry remains local-only; no `LOCAL_PASS` is claimed.

## Coordination

No modeless call sites, dynamic hubs, XAML/presentation, ProjectContext semantics, Ribbon, updater, Core or LOCAL inbox files were modified.
