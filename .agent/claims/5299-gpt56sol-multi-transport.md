# Reservation #5299 — MCP Multi-Transport Profiles / App-Scoped Secret Firewall

- Canonical-Session: agent:gpt-5.6-sol
- Branch: `agent/gpt56sol-20260902-1940-multitransport/issue-5299-successor`
- Intent: introduce one app-scoped multi-transport profile registry with per-transport isolation, actor/builder secret guards, explicit secret rotation, retired-secret zeroization, send-session serialization, and a hosted preflight/doc packet.
- Expected-Paths:
  - `.agent/claims/5299-gpt56sol-multi-transport.md`
  - `src/QS3D.BricsCAD.V25/McpTransportProfileRegistry.cs`
  - `src/QS3D.BricsCAD.V25/McpOpenAiSecureTunnel.cs`
  - `scripts/preflight-mcp-multi-transport-registry.py`
  - `docs/FEATURE-RUNBOOKS/mcp-multi-transport-registry.md`
  - `docs/superpowers/specs/2026-09-02-mcp-multi-transport-registry-design.md`
  - `docs/superpowers/plans/2026-09-02-mcp-multi-transport-registry.md`
- Verification: `python scripts/preflight-mcp-multi-transport-registry.py`; `python scripts/run-core-checks.py`; `python scripts/agent-guard.py pre-merge --base-ref main --head-ref HEAD --reservation .agent/claims/5299-gpt56sol-multi-transport.md`
- Overlap: no intentional code overlap; documentation plus focused additive MCP bridge/session source.
