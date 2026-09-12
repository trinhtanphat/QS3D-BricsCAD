# MCP native QSAVE application-context ownership

Issue: #6610
Lane: C04 — MCP / CAD Agent / Automation / Runtime Integration
Ownership-Key: `mcp.native-qsave-appctx-timeout-ownership-v1`

## Problem

`McpNativeCurrentDocumentSave` previously used `McpDiagnosticHub.InvokeInCadContext` for
side-effecting QSAVE setup and terminal-handler detach. The diagnostic dispatcher is allowed to
return after its bounded wait when a callback has already started. That contract is safe for an
abandoned read, but unsafe for a save mutation: the caller could observe failure and release its
mutation ownership while the application-context callback was still attaching handlers, queuing
QSAVE, or detaching handlers.

That creates a ghost-save / double-writer / stale-handler window and allows the tool result to
diverge from actual CAD state.

## Production contract

QSAVE setup and detach use a mutation-owned application-context work item with explicit states:

- `queued`: no side effect has started;
- `running`: the callback owns side-effecting CAD work;
- `cancelled-before-start`: timeout won the CAS while still queued, so the callback is prohibited
  from executing its action;
- `terminal`: the exact callback has finished (success or failure).

The initial wait is bounded. If the wait expires while still queued, the work is cancelled by CAS
before the caller unwinds. If the callback has already started, the caller must retain ownership and
wait for that exact callback to reach terminal state. It must not convert a running mutation into a
false-safe timeout and must not automatically replay QSAVE.

`McpDiagnosticHub.InvokeInCadContext` remains valid for read-only post-save DBMOD inspection; it is
not valid for QSAVE setup or native handler detach.

## Preserved native invariants

The existing save operation continues to:

- revalidate the expected managed `Document` and rooted database path before queueing;
- reject read-only drawings and nonzero `CMDACTIVE`;
- attach `CommandEnded`, `CommandCancelled`, and `CommandFailed` with conservative per-handler
  ownership and exact-once terminal completion;
- queue native `QSAVE` only through `McpCadMutationCoordinator.QueueNativeCommand`;
- retain unresolved handler cleanup and block the next save until cleanup is proven;
- verify persistent `DBMOD` content bits after a terminal QSAVE event;
- treat cancellation, emergency stop, handler-cleanup uncertainty, path/document drift, and DBMOD
  uncertainty as fail-closed states;
- never automatically retry or replay a save whose native completion is uncertain.

## Validation

Source/static validation:

```text
python scripts/preflight-mcp-native-qsave-appctx-ownership.py
python scripts/preflight-mcp-production-correctness.py
python scripts/preflight-mcp-capability-lanes.py
git diff --check
```

The dedicated preflight must fail if QSAVE setup or detach regresses to
`McpDiagnosticHub.InvokeInCadContext`, or if queued/running/cancel-before-start/terminal ownership
and the started-work terminal wait are removed.

## Runtime classification

Source/preflight validation is `REMOTE_SAFE`.

Timing-sensitive behavior inside licensed BricsCAD—including actual application-context scheduling,
native event accessor behavior, disposed-wrapper/AccessViolation behavior, and a real QSAVE during a
dispatch timeout—is `LOCAL_ONLY / NO_RESULT` until executed in the licensed host. No source or CI
result may be relabeled as a licensed native PASS.
