# MCP application-context timeout writer barrier

## Scope

This runbook covers the `McpCadMutationCoordinator` handoff used to arm native-command writer ownership through BricsCAD `ExecuteInApplicationContext`.

## Defect

Before issue #5392, `InvokeInCadContext` waited five seconds for the application-context callback and then returned a timeout immediately. `PrepareNativeCommand` consequently released `MutationGate`, but the queued callback still held the side-effecting action. If BricsCAD ran that callback later, it could arm `_pending` and install native command event handlers after the original request had already failed and released writer ownership. The resulting reservation belonged to no request and could ghost-block later mutations.

There was also a boundary race when the timeout and callback start occurred together: the caller could decide that dispatch had timed out while the callback was already beginning coordinator mutation.

## Invariant

Each queued `CadContextWork<T>` has one atomic ownership transition:

- `queued -> running` when the BricsCAD callback claims execution; or
- `queued -> cancelled-before-start` when the caller's bounded wait expires first.

Only the winner may proceed. A callback that observes cancellation returns before executing `Action` and therefore cannot mutate coordinator state. If cancellation loses because the callback is already running, the caller keeps its writer ownership and waits fail-closed for that already-started in-process action to settle before returning or releasing `MutationGate`.

This does not add retries, extend MCP mutation authority, or change the post-enqueue native-command terminal-event barrier established by issue #5378.

## Deterministic validation

Run:

```text
python scripts/preflight-mcp-appctx-timeout-barrier.py
python scripts/preflight-mcp-emergency-native-barrier.py
```

The first guard checks the explicit queued/running/cancelled-before-start CAS handshake, timeout arbitration order, and claim-before-side-effect ordering. The second protects the adjacent emergency-stop invariant that a dispatching native command remains owned until its matching terminal event.

The normal aggregate feature-source-guard job discovers both guards automatically.

## Runtime boundary

The handshake implementation, source guards, and admitted V25 compilation are `REMOTE_SAFE` evidence. Actual BricsCAD application-context scheduling/timing is licensed-host behavior and remains `LOCAL_ONLY / NO_RESULT` unless separately executed on a licensed host. Hosted CI must not be reported as `LOCAL_PASS`.

## Local qualification scenario

On a licensed V25 host, use a controlled diagnostic seam to delay an application-context callback beyond the bounded wait before it starts. Verify the request reports timeout, no pending native-command barrier appears later, and another valid mutation is not blocked by a ghost reservation. Separately race the callback into `running` immediately around the timeout boundary and verify the request does not release writer ownership until the callback has settled. Preserve zero unrelated DWG mutation and normal terminal-event cleanup.
