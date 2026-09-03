# Agent reservation — issue #5537

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260903-c04-transient-retry-circuit
Canonical carrier: agent/gpt56sol-20260903-c04-transient-retry-circuit/issue-5537-transient-retry-circuit
Lane-Key: issue-5537
Ownership-Key: v25.mcp.self-healing-transient-retry-circuit
Branch: agent/gpt56sol-20260903-c04-transient-retry-circuit/issue-5537-transient-retry-circuit
Expected-Paths: src/QS3D.BricsCAD.V25/McpSelfHealingRepairRuntime.cs; scripts/preflight-mcp-self-healing-transient-circuit.py; docs/FEATURE-RUNBOOKS/mcp-self-healing-transient-circuit.md; .agent/claims/5537-gpt56sol-mcp-transient-retry-circuit.md

Scope: add a bounded circuit breaker for repeated transient repair metadata so supervisors cannot be instructed to retry the same persistent CAD/transport blockage indefinitely.