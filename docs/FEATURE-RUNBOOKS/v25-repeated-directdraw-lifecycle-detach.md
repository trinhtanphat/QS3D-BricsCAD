# BricsCAD V25 repeated Direct Draw lifecycle detach

Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring  
Lane-Key: `issue-6321`

## Defect

`DirectDrawRepeatedCommands.RepeatedDocumentLifecycleGuard` subscribes to BricsCAD's process-level `DocumentToBeDeactivated` event so one repeated authoring command remains bound to the exact document from which it started. The old teardown set a terminal `_disposed` flag before calling the native event remove accessor. If that remove accessor threw while BricsCAD was tearing down a document, the exception was contained but the handler could remain registered forever. Because later `Dispose()` calls returned immediately, the event source could retain both the guard and its exact start `Document`.

## Required lifecycle contract

- Capture the exact start `Document`; never fall back to `MdiActiveDocument` inside the lifecycle guard.
- Publish explicit subscription ownership with `_subscribed`.
- Treat constructor subscription as potentially partially successful. If the native add accessor throws, request disposal and make a compensating remove attempt before rethrowing the original construction failure.
- `Dispose()` only requests detach and calls the retryable detach routine. It must not publish terminal ownership loss before the native remove succeeds.
- Clear `_subscribed` only after `DocumentToBeDeactivated -= ...` returns successfully.
- Contain native remove failures and keep ownership published so a retained callback can retry later.
- A callback received after disposal was requested retries detach and returns before comparing against the possibly stale start `Document` wrapper.
- Fence detach reentrancy so a hostile/native remove accessor cannot recursively corrupt subscription state.
- Do not mutate CAD, project state, undo checkpoints, geometry, selection, or command input in the lifecycle helper.

## Deterministic / REMOTE_SAFE qualification

Run:

```text
python scripts/preflight-v25-repeated-directdraw-lifecycle-detach.py
python scripts/preflight-all.py
```

Then require the repository Shared/Hybrid CI policy for the exact PR head, including admitted/trusted BricsCAD V25 references and the V25 plugin build where the workflow reaches those stages.

The source guard verifies explicit subscription/disposal ownership, retry ordering, successful-remove-before-clear semantics, exact-document affinity, and mutation isolation. Remote/static GREEN proves the source contract and compile compatibility only.

## LOCAL_ONLY native qualification

A real licensed BricsCAD V25 session is required to qualify native event-accessor teardown timing. Exercise repeated Wall/Beam Direct Draw from DWG A, request command teardown while A is being deactivated/closed, inject or reproduce one native event-remove failure, and confirm a retained callback retries detach without reading stale A identity. Also verify normal A→B switching still terminates the repeated sequence as `DOCUMENT_SWITCH` and preserves all already checkpointed segments.

Do not record `LOCAL_PASS` unless that licensed runtime scenario was actually executed. Missing local runtime is `LOCAL_ONLY / NO_RESULT`; remote GREEN is not native PASS.

## Self-review checklist

Review exact-document lifetime, constructor add rollback, subscribe/unsubscribe ownership, close/dispose retry, detach reentrancy, stale wrappers, callback ordering, cancellation behavior, command-level undo/rollback ownership, post-commit checkpoint integrity, exception containment/redaction, BricsCAD thread affinity, modeless/UI independence, and V25 compatibility. The carrier must not broaden into MCP runtime/transport, installer/release, or Quantity engine work.