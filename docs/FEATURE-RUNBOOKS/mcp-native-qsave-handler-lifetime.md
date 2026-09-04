# MCP native QSAVE handler lifetime

Carrier: #5610 / Lane-Key `issue-5610`.

## Source defect
`McpNativeCurrentDocumentSave.NativeSaveOperation` previously represented three native BricsCAD command-event subscriptions with one `_handlersAttached` flag. Cleanup cleared that flag before unsubscribe calls and swallowed unsubscribe failures, so cleanup could report safe and dispose `Done` without proving every native handler was detached. Partial attach failure had the same ambiguity.

## Source-ready contract
- `CommandEnded`, `CommandCancelled`, and `CommandFailed` ownership are tracked independently.
- Each ownership bit is published only after its native add succeeds.
- Partial attachment failure rolls back every successfully added handler.
- Each ownership bit is cleared only after its matching native remove succeeds.
- Cleanup is considered safe only when all three bits are clear.
- If serialized cleanup cannot prove full detach, the completion handle is intentionally kept alive; QSAVE is not replayed or retried.
- Terminal completion remains exactly-once through `_terminalSet`.
- Existing current-document identity, rooted path, CMDACTIVE, DBMOD, mutation-ledger, and process-global writer behavior remain unchanged.

## Deterministic verification
Run:

```text
python scripts/preflight-mcp-native-qsave-handler-lifetime.py
```

The auto-discovered guard pins per-handler ownership, add-before-publish ordering, rollback on partial attachment, and fail-closed detach truthfulness.

## Runtime boundary
Source/static validation and V25 compilation are REMOTE_SAFE. Injecting native BricsCAD event add/remove failures requires a licensed host and remains LOCAL_ONLY / NO_RESULT until actually executed. Do not report static evidence as `LOCAL_PASS`.
