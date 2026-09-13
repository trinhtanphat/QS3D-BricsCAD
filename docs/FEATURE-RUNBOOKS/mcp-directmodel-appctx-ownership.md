# MCP DirectModel application-context mutation ownership

## Scope

This runbook covers DirectModel mutations routed through `McpCadDirectModelRuntime`: native solid create/extrude/boolean operations, layer-state writes/restores, view mutations, and direct `cad_command_sequence` layout/native-command admission. Read-only layer/view/status calls remain bounded diagnostic reads.

## Safety contract

A mutation must not execute through `McpDiagnosticHub.InvokeInCadContext`. That helper is response-bounded and is reserved for reads because a timeout after callback start cannot prove a side effect stopped.

DirectModel mutations use a queued/running/cancelled-before-start/terminal application-context work item. A timeout may cancel only while still queued. Once running, the caller keeps completion ownership until the callback is terminal; there is no automatic replay or blind retry.

At callback start, capture the exact active managed `Document` and its non-zero native `Database.UnmanagedObject` generation. Revalidate both immediately before successful result publication. Generation drift makes completion uncertain and fails closed.

The outer `McpCadAgentRuntime.Mutation` remains the process-global writer/ack owner. Native command sequences continue to transfer pending native-command ownership to `McpCadMutationCoordinator`; this DirectModel dispatcher must not create a second writer.

## Validation

Run:

`python scripts/preflight-mcp-directmodel-appctx-ownership.py`

`python scripts/preflight-mcp-production-correctness.py`

`python scripts/preflight-mcp-capability-lanes.py`

`git diff --check`

Licensed BricsCAD callback scheduling, same-wrapper database replacement/disposal timing, and `AccessViolation` evidence are LOCAL_ONLY / NO_RESULT unless exercised in the licensed runtime.