# MCP mutation coordinator audit isolation

## Scope

This runbook covers diagnostic callback isolation inside `McpCadMutationCoordinator`, the process-global writer, native-command and interactive-modal coordination boundary.

## Failure mode

Audit sinks are optional diagnostics. They must never participate in authoritative writer state transitions. Previously direct audit callbacks ran after or during publication of writer leases, mutation operation IDs, interactive modal ownership and native-command reservations/terminal cleanup. A throwing sink could therefore report an operation as failed after state had already changed, strand lease/pending ownership until expiry/reset, or interrupt native event cleanup/ledger publication.

## Required source contract

- every coordinator diagnostic passes through one fail-soft `SafeAudit` helper;
- `SafeAudit` catches sink failures and never rethrows into writer/native event paths;
- no direct `audit?.Invoke`, `pending.Audit?.Invoke` or `_audit?.Invoke` remains in the coordinator;
- lease ownership, process-global `MutationGate`, prepared/native reservations and terminal-event detach rules remain unchanged;
- interactive modal release remains finally-bound;
- mutation-scope identity restoration precedes writer release;
- native terminal bookkeeping and ACK-ledger synchronization remain authoritative and are not skipped because diagnostics fail;
- no automatic native command replay/retry is introduced.

## Deterministic validation

Run:

```powershell
python scripts/preflight-mcp-mutation-coordinator-audit-isolation.py
```

Protected exact-head `preflight` and `core` must both pass before merge.

## Runtime boundary

Static/source guards, deterministic smoke tests and trusted-reference V25 compilation are REMOTE_SAFE. Injecting a throwing diagnostic sink through real BricsCAD command/modal event lifetimes requires a licensed local host and remains LOCAL_ONLY / NO_RESULT until executed. Remote CI is not `LOCAL_PASS` evidence.
