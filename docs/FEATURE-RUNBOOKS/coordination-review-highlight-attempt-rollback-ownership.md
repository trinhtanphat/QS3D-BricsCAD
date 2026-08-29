# Coordination review highlight-attempt rollback ownership

## Scope

Issue #4545 closes a native lifecycle hole in V25 Coordination review Highlight failure handling. A multi-entity Highlight attempt can partially mutate native entity highlight state before a later entity or transaction step fails. Existing compensation was best-effort but did not preserve ownership for IDs whose Unhighlight compensation could not be confirmed.

## Required invariant

Successful Highlight still publishes `_highlighted` only after the native transaction commits. Failed Highlight keeps the original exception as the primary failure, compensates every attempt-local successful native highlight, and transfers every unconfirmed compensation ID into persistent `_highlighted` ownership before rethrow. A compensation transaction failure is treated conservatively: all attempt-local IDs remain owned for later cleanup.

`ClearHighlight()` remains the retry executor. It removes only IDs whose Unhighlight succeeds and whose cleanup transaction commits. Destroyed-document teardown is the only explicit whole-set abandon path.

## Repository-safe validation

Run the auto-discovered source guards, including:

- `scripts/preflight-coordination-review-highlight-rollback.py`
- `scripts/preflight-coordination-review-highlight-attempt-rollback-ownership.py`
- `scripts/preflight-coordination-review-highlight-cleanup-per-entity-ownership.py`
- `scripts/preflight-coordination-review-transient-cleanup-ownership.py`

Then run applicable shared CI for the exact branch head and protected PR candidate. Hosted validation may prove source ordering and managed-reference compile only; it is not licensed BricsCAD runtime evidence.

## Runtime boundary

No `LOCAL_PASS` is claimed by this lane. If a future local matrix explicitly exercises native Highlight/Unhighlight failure injection, bind any evidence to that exact integrated source SHA and host identity.
