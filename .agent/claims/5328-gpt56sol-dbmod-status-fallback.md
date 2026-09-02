# Agent reservation — issue #5328

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260902-dbmod-status-fallback
Canonical carrier: agent/gpt56sol-20260902-dbmod-status-fallback/issue-5328-dbmod-status-fallback
Lane-Key: issue-5328
Ownership-Key: v25.mcp.dbmod-status-fallback
Branch: agent/gpt56sol-20260902-dbmod-status-fallback/issue-5328-dbmod-status-fallback
Expected-Paths: src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs; scripts/preflight-mcp-cad-agent-dbmod-semantics.py; scripts/preflight-mcp-cad-direct3d-save.py; docs/FEATURE-RUNBOOKS/mcp-cad-agent-dbmod-semantics.md; .agent/claims/5328-gpt56sol-dbmod-status-fallback.md

Scope: align `cad_active_document` and the synchronous fallback QSAVE completion check with the persistent DBMOD content mask already used by direct save after #5325. Residual window/view bits must not be reported as unsaved drawing content; persistent bits remain fail-closed. The inherited direct3d/save preflight is included only to replace its stale exact-zero active-document assertion with the same content-aware invariant. Licensed BricsCAD save/reopen qualification remains LOCAL_ONLY.
