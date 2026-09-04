# MCP diagnostic idle-poll responsiveness

## Scope

This runbook covers command-monitor attachment in `McpDiagnosticHub` and the interaction between its bounded diagnostics timer and BricsCAD application-context dispatch.

## Root cause

`McpDiagnosticHub.Poll()` runs every 1000 ms to observe bounded MCP transport/OAuth state. Before this fix the timer also called `QueueAttachActiveDocument()` on every tick. That helper always scheduled `Application.DocumentManager.ExecuteInApplicationContext(...)`, even when the active document was already present in the idempotent `Subscriptions` dictionary.

The duplicate attach itself was harmless, but the application-context hop was not free: an otherwise-idle diagnostics timer continuously woke/serialized BricsCAD's CAD context while users were editing or idling. This produced avoidable host contention and could amplify the latency of real MCP CAD work.

## Required behavior

- `Start()` subscribes once to `Application.DocumentManager.DocumentBecameCurrent` and performs one initial `QueueAttachActiveDocument()` for the document already current at startup.
- `Poll()` retains the existing 750 ms initial delay / 1000 ms cadence and continues observing `LastError` plus `LastOAuthMcpActivityUtc`, but it never queues CAD-context attachment work.
- `OnDocumentBecameCurrent` queues attachment only when the host reports a document switch/current-document lifecycle event.
- `Attach()` remains authoritative and idempotent through the existing `Subscriptions.ContainsKey(document)` check.
- `Stop()` clears document subscriptions and unsubscribes `DocumentBecameCurrent` fail-soft before the diagnostics bridge is considered stopped.
- This change does not alter the CAD mutation writer, native-command ownership, timeout-after-start safety, or no-auto-retry semantics in `McpCadAgentRuntime` / `McpCadMutationCoordinator`.

## Deterministic validation

Run:

```powershell
python scripts/preflight-mcp-diagnostic-idle-poll.py
```

The regression guard requires the 1-second `Poll()` body to contain no `QueueAttachActiveDocument()` call, preserves the initial startup attachment, requires matching `DocumentBecameCurrent` subscribe/unsubscribe lifecycle, and keeps the bounded transport/OAuth polling contract.

Protected exact-head `preflight` and `core` must both be successful before merge.

## Runtime boundary

Source/static validation and trusted-reference V25 compilation are REMOTE_SAFE. Measuring BricsCAD UI responsiveness and host scheduling under a licensed interactive V25 session remains LOCAL_ONLY / NO_RESULT unless that exact candidate is exercised locally. Remote CI must not be reported as a licensed runtime PASS.
