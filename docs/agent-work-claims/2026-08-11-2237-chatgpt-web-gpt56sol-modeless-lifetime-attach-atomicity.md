# Work claim — document-bound modeless lifetime attachment atomicity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-modeless-lifetime-attach-atomicity`
- Registered: `2026-08-11T22:37:00+07:00`
- Baseline main SHA: `b07b02da76168b6f32231a3a4ccef1f8bdda66a2`
- Priority: P1 deterministic event-ownership hardening found during owner-requested `continue all` audit.

## Confirmed defect

`DocumentBoundWindowLifetime.Registration.Attach(...)` subscribes `DocumentToBeDestroyed`, `Activated`, `PreviewMouseDown`, `PreviewKeyDown` and `Closed` sequentially, but marks `_attached = true` only after the final subscription. If any later event subscription throws, earlier handlers remain subscribed while `_attached` stays false. A retry can then subscribe those earlier handlers again, and ordinary `Detach()` cannot clean the failed attempt because it exits when `_attached` is false.

The same failed attempt can also retain a project-affinity snapshot captured before subscription failure, so a retry may inherit stale affinity instead of binding to the then-current project.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/DocumentBoundWindowLifetime.cs`
- `scripts/preflight-document-bound-window-attach-atomicity.py` (new)
- this claim file for close-out

## Intended contract

- Attach either completes all event subscriptions and owns the registration, or removes every possibly-added handler before rethrowing.
- Failed attach resets project-affinity state so retry binds to current canonical project state.
- Existing same-window idempotence, cross-document rebind rejection, source-DWG close behavior and project-change fail-closed behavior remain unchanged.

## Excluded scope

- No modeless window call-site edits, dynamic hub behavior, presentation/XAML, ProjectContext semantics, Ribbon/updater/Core, installer/signing/release or LOCAL inbox changes.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

Re-fetch current source before writing. Reuse the existing best-effort `Detach()` path for rollback without changing normal close semantics, add a focused auto-discovered source preflight for failure cleanup/retry, inspect exact diff, and verify ancestry on moving `main` without force-push.

## Completion condition

Document-bound modeless attachment becomes retry-safe/fail-atomic at source level with focused regression guard merged on `main`; native event-add failure injection remains local-only.
