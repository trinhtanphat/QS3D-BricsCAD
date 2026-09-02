# Agent reservation — issue #5299

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260902-1505-multitransport
Canonical carrier: agent/gpt56sol-20260902-1505-multitransport/issue-5299-transport-registry
Lane-Key: issue-5299
Ownership-Key: v25.mcp.concurrent-transport-registry
Branch: agent/gpt56sol-20260902-1505-multitransport/issue-5299-transport-registry
Expected-Paths: src/QS3D.BricsCAD.V25/McpTransportProfileRegistry.cs; src/QS3D.BricsCAD.V25/McpOpenAiSecureTunnel.cs; scripts/preflight-mcp-multi-transport-registry.py; docs/FEATURE-RUNBOOKS/mcp-multi-transport-registry.md; docs/superpowers/specs/2026-09-02-mcp-multi-transport-registry-design.md; docs/superpowers/plans/2026-09-02-mcp-multi-transport-registry.md; .agent/claims/5299-gpt56sol-multi-transport.md

Scope: add a versioned, secret-free transport profile registry and migrate legacy singleton transport preference into a default profile. Preserve SelectedProvider only as backward-compatible UI preference; it must no longer represent exclusive external transport ownership. Existing process-global MCP CAD single-writer coordination is out of scope and must remain unchanged.