# Curtain Wall Hub single-instance / wrapper-safe veto lifecycle

## Scope

`QS3DCURTAIN` opens a document-bound modeless `CurtainWallWindow`. The window intentionally retains its managed BricsCAD `Document` wrapper, attaches `DocumentBoundWindowLifetime`, and uses that exact wrapper for project reads/mutations, active-document checks, editor messages and command dispatch. The launcher must therefore prevent duplicate hubs while distinguishing exact-wrapper reuse from native-database wrapper drift.

This lifecycle package does not change Curtain geometry, panel generation, quantities, family arithmetic or advanced-geometry ownership.

## Source-ready contract

- Publication records the exact `CurtainWallWindow`, exact managed `Document` wrapper, and stable non-zero native database identity.
- Repeating `QS3DCURTAIN` for the exact same native database and exact same managed wrapper reuses/activates the existing hub.
- If the host exposes a different managed wrapper for the same native database, the old wrapper-bound hub is not reused. It must reach terminal close before a replacement is shown.
- Switching to another native database uses the same terminal-close arbitration.
- A close exception or close veto fails closed; no replacement is constructed or published while the prior owner remains loaded.
- A stale unloaded publication is cleared defensively.
- A candidate is published only after `Application.ShowModelessWindow` returns and the candidate is still loaded.
- Only the matching candidate's terminal `Closed` callback may clear the published window/wrapper/native identity.
- Existing `CurtainWallWindow` project binding, `DocumentBoundWindowLifetime`, Family mutation rollback, regeneration and read-only/project-not-created safeguards remain unchanged.

## Hosted validation

Require Shared CI on one exact pushed candidate. Admission must pass Reservation/Lane-Key/path collision, generic source guard and all discovered feature source guards. Core must pass deterministic smoke tests, trusted BricsCAD V25 reference validation, V25 plugin compilation and final build.

The focused guard `scripts/preflight-curtain-wall-hub-single-instance-veto-safe.py` rejects the historical unconditional new-window path and proves exact-owner reuse, wrapper-drift/cross-DWG terminal-close arbitration, stable native identity and candidate publication ordering.

## LOCAL_ONLY licensed matrix

On one exact source/plugin identity in licensed BricsCAD V25, using disposable drawings/projects only:

1. Open drawing A with a disposable QS3D project and invoke `QS3DCURTAIN`; prove exactly one Curtain Wall Hub is visible and bound to A.
2. Invoke again with the exact same managed wrapper; prove the same window activates and no duplicate is created.
3. Exercise an authorized managed-wrapper refresh for A's same native database where the host exposes one; prove the old wrapper-bound hub reaches terminal close before a replacement is published.
4. With A's hub open, activate drawing B and invoke `QS3DCURTAIN`; prove A's owner reaches terminal close before B's hub is shown.
5. Exercise an authorized close-veto/close-failure probe; prove no second hub is published while the old hub remains loaded, then retry after terminal close.
6. In each accepted owner, prove Refresh/Family Save/Recalculate and command dispatch still reject the wrong active drawing and remain bound to the intended source project.
7. Close/destroy the owning drawing and verify `DocumentBoundWindowLifetime` closes the hub and publication can be released.
8. Reopen after terminal close and prove one fresh owner can be published.
9. Verify no owned modeless/process/private-state residue remains after cleanup.

Hosted static/compile evidence is never `LOCAL_PASS`; publish only sanitized exact-SHA/ProductVersion runtime evidence through the canonical local qualification flow.
