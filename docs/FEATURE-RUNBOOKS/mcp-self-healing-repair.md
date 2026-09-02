# MCP self-healing repair runbook

This runbook consumes the `repair` object attached to existing `tools/call` structured errors. It never creates a dynamic repair MCP tool and never bypasses mutation confirmation, writer leases, repository rules, CI, or licensed-runtime evidence.

## Decision contract

- `recommendedAction=correct_call_or_refresh_tools`: correct call locally, refresh `tools/list` when needed, and do not create a source patch.
- `recommendedAction=retry_transient`: retry, reconnect, or serialize the existing operation with bounded attempts. Do not infer a source defect from a transient lock, busy state, or transport failure.
- `sourceRepairEligible=true` with `recommendedAction=open_source_repair`: open one GitHub repair carrier keyed by `fingerprint`, add a regression guard, patch only the owned paths, and require exact-head green PR CI before merge.
- `circuitOpen=true` / `humanReviewRequired=true`: do not patch-loop. Stop automatic source repair for that fingerprint and require human diagnosis or runtime evidence.

## GitHub repair carrier workflow

1. Preserve the failing tool, error code, lane, ticket id, fingerprint, build SHA, runtime version, and reproducible project checkpoint. Do not include secrets or raw sensitive arguments.
2. Reuse an existing open GitHub repair carrier for the same fingerprint; otherwise create one Reservation v2 carrier with non-overlapping `Expected-Paths`.
3. Write the regression/preflight first, then implement the smallest source repair. Do not add a new MCP tool merely to expose repair state.
4. Run repository preflight and exact-head CI. Never force-push, bypass branch protection, bypass confirmation, bypass the process-global writer, or treat skipped licensed BricsCAD evidence as success.
5. After merge and runtime update, retry the original project checkpoint. If the same repairable fingerprint reaches the circuit threshold, stop and escalate.

The in-process ledger is bounded and deduplicated, so many chats or agents reporting the same normalized failure converge on one repair identity rather than spawning unbounded repair work.
