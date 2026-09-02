# MCP emergency-stop native-command writer barrier

## Scope

This runbook covers the process-global writer barrier around asynchronous native BricsCAD commands queued by `McpCadMutationCoordinator`.

## Defect

Before issue #5378, `McpCadAgentRuntime.StopAutomation()` invoked the coordinator reset before delivering ESC. The reset detached and cleared any pending native-command event barrier immediately. A later resume could therefore admit another MCP mutation while the old BricsCAD command had not yet emitted `CommandEnded`, `CommandCancelled`, or `CommandFailed`.

The failure mode is safety-critical because `SendStringToExecute` is asynchronous: returning from the MCP request is not proof that native DWG mutation is terminal.

## Invariant

Once `QueueNativeCommand` begins handing a command to BricsCAD, pending writer ownership is durable before `enqueue()` executes. Reset/emergency-stop may revoke leases and discard a reservation that is only prepared, but it must not detach a dispatching command's event barrier. The barrier is released only by:

- the matching native terminal event; or
- synchronous enqueue failure, through reservation disposal.

Resume does not reset the coordinator, so it cannot bypass a preserved pending command.

## Deterministic validation

Run:

```text
python scripts/preflight-mcp-emergency-native-barrier.py
```

The guard verifies that dispatch durability is established before enqueue, that reset preserves dispatching pending state, and that resume contains no coordinator reset path.

The repository's normal discovered feature-source-guard job executes this preflight automatically.

## Runtime boundary

The source/preflight behavior is `REMOTE_SAFE`. A V25 compile using admitted BricsCAD references is valid remote evidence for API compatibility.

Actual timing of `SendStringToExecute` plus `CommandEnded` / `CommandCancelled` / `CommandFailed` under licensed BricsCAD is `LOCAL_ONLY`. Do not claim `LOCAL_PASS` without a real licensed runtime execution. Until that evidence exists, report native runtime status as `LOCAL_ONLY / NO_RESULT`.

## Local qualification scenario

With a licensed V25 host, queue a bounded native command that remains active long enough to observe the barrier, trigger `cad_agent_stop`, then `cad_agent_resume` before terminal command evidence. Confirm that a new mutation is rejected while `pendingNativeCommand=true`, then confirm it becomes admissible only after the original command emits a matching terminal event. Also verify enqueue failure cleans the reservation and does not leave the writer permanently blocked.
