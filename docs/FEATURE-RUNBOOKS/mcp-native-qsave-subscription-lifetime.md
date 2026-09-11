# MCP Native QSAVE Subscription Lifetime

## Scope

This runbook covers `McpNativeCurrentDocumentSave.NativeSaveOperation` terminal-event ownership for the host-owned native `QSAVE` path.

## Safety contract

- Capture and revalidate the exact active `Document` and rooted drawing path before native QSAVE queueing.
- Publish per-handler may-be-subscribed ownership before each fallible BricsCAD `CommandEnded`, `CommandCancelled`, or `CommandFailed` event add.
- On attach failure, attempt exact matching detach; if any remove cannot be proven, retain the operation for later bounded cleanup and block new native saves.
- Clear a handler ownership bit only after its matching native `-=` returns successfully.
- Terminal callbacks are accepted only from the exact saved `Document` and normalized `QSAVE` command.
- A timeout, cancellation, handler-cleanup uncertainty, active-document drift, path drift, or persistent DBMOD content bit must fail closed.
- Never retry or replay native QSAVE automatically after uncertain dispatch or terminal state.
- Hosted/source checks do not substitute for licensed BricsCAD event-accessor fault injection or disposed-wrapper timing.

## Validation

Run `python scripts/preflight-mcp-native-qsave-subscription-lifetime.py`, `python scripts/preflight-mcp-native-qsave-handler-lifetime.py`, MCP production-correctness, capability-lanes, Reservation-v2 and exact-head protected CI.

Licensed V25/V26 partial event add/remove and real MDI timing remain `LOCAL_ONLY / NO_RESULT` unless directly recorded.
