# Agent reservation — issue #5734

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260905-view-direct-regressions
Canonical carrier: agent/gpt56sol-20260905-view-direct-regressions/issue-5734
Lane-Key: issue-5734
Ownership-Key: v25.mcp.view-direct-regressions
Branch: agent/gpt56sol-20260905-view-direct-regressions/issue-5734
Expected-Paths: src/QS3D.BricsCAD.V25/McpCadViewStatusRuntime.cs; scripts/preflight-mcp-view-extents-modal-safety.py; .agent/claims/5734-gpt56sol-view-direct-regressions.md

Scope: fix live V25 direct-view regressions where zoom extents dirties a clean DWG and cad_view_set can silently apply a different viewport size or trigger LookFrom/modal UI. Preserve fail-closed CMDACTIVE and no forced refresh behavior.
