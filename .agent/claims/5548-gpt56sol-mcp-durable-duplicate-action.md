# Agent reservation — issue #5548

Status: CLOSED
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260904-c04-ledger-duplicate-action
Canonical carrier: agent/gpt56sol-20260904-c04-ledger-duplicate-action/issue-5548-durable-ledger-duplicate-action
Lane-Key: issue-5548
Ownership-Key: v25.mcp.durable-ack-duplicate-action-load
Branch: agent/gpt56sol-20260904-c04-ledger-duplicate-action/issue-5548-durable-ledger-duplicate-action
Expected-Paths: src/QS3D.BricsCAD.V25/McpMutationAckLedger.cs; scripts/preflight-mcp-durable-mutation-duplicate-action.py; docs/FEATURE-RUNBOOKS/mcp-durable-mutation-duplicate-action.md; .agent/claims/5548-gpt56sol-mcp-durable-duplicate-action.md

Scope: fail closed when the persisted durable mutation ledger contains duplicate actionId identities, preventing file-order-dependent replay fingerprint/provenance replacement after restart.

Closeout: implementation merged to protected main via PR #5549 before issue #5554 began; this metadata close releases the completed reservation without changing its historical scope.
