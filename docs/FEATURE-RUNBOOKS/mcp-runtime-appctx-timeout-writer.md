# MCP runtime application-context timeout writer safety

## Scope

This runbook covers `McpCadAgentRuntime.InvokeCad` dispatch through BricsCAD `ExecuteInApplicationContext` while an MCP mutation owns the process-global DWG writer gate.

## Safety invariant

A bounded timeout may cancel work only while the CAD callback is still queued. Once the callback has atomically changed the work item from `Queued` to `Running`, the caller must not return or unwind its mutation scope until that callback settles. Releasing the writer while already-started CAD work is still executing would admit a second mutation concurrently with native/database work from the first request.

The runtime must therefore use the same fail-closed race rule as `McpCadMutationCoordinator.InvokeInCadContext`:

1. Queue exactly one application-context callback.
2. Wait for the normal bounded dispatch timeout.
3. On timeout, atomically transition `Queued -> CancelledBeforeStart`.
4. If cancellation wins, report timeout without retrying the operation; the later callback observes cancellation and performs completion-handle cleanup without executing the action.
5. If cancellation loses because the callback is already `Running`, wait for its completion while retaining caller/writer ownership, then propagate the callback's actual result or error.
6. A synchronous `ExecuteInApplicationContext` failure must still dispose the completion handle owned by the caller.

There is deliberately no retry after timeout. Native side effects are uncertain once execution has started, and blind replay could duplicate CAD mutations.

## Validation

Run `python scripts/preflight-mcp-runtime-appctx-timeout-writer.py`. The guard is auto-discovered by the aggregate feature-source preflight and locks the state transition ordering, post-start settle behavior, callback ownership, and completion-handle disposal boundary.

Run the admitted BricsCAD V25 compile in hosted CI. A licensed BricsCAD timeout-race injection is a separate `LOCAL_ONLY` verification; hosted/static GREEN does not constitute native runtime PASS.
