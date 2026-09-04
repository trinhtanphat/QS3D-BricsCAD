# MCP native QSAVE terminal publication

## Scope

This runbook covers the source-side terminal publication contract for `McpNativeCurrentDocumentSave.NativeSaveOperation`.

## Failure mode

A matching BricsCAD `QSAVE` terminal event is one-shot. Once `_terminalSet` is won, optional diagnostics must not be able to prevent the waiter from observing that terminal state. Historically the event handler invoked the audit sink before `Done.Set()`. An audit exception could therefore escape the native event boundary and strand the waiter until the 30-second uncertain-save timeout even though the native command had already ended/cancelled/failed.

## Required source contract

- `_terminalSet` remains the exactly-once terminal winner.
- `TerminalError` is published before completion signalling.
- terminal handler detach is attempted without weakening the per-handler ownership/fail-closed cleanup contract.
- the optional terminal audit callback is fail-soft and cannot escape the native event handler.
- `Done.Set()` is finally-bound after the terminal winner is established, so diagnostic failure cannot suppress terminal publication.
- no automatic `QSAVE` replay or retry is introduced.
- existing current-document/path affinity and DBMOD completion verification remain authoritative.

## Deterministic validation

Run:

```powershell
python scripts/preflight-mcp-native-qsave-terminal-publication.py
```

The repository's auto-discovered preflight also runs this guard. Protected exact-head `preflight` and `core` must both succeed before merge.

## Runtime boundary

Source/static and trusted-reference V25 compilation are REMOTE_SAFE. Deliberately injecting a throwing audit sink into real BricsCAD command terminal events requires a licensed local host and remains LOCAL_ONLY / NO_RESULT unless actually executed. Remote evidence must not be reported as `LOCAL_PASS`.
