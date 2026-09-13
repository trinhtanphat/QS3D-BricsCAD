# Embedded MCP CAD mutation application-context ownership

## Defect class

`McpEmbeddedServer.InvokeCad` marshals CAD callbacks through `ExecuteInApplicationContext` with a bounded response wait. The legacy timeout path can return an uncertain failure after the callback has already transitioned to running, while the callback continues native work. For side-effecting tools this makes transport truth diverge from CAD truth and permits an unsafe caller retry or a second writer while the first mutation is still active.

## Required ownership contract

- Queue state starts as `queued`.
- Timeout may atomically transition `queued -> cancelled-before-start` and return a timeout only when the callback has not started.
- Callback start atomically transitions `queued -> running` before invoking native CAD work.
- Once `running`, the caller retains ownership until the exact callback reaches terminal completion; response timeout does not abandon the writer.
- A cancelled callback must return before entering any `try/finally` that signals a completion object which the cancelling caller may already have disposed.
- Queue failure disposes the completion object and propagates a sanitized/structured failure.
- No blind replay, no automatic retry, and no second writer are introduced.

## Safety review

Review every embedded mutation reachable through this boundary, including create/transform/delete/layer operations, native command sequencing and QS3D command dispatch. Verify thread/application context, document lifetime, native database wrapper lifetime, `CMDACTIVE`/`DBMOD` result truth, cancellation, duplicate mutation, stale active document, error redaction and fail-soft behavior. Where a tool captures a document-specific result, it must not publish success for a different document/native database generation.

## Verification

`python scripts/preflight-mcp-embedded-mutation-appctx-ownership.py`

Also run the MCP production/capability preflights and exact-head Shared + Hybrid CI. Licensed BricsCAD callback scheduling, same-wrapper database replacement/disposal, MDI timing and AccessViolation behavior are LOCAL_ONLY / NO_RESULT unless exercised in a licensed host.
