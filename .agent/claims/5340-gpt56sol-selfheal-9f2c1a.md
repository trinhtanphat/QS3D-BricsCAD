# Agent reservation — issue #5340

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-selfheal-9f2c1a
Canonical carrier: agent/gpt56sol-selfheal-9f2c1a/issue-5340-mcp-self-healing-repair
Lane-Key: issue-5340
Ownership-Key: mcp/self-healing-repair-loop
Branch: agent/gpt56sol-selfheal-9f2c1a/issue-5340-mcp-self-healing-repair
Expected-Paths: .agent/claims/5340-gpt56sol-selfheal-9f2c1a.md; .github/workflows/issue-5340-main-sync-once.yml; docs/FEATURE-RUNBOOKS/mcp-self-healing-repair.md; docs/superpowers/plans/2026-09-02-mcp-self-healing-repair.md; scripts/preflight-mcp-self-healing-repair.py; src/QS3D.BricsCAD.V25/McpSelfHealingRepairRuntime.cs; src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs

Scope: add bounded, deduplicated self-healing repair metadata to existing MCP tools/call failures without adding a new MCP tool or bypassing the process-global CAD writer. Repeated identical repairable failures must open a circuit and require human review; caller/policy/schema mistakes must never become automatic source-repair candidates. The temporary branch-only `issue-5340-main-sync-once.yml` workflow is reserved solely to merge latest main without force and must delete itself in the merge commit.
