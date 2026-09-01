# MCP capability lanes

The QS3D embedded MCP server deliberately separates native BricsCAD control from QS3D business semantics.

## Tool lanes

| Prefix | Lane | Meaning |
| --- | --- | --- |
| `mcp_*` | MCP control | Connector/capability/routing state. |
| `bricscad_*` | BricsCAD host | Host/version/document state. |
| `cad_*` | CAD direct | Native database, commands, view, save, entity and emergency-control operations. |
| `desktop_*` | Desktop automation | Bounded Windows/BricsCAD UI automation. |
| `qs3d_*` | QS3D domain | Project, Family, Floor, business authoring and QS3D command semantics. |

A QS3D-domain failure does not mean the MCP transport or BricsCAD host is disconnected. `cad_*` tools continue to operate as long as BricsCAD itself is available.

## Status tools

`mcp_status` returns the aggregate capability view. `bricscad_status` returns host state only. `qs3d_domain_status` reports QS3D business health/context only. `qs3d_status` is retained as a deprecated compatibility alias of `qs3d_domain_status`; it intentionally no longer contains the active DWG name or current layer.

Missing QS3D context such as an active Family or Floor is reported as `QS3D_CONTEXT_REQUIRED`. It does not set CAD-direct unavailable.

## Execution mode

Every tool accepts optional `executionMode` and the compatibility alias `execution_mode`:

- `AUTO`: normal planner-selected routing.
- `CAD_DIRECT`: native CAD/BricsCAD/desktop operations are allowed; QS3D business mutations are rejected. QS3D status remains readable.
- `QS3D_DOMAIN`: QS3D business mutations are allowed. Read-only CAD/host/desktop diagnostics and emergency controls remain available, but native CAD and desktop automation mutations are rejected so failed business semantics cannot silently fall back to approximate geometry or UI-driven CAD edits.

If both aliases are supplied, they must resolve to the same value.

## Error contract

MCP tool failures return `isError: true` plus structured `error.code`, `error.lane`, and `error.message` fields. Stable codes include `CAD_HOST_UNAVAILABLE`, `CAD_COMMAND_FAILED`, `DESKTOP_CONSENT_REQUIRED`, `DESKTOP_AUTOMATION_FAILED`, `QS3D_DOMAIN_UNAVAILABLE`, `QS3D_CONTEXT_REQUIRED`, `QS3D_SOURCE_BUG`, `EXECUTION_MODE_VIOLATION`, `MCP_INVALID_ARGUMENT`, and `MCP_TOOL_FAILED`.

## Routing examples

A request such as “draw a 10 x 20 rectangle” should use a `cad_*` tool and has no QS3D project prerequisite. A request such as “place the active single-footing Family on the active Floor” uses `qs3d_place_single_footing`; if its business context is missing, the tool returns a QS3D-domain error while CAD-direct remains usable.

In `AUTO`, a caller may explicitly choose a CAD equivalent after a QS3D error. In `QS3D_DOMAIN`, native CAD and desktop mutation fallbacks are intentionally blocked unless the caller changes the execution mode, preventing silent semantic degradation.
