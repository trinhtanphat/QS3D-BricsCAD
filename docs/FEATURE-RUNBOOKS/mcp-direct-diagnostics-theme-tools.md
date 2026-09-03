# MCP direct diagnostics and host-theme tools

Issue: #4765  
Follow-up to: #4750 / PR #4752

## Purpose

Expose the already-bounded QS3D/BricsCAD diagnostic stream and host-wide theme coordinator as first-class MCP tools so ChatGPT does not need to route these operations through generic `cad_audit_tail` or `qs3d_run_command` calls.

## Tools

- `diagnostics_log_tail` — latest 1–100 sequenced unified diagnostic events.
- `diagnostics_since` — up to 100 events with `sequence > afterSequence`.
- `diagnostics_snapshot` — captures current MCP state, theme state, BricsCAD host state and the latest bounded QS3D project audit into the unified diagnostic stream, then returns the newest events.
- `diagnostics_wait` — bounded long-poll for events after a sequence cursor, with a maximum 15 second timeout. It does not enable an unbounded server event stream.
- `theme_get` — returns configured `system|dark|light`, effective `dark|light`, and BricsCAD `COLORTHEME`.
- `theme_set` — sets `system|dark|light` through the canonical `Qs3dThemeCoordinator`; it is registered as an MCP mutation and therefore requires `confirmMutation=true` and respects emergency-stop epoch validation.

## Diagnostics boundaries

The tools do not accept filesystem paths. They can read only the canonical `%AppData%/QS3D/mcp-agent-audit.jsonl` stream and its single rotated `.1` file through `McpCadAgentRuntime.AuditFilePath`. Event responses are bounded to 100 entries, individual candidate lines are bounded, and only sequenced diagnostic records are returned.

On diagnostics startup, `McpDiagnosticHub` scans only those two bounded canonical audit files for the highest persisted diagnostic sequence and seeds the in-process counter from it. This keeps `diagnostics_since` / `diagnostics_wait` cursors monotonic across a normal QS3D or BricsCAD restart while retained audit history exists, instead of reusing low sequence values from zero.

Host-facing snapshot/state reads are marshalled through BricsCAD `ExecuteInApplicationContext` with a bounded timeout and cancel-before-start behavior. The MCP HTTP worker therefore never directly touches document/project/system-variable state that belongs on BricsCAD's application context.

The underlying `McpDiagnosticHub` continues to redact bearer/token/secret/password-like values and captures MCP transport errors/OAuth activity, QS3D startup/runtime exceptions, QS3D project audit entries, BricsCAD command failures/cancellations, and QS3D command lifecycle events.

## Theme propagation

`theme_set` does not style only the MCP popup. It calls `Qs3dThemeCoordinator`, which owns the persistent theme mode, changes BricsCAD `COLORTHEME`, recolors canonical QS3D WPF resources and loaded/future QS3D surfaces, and follows Windows app theme changes while configured as `system`. The result-state read is marshalled back onto BricsCAD application context before reading `COLORTHEME`.

## Security invariants

- No arbitrary file reader.
- No arbitrary shell, cmd, PowerShell, process launch, script or eval surface.
- Direct diagnostic tools are read-only MCP operations.
- `theme_set` stays in the existing MCP mutation guard and needs `confirmMutation=true`.
- Desktop local-consent remains scoped to `desktop_*` tools; host theme switching does not accidentally request desktop-automation consent.
- OAuth/bearer authorization, audit, emergency stop and existing MCP protocol negotiation remain unchanged.

## Verification

Run:

```text
python scripts/preflight-mcp-direct-diagnostics-theme.py
```

Then require fresh shared preflight/core/V25 plugin CI on the exact PR head and current `main` before merge.
