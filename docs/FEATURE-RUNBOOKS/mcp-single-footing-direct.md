# MCP direct Móng đơn placement runbook

Issue: #4928

## Tool

The embedded MCP server publishes `qs3d_place_single_footing` through `McpCadDirectModelRuntime.ToolDescriptors()`.

Input contract:

- `x`: required drawing X coordinate, finite number.
- `y`: required drawing Y coordinate, finite number.
- `confirmMutation`: required literal `true`.
- No `z` input is exposed.

The tool is a semantic QS3D authoring mutation, not a prompt-driving wrapper around `QS3DDRAWSINGLEFOOTING`.

## Authoring ownership

`McpCadDirectModelRuntime.PlaceSingleFooting` resolves the active BricsCAD document and calls:

```text
SingleFootingCommands.PlaceActiveSingleFootingAt(document, new Point3d(x, y, 0d))
```

The `0d` Z value is deliberately not a footing elevation. `SingleFootingCommands.PlaceOne` resolves `ResolveActiveFloorElevation(project)` and converts that Active Floor elevation to drawing units before creating the semantic footprint and generated Solid3d. This keeps one canonical elevation policy for both interactive and MCP authoring.

The existing `QS3DDRAWSINGLEFOOTING` command remains the human repeated-center-pick workflow. MCP does not automate its `Editor.GetPoint` prompt and does not use generic `qs3d_run_command` for this operation.

## Safety boundary

`qs3d_place_single_footing` is registered in `McpCadDirectModelRuntime`, whose generic route is already exposed by the embedded server and dispatched through `McpCadAgentRuntime.Mutation`.

Therefore the tool requires the standard `confirmMutation=true` admission and inherits the shared mutation epoch/emergency-stop checks added by #4797. No shell/process launch, arbitrary script/eval, native command injection, desktop input, or generic filesystem surface is added.

## Source qualification

Run:

```text
python scripts/preflight-mcp-single-footing-direct.py
python scripts/preflight-single-footing-mcp-bridge.py
```

Repository protected CI must also pass aggregate preflight, deterministic Core smoke tests, trusted BricsCAD V25 compile-reference validation, and V25 plugin build on the exact PR head before merge.

## Licensed host qualification

Hosted/source CI is not licensed BricsCAD runtime evidence. On a licensed V25 host, minimally verify:

1. Open a QS3D project in Model Space with an Active Floor and active Móng → Móng đơn Family.
2. Call `qs3d_place_single_footing` with known drawing `x`,`y` and `confirmMutation=true`.
3. Confirm exactly one semantic Foundation source and one generated Solid3d are created at the requested X/Y.
4. Confirm base elevation equals the Active Floor elevation, including a non-zero-floor test.
5. Confirm Family L1/W1/L2/W2/H1/H2 and generated volume provenance match the active Móng đơn Family.
6. Confirm missing project, wrong active Family, Paper Space, missing confirmation, and malformed/non-finite coordinates fail closed.
7. Confirm `cad_agent_stop` prevents a new direct footing mutation until an explicit resume/new confirmation cycle.
