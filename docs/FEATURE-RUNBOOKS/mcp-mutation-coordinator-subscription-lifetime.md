# MCP Mutation Coordinator Native Subscription Lifetime

## Scope

This runbook covers the process-global native-command writer barrier in `McpCadMutationCoordinator`.
It governs `CommandWillStart`, `CommandEnded`, `CommandCancelled`, and `CommandFailed` handler ownership.

## Safety contract

- Publish the authoritative pending writer before the first fallible native event add.
- Mark each handler as may-be-subscribed before its native `+=` call.
- Accept callbacks only after all four handlers are coherently attached.
- A callback is authoritative only for the exact pending object, document, and normalized command.
- Disable callback authority before cleanup starts.
- Clear a handler ownership bit only after its matching native `-=` returns successfully.
- Any unresolved detach keeps the process-global writer quarantined.
- A terminal event may make detach retryable by `Reset`, but cleanup must never replay the CAD command.
- A second native writer is rejected while unresolved pending ownership exists.

## Validation

Run `python scripts/preflight-mcp-mutation-coordinator-subscription-lifetime.py`.
Also run MCP production-correctness, native-QSAVE lifetime, capability-lanes, Reservation-v2, and aggregate exact-head CI.
Licensed BricsCAD event-accessor fault injection remains `LOCAL_ONLY / NO_RESULT` unless a licensed runtime records it.
