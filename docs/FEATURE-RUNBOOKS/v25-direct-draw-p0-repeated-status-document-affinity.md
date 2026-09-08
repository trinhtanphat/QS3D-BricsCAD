# V25 Direct Draw P0 / repeated status document affinity

## Scope

C03 / issue #6153 covers process-wide Workspace status publication from P0 Direct Draw (`DirectDrawCommands`) and repeated Wall/Beam authoring (`DirectDrawRepeatedCommands`). Both workflows own an exact source BricsCAD `Document`; a later status from that source must not overwrite the Workspace after another DWG becomes active.

## Production contract

- `DirectDrawCommands.Guard` reports stable operation failure through `DirectDrawUiFailureReporter.ReportOperationFailure(document, operation)`.
- `DirectDrawCommands.FinalizeUi` is already post-commit presentation. Success and warning status route through the shared reporter, so presentation failure cannot change native/semantic commit truth and stale source status cannot smear onto another active DWG.
- `DirectDrawRepeatedCommands.Report` routes arbitrary stable messages through `DirectDrawUiFailureReporter.ReportMessage(document, message)`, preserving best-effort source Editor output while exact-document-fencing the process-wide palette.
- Shared reporting stays synchronous, exception-isolated and presentation-only. It owns no project bootstrap/mutation, document lock, CAD transaction, command dispatch, selection mutation, subscriptions, deferred dispatcher work or retained native wrapper.
- Keep P0 `EnsureActive`, prompt/UCS/model-space freshness, snapshot/rollback and generated-geometry ownership unchanged.
- Keep repeated `DocumentToBeDeactivated` subscribe/unsubscribe, whole-command rollback, transition checkpoints and partial-success semantics unchanged.
- Never surface raw `Exception.Message` at these command boundaries.

## REMOTE_SAFE verification

Run `python scripts/preflight-v25-direct-draw-p0-repeated-status-document-affinity.py`, aggregate feature source guards, deterministic smoke and the admitted-reference BricsCAD V25 compile on the exact candidate SHA. Protected PR `preflight` and `core` must both be fresh and GREEN after final main reconciliation.

## LOCAL_ONLY qualification

Licensed BricsCAD V25 remains a separate native qualification cell. Exercise P0 and repeated Wall/Beam workflows from DWG A, switch active MDI document to B around failure/post-commit/partial-success publication, and verify A cannot overwrite B's Workspace. Also confirm accepted native/semantic geometry, rollback, whole-command undo checkpointing, cancellation and selection behavior remain correct. Record `LOCAL_PASS` only from an actual licensed runtime bound to the exact tested SHA; otherwise report `LOCAL_ONLY / NO_RESULT`.
