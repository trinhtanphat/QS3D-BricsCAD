# MCP Multi-Session Single-Writer Runbook

## Purpose

QS3D's embedded MCP may admit multiple concurrent clients and MCP sessions, but all mutations against the same BricsCAD process/DWG must pass through one process-global writer lane. Read-only tools remain available to other sessions while an explicit writer lease is active.

This runbook covers Issue #5279 and the `mcp-process-global-single-writer-dwg-mutation` ownership contract.

## Writer modes

### Backward-compatible ephemeral writer

Existing clients may continue to call mutation tools with `confirmMutation=true` and no `writerToken`. Each mutation is serialized through the process-global writer gate and releases the ordinary call gate when that mutation returns, except when an asynchronous native BricsCAD command retains the native-command barrier described below.

### Explicit multi-step writer lease

Use an explicit lease when one MCP workflow must prevent another session from interleaving mutations across several calls.

1. Call `cad_writer_acquire` with optional `leaseSeconds`.
2. Keep the returned opaque `writerToken` private to that workflow.
3. Supply that same `writerToken` on every mutation tool call in the workflow.
4. Call `cad_writer_release` with the matching `writerToken` when the workflow is done.

Lease bounds are 15–300 seconds with a default of 120 seconds. A valid mutation using the active lease refreshes its expiry. A mutation without the matching token fails before CAD dispatch while an explicit lease is active.

`cad_writer_status` reports coordination state without returning or logging the writer token.

## Native command barrier

`SendStringToExecute` is asynchronous: the MCP call can return before BricsCAD finishes the queued command. Therefore all MCP paths that queue native commands retain a logical DWG writer barrier after the call returns.

Covered paths include:

- classic `cad_command_sequence` command dispatch;
- the direct `cad_command_sequence` EXTRUDE bridge;
- `qs3d_run_command`.

The native-command barrier is released only when BricsCAD reports the matching command terminal event (`CommandEnded`, `CommandCancelled`, or `CommandFailed`) or when MCP automation/server reset/stop explicitly clears coordination state. There is no timer-based native barrier expiry.

If `cad_writer_release` is called while an owned native command is still pending, release is deferred until that native command reaches a terminal event.

## Save ownership

QSAVE and direct save operations enter through the same process-global mutation ownership. `SaveActiveDocument` performs exactly one native save attempt and verifies clean `DBMOD` before reporting success.

If BricsCAD reports a native file-open/save error or completion is uncertain, do not blindly retry. Inspect the drawing, `DBMOD`, MCP audit evidence and filesystem state first; an automatic retry could duplicate or conflict with an in-flight write.

## Token handling

Writer tokens are opaque capability values. Do not print them to MCP audit logs, diagnostics, status payloads, exception text, telemetry or documentation examples. Only the acquire response and subsequent caller-supplied `writerToken` argument should carry the value.

## Hosted/source validation

Run the source contract guard:

```powershell
python scripts/preflight-mcp-single-writer-multi-session.py
```

Hosted CI proves source-level contracts and integration/build health only. It does not prove licensed same-DWG concurrency behavior inside a real BricsCAD process.

## LOCAL_ONLY licensed stress validation

On a licensed BricsCAD environment, use two or more independent MCP sessions against the same embedded MCP process and same DWG.

Validate at minimum:

1. two read-only calls can remain independently usable while one session owns an explicit writer lease;
2. a mutation from the non-owner fails before CAD dispatch while the lease is active;
3. the owner can execute several mutations with the same token without interleaving from the other session;
4. an asynchronous EXTRUDE or QS3D native command keeps later mutations blocked until BricsCAD emits its terminal event;
5. QSAVE/save remains single-attempt and produces no `eCantOpenFile`/cross-session save collision under the coordinated workflow;
6. release removes ownership and a later unleased mutation can enter normally;
7. automation stop/server reset clears stale lease/native-command coordination safely;
8. audit/status output contains no writer token.

Record this licensed stress evidence separately as LOCAL_ONLY; do not treat hosted/static checks as a substitute.
