# Audit Log single-instance / veto-safe lifecycle

## Scope

`QS3DAUDIT` is a read-only, native-database-bound modeless workflow. Repeated invocation must not create duplicate Audit Log windows for one native database, and switching drawings must not publish a replacement until the prior owner has reached terminal `Closed`.

## Source-ready contract

- Publication records the exact `AuditLogWindow` plus a stable non-zero BricsCAD native database identity.
- Repeating `QS3DAUDIT` for the same native database reuses/activates the existing window. Managed `Document` wrapper drift does not require replacement because `AuditLogWindow` re-resolves the live wrapper by native database identity.
- Cross-DWG invocation requests close of the existing owner and proceeds only after that window is no longer loaded. Close exceptions or vetoes fail closed and do not publish a second window.
- A stale unloaded publication is cleared defensively.
- The candidate is published only after `Application.ShowModelessWindow` returns successfully and the candidate is still loaded.
- Only the matching window's terminal `Closed` callback may release publication.
- Existing read-only behavior is preserved: opening/reusing Audit Log does not create a project, and audit projection remains bound to the native database owned by the window.

## Hosted validation

Run the repository Shared CI on the exact candidate and require Reservation/Lane-Key/path collision, generic source guard, all discovered feature guards, deterministic Core smoke, trusted V25 reference validation, V25 plugin compilation, and final build to pass.

The focused preflight `scripts/preflight-audit-log-single-instance-veto-safe.py` proves the ordering and rejects the historical close-then-unconditional-recreate source shape.

## LOCAL_ONLY licensed matrix

On one exact source/plugin identity in licensed BricsCAD V25, using disposable drawings only:

1. Open drawing A and invoke `QS3DAUDIT`; prove exactly one Audit Log is visible and bound to A.
2. Invoke again on the exact same managed wrapper; prove activation/reuse and no second window.
3. Exercise a managed-wrapper refresh for the same native database where the host exposes one; prove the same Audit Log remains usable and reload resolves the live wrapper without duplicate publication.
4. With A's Audit Log open, activate drawing B and invoke `QS3DAUDIT`; prove A reaches terminal close before B's Audit Log is shown.
5. Exercise an authorized close-veto/close-failure probe; prove B is not published while A remains non-terminal and a later retry can proceed after A closes normally.
6. Close the owning drawing and verify the existing `DocumentBoundWindowLifetime` closes the Audit Log safely and publication is releasable.
7. Repeat after terminal close to prove a fresh owner can be published exactly once.
8. Verify no QS3D project is created by Audit Log in a drawing that did not already have one, no CAD mutation occurs, and no owned modeless/process residue remains after cleanup.

Hosted/static/compile evidence is never `LOCAL_PASS`; publish only sanitized exact-SHA runtime evidence through the canonical local qualification flow.
