# Agent reservation — issue #5532

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260903-c04-durable-ack-document-affinity
Canonical carrier: agent/gpt56sol-20260903-c04-durable-ack-document-affinity/issue-5532-durable-replay-document-affinity
Lane-Key: issue-5532
Ownership-Key: v25.mcp.durable-ack-document-affinity
Branch: agent/gpt56sol-20260903-c04-durable-ack-document-affinity/issue-5532-durable-replay-document-affinity
Expected-Paths: src/QS3D.BricsCAD.V25/McpMutationAckLedger.cs; src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs; scripts/preflight-mcp-durable-mutation-document-affinity.py; docs/FEATURE-RUNBOOKS/mcp-durable-mutation-document-affinity.md; .agent/claims/5532-gpt56sol-mcp-durable-replay-document-affinity.md

Scope: fail closed when a persisted/durable mutation actionId is replayed against a different or unverifiable active drawing, while preserving same-drawing durable replay and volatile uncertainty protection.