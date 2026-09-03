# Agent reservation — issue #5504

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260903-shared-interactive-modal
Canonical carrier: agent/gpt56sol-20260903-shared-interactive-modal/issue-5504
Lane-Key: issue-5504
Ownership-Key: v25.mcp.shared-interactive-modal-admission
Branch: agent/gpt56sol-20260903-shared-interactive-modal/issue-5504
Expected-Paths: src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs; src/QS3D.BricsCAD.V25/McpOAuthConsent.cs; scripts/preflight-mcp-interactive-modal-admission.py; scripts/preflight-mcp-oauth-cad-interaction.py; docs/FEATURE-RUNBOOKS/mcp-interactive-modal-admission.md; docs/superpowers/plans/2026-09-03-mcp-interactive-modal-admission.md; .agent/claims/5504-gpt56sol-mcp-interactive-modal-admission.md
Excluded-Paths: src/QS3D.BricsCAD.V25/McpCadViewStatusRuntime.cs (#5452 owner)

Scope: introduce semantic shared interactive-modal admission at the existing process-global writer serialization boundary, migrate OAuth away from fake mutation admission, and guard against nested semaphore reacquisition from an already-active mutation/native-command scope.
