# Agent reservation — issue #5441

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260903-mcp-startup-save-stability
Canonical carrier: agent/gpt56sol-20260903-mcp-startup-save-stability/issue-5441
Lane-Key: issue-5441
Ownership-Key: v25.mcp.startup-save-stability
Branch: agent/gpt56sol-20260903-mcp-startup-save-stability/issue-5441
Expected-Paths: src/QS3D.BricsCAD.V25/PluginEntry.cs; src/QS3D.BricsCAD.V25/McpNativeCurrentDocumentSave.cs; src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs; src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs; scripts/preflight-mcp-startup-save-stability.py; docs/FEATURE-RUNBOOKS/mcp-startup-save-stability.md; .agent/claims/5441-gpt56sol-mcp-startup-save-stability.md

Scope: MCP/BricsCAD infrastructure only: startup desktop Resume + persistent tunnel autostart isolation, native current-document QSAVE lifecycle shared by cad_save and bounded QSAVE, read-only command-state contract verification, and preservation of screen-update safety. No A0X/drawing-specific content.
