# Agent reservation — issue #5475

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260903-1517-direct-kernel-clones
Canonical carrier: agent/gpt56sol-20260903-1517-direct-kernel-clones/issue-5475-direct-kernel-clones
Lane-Key: issue-5475
Ownership-Key: v25.mcp.direct-kernel-clones
Branch: agent/gpt56sol-20260903-1517-direct-kernel-clones/issue-5475-direct-kernel-clones
Expected-Paths: src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs; scripts/preflight-mcp-direct-geometry-runtime-safety.py; docs/FEATURE-RUNBOOKS/mcp-direct-geometry-runtime-safety.md; .agent/claims/5475-gpt56sol-direct-kernel-clones.md

Scope: replace Region-backed direct extrusion with transient Curve-clone extrusion and move Boolean kernel evaluation fully onto transient target/operand clones before HandOverTo preserves the original target identity. Preserve existing REGENALL/modal safety and bounded Solid3d extents contracts. No save-path changes.
