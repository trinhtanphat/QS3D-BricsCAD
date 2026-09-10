# Agent reservation — issue #6163

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260908-mcp-prod-correctness
Canonical carrier: agent/mcp-prod-correctness-6163
Lane-Key: issue-6163
Ownership-Key: v25.mcp.production-correctness
Branch: agent/mcp-prod-correctness-6163
Expected-Paths: src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs; src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs; src/QS3D.BricsCAD.V25/McpQs3dDomainRuntime.cs; src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs; src/QS3D.BricsCAD.V25/McpPopupObserver.cs; src/QS3D.BricsCAD.V25/McpPopupWindowClassifier.cs; src/QS3D.BricsCAD.V25/McpNativeCurrentDocumentSave.cs; tests/QS3D.BricsCAD.V25.LocalQualification/McpProductionCorrectnessQualificationCommands.cs; scripts/preflight-mcp-cad-direct3d-save.py; scripts/preflight-mcp-production-correctness.py; scripts/preflight-mcp-popup-notification-observer.py; .agent/claims/6163-gpt56sol-mcp-prod-correctness.md

Scope: production-correct MCP domain/context binding, CAD Direct Boolean/extrusion validation and atomicity, native save completion semantics, HTTP transport error classification/correlation, popup severity mapping, build provenance and deterministic qualification. Foreground control remains fail-closed/local-consent-only and no paid resource is enabled. Licensed V25/V26 runtime evidence remains PENDING_NATIVE unless a real host is available.
