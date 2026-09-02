# Agent reservation — issue #5307

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260902-screen-update-race
Canonical carrier: agent/gpt56sol-20260902-screen-update-race/issue-5307-view-idle-gate
Lane-Key: issue-5307
Ownership-Key: v25.mcp.screen-update-view-idle-gate
Branch: agent/gpt56sol-20260902-screen-update-race/issue-5307-view-idle-gate
Expected-Paths: src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs; src/QS3D.BricsCAD.V25/McpCadViewStatusRuntime.cs; scripts/preflight-mcp-screen-update-safety.py; docs/FEATURE-RUNBOOKS/mcp-screen-update-safety.md; .agent/claims/5307-gpt56sol-screen-update-race.md

Scope: prevent direct MCP view mutations from entering BricsCAD graphics/view updates while another BricsCAD command is active. Fail closed on busy command state; do not force REGEN/UpdateScreen or auto-dismiss the popup.