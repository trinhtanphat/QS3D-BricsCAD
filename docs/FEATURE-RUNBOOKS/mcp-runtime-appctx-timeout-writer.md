# MCP runtime application-context timeout writer safety

## Scope

This runbook covers `McpCadAgentRuntime.InvokeCad` dispatch through BricsCAD `ExecuteInApplicationContext` while an MCP mutation owns the process-global DWG writer gate.

## Root cause

The application-context dispatch already had a bounded initial wait and an atomic queued-work cancellation path. The problematic branch occurred after the callback had changed from `Queued` to `Running`: the request used an unbounded `item.Done.Wait()`. A slow or stalled already-started callback could therefore keep the MCP response blocked indefinitely.

Simply returning at that deadline would be unsafe if the caller also disposed the mutation scope: a second request could enter the process-global writer while the first callback was still mutating BricsCAD.

## Safety invariants

1. Queue exactly one application-context callback.
2. Wait for the normal bounded dispatch timeout.
3. On timeout, atomically attempt `Queued -> CancelledBeforeStart`.
4. If cancellation wins, report timeout; a later delivered callback cannot invoke `item.Action()` because its running-state CAS loses.
5. If cancellation loses because the callback is already `Running`, do not replay the operation and do not extend the MCP request with an unbounded completion wait.
6. Before caller unwind, transfer completion ownership to the `CadWorkItem` and transfer the active mutation writer through `McpCadMutationCoordinator.DetachMutationForDeferredCompletion`.
7. The MCP request may then return a bounded started-work timeout, but the detached process-global writer remains quarantined until `CadWorkItem.Complete()` runs on the callback terminal path.
8. Started/uncertain work keeps its accepted mutation acknowledgement identity. It is not abandoned for retry because blind replay could duplicate a CAD mutation already in progress.
9. If the callback terminal path wins the race before deferred writer attachment, `AttachWriterScope` releases the writer immediately because completion is already authoritative.
10. Completion-handle disposal is ownership-based; the old racy abandoned-handle pattern must not be restored.

There is deliberately no retry after a started-work timeout. The caller can use mutation-status/idempotency evidence rather than resubmitting uncertain side effects.

## Validation

Run:

```text
python scripts/preflight-mcp-runtime-appctx-timeout-writer.py
```

The focused guard locks the bounded cancel/handoff ordering, absence of `item.Done.Wait()` on the started path, deferred writer transfer, callback terminal release, and race-safe completion ownership. It also preserves the older response-budget, embedded-MCP, full-agent, and production-hardening proofs.

Fresh repository CI must additionally pass source guards, deterministic smoke tests, and the BricsCAD V25 compile lane on the exact PR head before merge.

## Runtime boundary

Source/static guards, deterministic tests, and V25 compile are `REMOTE_SAFE`. A licensed BricsCAD timeout-race injection that deliberately stalls an already-started application-context callback is `LOCAL_ONLY`; hosted/static GREEN is not a claim that the licensed runtime responsiveness test was exercised.
