# MCP Capability Lanes Design

## Goal

Make ChatGPT function calling treat native BricsCAD control and QS3D business workflows as independent capabilities. A broken or missing QS3D project/domain context must not make `cad_*`, `bricscad_*`, or eligible `desktop_*` tools appear unavailable.

## Architecture

The embedded MCP surface is split into five logical lanes:

- `mcp_*`: connector/capability state and routing policy.
- `bricscad_*`: host/runtime/document state.
- `cad_*`: native BricsCAD database, command, view, save, and entity operations.
- `desktop_*`: bounded Windows/BricsCAD UI automation.
- `qs3d_*`: QS3D business semantics, project/family/floor context, and domain commands.

`McpCadAgentRuntime` remains the outer confirmation/emergency-stop dispatch boundary. Native CAD stays in `McpCadDirectModelRuntime`. QS3D business execution moves to a dedicated `McpQs3dDomainRuntime`. Shared lane classification, execution-mode policy, and error classification live in host-neutral `QS3D.Core.Agent.McpToolCapabilityContract` so they can be smoke-tested without BricsCAD.

## Status Contract

Expose four status tools:

- `mcp_status`: aggregate capability view and selected execution mode.
- `bricscad_status`: BricsCAD host/version/active-document/current-layer state only.
- `qs3d_domain_status`: QS3D domain health and business-context availability only.
- `qs3d_status`: deprecated compatibility alias for `qs3d_domain_status`; it must not contain active document or current layer fields.

No QS3D status lookup may create or bind a project as a side effect. It may inspect only an already cached canonical project context. Missing Family/Floor/project context is a context condition, not a CAD/MCP disconnect.

## Execution Modes

Every tool schema accepts optional `executionMode` and compatibility alias `execution_mode` with values `AUTO`, `CAD_DIRECT`, or `QS3D_DOMAIN`. If both aliases are supplied they must agree.

- `AUTO`: normal routing; any published lane may be used.
- `CAD_DIRECT`: QS3D business mutations are rejected with `EXECUTION_MODE_VIOLATION`; read-only QS3D domain status remains available. Native CAD, BricsCAD status, desktop automation, and emergency controls remain usable.
- `QS3D_DOMAIN`: QS3D mutations are allowed. Read-only CAD/host/desktop diagnostics and emergency controls remain usable, but native CAD and desktop automation mutations are rejected so the system cannot silently replace failed business semantics with approximate geometry or UI-driven CAD edits.

## Error Contract

Tool errors must return structured `code`, `lane`, and `message` fields while preserving the MCP `isError` envelope. Canonical codes are:

- `CAD_HOST_UNAVAILABLE`
- `CAD_COMMAND_FAILED`
- `DESKTOP_CONSENT_REQUIRED`
- `DESKTOP_AUTOMATION_FAILED`
- `QS3D_DOMAIN_UNAVAILABLE`
- `QS3D_CONTEXT_REQUIRED`
- `QS3D_SOURCE_BUG`
- `EXECUTION_MODE_VIOLATION`
- `MCP_INVALID_ARGUMENT`
- `MCP_TOOL_FAILED`

A QS3D source/domain failure updates only QS3D-domain health. It must not alter native CAD availability.

## Compatibility

Existing tool names remain published. `qs3d_run_command` keeps its bounded `^QS3D[A-Za-z0-9_]*$` command contract. `qs3d_place_single_footing` keeps the same X/Y schema and shared Active Floor semantics, but ownership moves out of the direct-CAD runtime.

## Testing

Add a pure Core smoke test for lane classification, execution-mode gates, aliases, and error mapping, including proof that `QS3D_DOMAIN` allows desktop observation but blocks desktop mutations. Add source preflight guards proving QS3D placement is no longer owned by `McpCadDirectModelRuntime`, status fields are separated, both execution-mode aliases are published, structured errors exist, `QS3D_DOMAIN` cannot mutate through the desktop lane, and native CAD dispatch contains no QS3D-health dependency. Existing V25/V26 build and aggregate preflight remain mandatory before merge.
