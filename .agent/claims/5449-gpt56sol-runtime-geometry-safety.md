# Agent reservation — issue #5449

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260903-0808-runtime-geometry-safety
Canonical carrier: agent/gpt56sol-20260903-0808-runtime-geometry-safety/issue-5449-runtime-geometry-safety
Lane-Key: issue-5449
Ownership-Key: v25.mcp.direct-geometry-native-command-safety
Branch: agent/gpt56sol-20260903-0808-runtime-geometry-safety/issue-5449-runtime-geometry-safety
Expected-Paths: src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs; scripts/preflight-mcp-direct-geometry-runtime-safety.py; docs/FEATURE-RUNBOOKS/mcp-direct-geometry-runtime-safety.md; .agent/claims/5449-gpt56sol-runtime-geometry-safety.md

Scope: preserve direct Region extrusion and transient-clone Boolean contracts, guard Solid3d extents fallback, and fail closed on unsafe REGENALL/modal native command dispatch. cad_save/QSAVE remains owned by #5441/#5442 and is explicitly excluded from this lane.
