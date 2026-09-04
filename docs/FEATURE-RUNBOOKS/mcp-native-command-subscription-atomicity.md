# MCP native-command event subscription atomicity

## Safety contract

`McpCadMutationCoordinator` must never reopen process-global DWG writer admission while BricsCAD command-event handlers from a failed native-command arm may still be attached.

The four `Document` subscriptions (`CommandWillStart`, `CommandEnded`, `CommandCancelled`, `CommandFailed`) are an all-or-nothing publication unit. `_pending` is published normally only after every add succeeds. If any add throws, cleanup is attempted exactly once; there is no blind retry of host event registration.

If every unsubscribe succeeds, the original host exception propagates and outer writer admission may unwind normally. If any unsubscribe cannot be proven, the candidate is retained in `_pending` as a fail-closed quarantine. Subsequent mutations, native commands, interactive MCP UI, and writer-lease acquisition remain rejected until a later cleanup attempt succeeds or the plugin process is restarted.

`Reset()` and an uncommitted `NativeCommandReservation.Dispose()` may clear `_pending` only after `TryDetachPendingLocked` reports success. A matching native terminal event still synchronizes the mutation ACK because the CAD command may really have completed, but terminal cleanup failure keeps writer quarantine active.

## Validation

Run:

```text
python scripts/preflight-mcp-native-command-subscription-atomicity.py
```

The guard verifies ordering, rollback/rethrow, cleanup-result propagation, reset/dispose quarantine preservation, and absence of native subscription retry loops. It is auto-discovered by `scripts/preflight-all.py`.

## Runtime classification

The source guard and V25 compilation are REMOTE_SAFE. Injecting BricsCAD event-add/event-remove failures requires a licensed host and is LOCAL_ONLY; do not claim `LOCAL_PASS` without that execution evidence.
