# V25 Active Family status document affinity

## Scope

C03 / issue #6151 covers `QS3DDRAWACTIVE`, `QS3DDRAWACTIVEADV`, and `QS3DDRAWACTIVEREPEAT` status presentation when the active BricsCAD document changes between command capture and dispatch/failure reporting.

The dispatcher captures one exact source `Document`. `RequireCurrentDispatchSnapshot` must continue to fail closed if `Application.DocumentManager.MdiActiveDocument` is no longer that same instance. Best-effort editor reporting may still target the captured source document, but the process-wide Workspace palette must never publish that source document's stale status after another document becomes active.

## Invariants

- Do not recapture a replacement `MdiActiveDocument` in the catch path.
- Keep `document.Editor.WriteMessage(...)` best-effort and bound to the captured source document.
- Fence `PaletteCoordinator.SetStatus(...)` with exact reference identity against the current `MdiActiveDocument` immediately before publication.
- Keep reporting synchronous and presentation-only: no project creation/bootstrap, `DocumentLock`, CAD transaction, selection mutation, command dispatch, deferred dispatcher work, or retained native wrapper.
- Do not alter Direct Draw transaction/rollback/geometry ownership; target authoring commands remain authoritative.
- Do not surface raw exception details in user-visible status.

## REMOTE_SAFE verification

Run `python scripts/preflight-v25-active-family-status-document-affinity.py`, aggregate feature guards, deterministic smoke, and the admitted-reference BricsCAD V25 compile on the exact candidate SHA. Protected PR `preflight` and `core` must be fresh and GREEN before merge.

## LOCAL_ONLY qualification

Licensed BricsCAD V25 interaction remains separate from hosted/static evidence. A representative native cell is: start an Active Family Quick/Advanced/Repeat workflow in DWG A, switch active MDI document to DWG B before dispatch/failure publication, and verify A's failure/status cannot overwrite B's Workspace state while no unintended geometry/project mutation occurs. Record `LOCAL_PASS` only from an actual licensed runtime execution bound to the exact tested SHA; otherwise report `LOCAL_ONLY / NO_RESULT`.
