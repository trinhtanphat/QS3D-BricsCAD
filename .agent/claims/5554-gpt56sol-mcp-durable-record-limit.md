# Agent reservation — issue #5554

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260904-c04-ledger-record-limit
Canonical carrier: agent/gpt56sol-20260904-c04-ledger-record-limit/issue-5554-durable-ledger-record-limit
Lane-Key: issue-5554
Ownership-Key: v25.mcp.durable-ack-record-limit-load
Branch: agent/gpt56sol-20260904-c04-ledger-record-limit/issue-5554-durable-ledger-record-limit
Expected-Paths: src/QS3D.BricsCAD.V25/McpMutationAckLedger.cs; scripts/preflight-mcp-durable-mutation-record-limit.py; docs/FEATURE-RUNBOOKS/mcp-durable-mutation-record-limit.md; .agent/claims/5548-gpt56sol-mcp-durable-duplicate-action.md; .agent/claims/5554-gpt56sol-mcp-durable-record-limit.md

Scope: fail closed when a persisted durable mutation ledger contains more than MaxDurableRecords nonblank records, so tail records cannot bypass duplicate/malformed/identity validation; also close the already-merged issue #5548 reservation that otherwise collides on the same ledger path.
