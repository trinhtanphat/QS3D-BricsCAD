# BricsCAD V25 SelectionSync retryable native detach

Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring  
Lane-Key: `issue-6331`

## Defect

`SelectionSyncCoordinator` owns a generation-specific `ImpliedSelectionChanged` handler for each attached BricsCAD `Document`. Previously `Detach` and attach rollback revoked the active generation dictionaries and then attempted native `-=` best-effort. If the BricsCAD remove accessor threw during document teardown, the managed coordinator forgot the exact handler while the native event source could still retain the delegate closure, exact `Document`, and generation token indefinitely.

The stale generation was behavior-fenced, so the primary risk was lifetime retention rather than stale UI publication. The missing piece was durable native subscription ownership and a retry path.

## Required lifecycle contract

- Keep active modeless/UI generation ownership separate from native subscription ownership.
- Key native subscription records by the exact generation token, not by `Document`, so a failed old detach cannot be overwritten by a later reattachment of the same native wrapper.
- Publish conservative `MayBeSubscribed` ownership before entering the native add accessor because the accessor may fail after partial registration.
- Revoke active generation authority before attempting detach, including pending timer/refresh authority.
- `RequestDetach` marks only the exact generation and enters a retry-safe native detach routine.
- Clear `MayBeSubscribed` and forget the native subscription record only after native `-=` returns successfully.
- Contain native remove failures and retain the exact record for a later retry.
- A retained callback whose generation has `DetachRequested` retries detach and returns before active-document/UI checks; it cannot resurrect stale selection state.
- Fence native remove reentrancy so a callback pumped by the remove accessor cannot recursively corrupt ownership.
- `Stop()` detaches all active generations and retries pending native records, but does not lie by clearing records whose remove is still rejected.
- Preserve exact-document affinity. Never replace the captured document with `MdiActiveDocument` for native ownership.
- Do not add CAD transactions, document locks, project mutation, command execution, geometry mutation, or undo ownership to SelectionSync teardown.

## REMOTE_SAFE deterministic qualification

Run the source guards on the exact candidate:

```text
python scripts/preflight-v25-selection-sync-retryable-detach.py
python scripts/preflight-all.py
```

Then require the protected Shared/Hybrid CI for the exact PR head. For a build-relevant candidate the authoritative Shared run must reach deterministic smoke, trusted/admitted BricsCAD V25 reference acquisition/validation, and the V25 plugin compile.

Remote/static GREEN proves source-contract and compile compatibility only.

## LOCAL_ONLY native qualification

A licensed BricsCAD V25 session is required to qualify native event-accessor behavior. Exercise at least:

1. attach SelectionSync to DWG A and produce implied-selection events;
2. request A teardown while native `ImpliedSelectionChanged -= handler` is made to fail once;
3. confirm A's active generation is immediately revoked and no stale modeless inspection/status is published;
4. trigger a retained callback or later lifecycle retry and confirm the exact old handler detaches successfully;
5. reattach the same native document wrapper/new generation between failed and successful old detach and confirm only the new generation may publish UI;
6. close/switch A→B and confirm B remains authoritative;
7. stop/unload and confirm timer/refresh cleanup and best-effort native detach do not throw outward.

Do not record `LOCAL_PASS` unless those native scenarios were actually executed in a real licensed V25 runtime. Missing licensed runtime is `LOCAL_ONLY / NO_RESULT`.

## Self-review checklist

Review exact `Document`/generation affinity, add/remove partial-failure ownership, subscribe/unsubscribe ordering, duplicate native handler containment, detach retry and reentrancy, document close/dispose, `Stop()` behavior, pending `DispatcherTimer` cancellation, refresh token ownership, A→B switching, stale wrappers, exception containment/redaction, UI-thread/modeless publication, post-commit independence, CAD transaction/undo ownership, geometry isolation, and V25 compatibility.