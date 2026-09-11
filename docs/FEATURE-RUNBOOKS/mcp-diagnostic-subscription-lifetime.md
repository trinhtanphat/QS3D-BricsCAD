# MCP diagnostic native subscription lifetime

## Scope
C04 ownership for `McpDiagnosticHub` BricsCAD `Document` command-event monitoring.

## Failure boundary
Native event add/remove accessors can fail after a subset of handlers changed state. Forgetting ownership after partial attach/detach can leave callbacks rooted against stale or disposed document wrappers.

## Required contract
- Publish `DocumentSubscription` ownership before the first fallible native event add.
- Mark each handler may-be-subscribed before its corresponding add accessor.
- Enable callback processing only after the complete four-handler attach succeeds.
- On partial attach failure, disable callbacks and attempt bounded detach.
- Remove registry ownership only after all may-be-subscribed handlers are proven detached.
- `Stop()` disables callbacks first and retains unresolved subscriptions for later cleanup retry.
- A restart/current-document attach must resolve retained cleanup before adding a replacement subscription.
- Authoritative registry identity plus callback-enabled state rejects stale callbacks.
- Never retry or replay CAD mutations as part of diagnostics repair.

## Validation
Run `python scripts/preflight-mcp-diagnostic-subscription-lifetime.py`, Reservation-v2, MCP production-correctness and related native-save/runtime guards.

## Runtime classification
Deterministic/source validation is `REMOTE_SAFE`. Native event-accessor fault injection and disposed-document timing require licensed BricsCAD V25/V26 and remain `LOCAL_ONLY / NO_RESULT` until executed.