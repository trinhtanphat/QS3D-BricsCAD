# Family / Level Manager single-instance veto-safe qualification

Status: `SOURCE_PREPARED / LOCAL_ONLY runtime matrix`

This runbook qualifies the native WPF/modeless boundary for `QS3DFAMILIES` and `QS3DLEVELS`. Hosted CI can prove source ordering, compile integrity and deterministic guards; it cannot claim licensed BricsCAD window behavior.

## Candidate identity
Record one exact pushed Git SHA, ProductVersion, BricsCAD V25 version and adapter DLL SHA-256. Do not mix evidence across binaries.

## Matrix — execute separately for Family Manager and Level Picker
1. **same wrapper repeat** — open command twice in one live DWG; the second invocation must activate/reuse the existing manager and there must be one live manager window.
2. **managed wrapper drift, same native database** — with a safe harness that proves the managed `Document` wrapper changed while native database identity remains the same, invoke again. The old wrapper-bound window must reach terminal `Closed` before replacement is published.
3. **wrapper-drift close veto** — force a legitimate `Closing` veto on the old manager; reinvocation must fail closed and keep exactly the old manager, never publish a second window.
4. **cross-DWG terminal close** — with manager open on A, activate B and invoke the same command. A's manager must terminally close before B's is shown/published.
5. **cross-DWG close veto / exception** — veto or throw during old-window close. B must not receive a new manager and old publication remains authoritative.
6. **normal close/reopen** — user closes the window normally; subsequent invocation in the same live wrapper must open one fresh manager.
7. **source-operation regression** — Family: refresh/create-or-safe-edit/selection assignment only on a disposable fixture. Level: refresh/safe floor operation/selection assignment only on a disposable fixture. Confirm all mutation still binds to the construction wrapper and no operation silently follows another active DWG.
8. **document shutdown** — close/destroy the source DWG with the manager open and confirm the constructor-owned `DocumentBoundWindowLifetime` closes the manager without stale publication or owned process/UI residue.

## Required evidence
For every cell record: candidate SHA/ProductVersion/DLL hash, DWG fixture, command, source managed-wrapper identity, native database identity when safely observable, number of live matching windows before/after, whether `Closing`/`Closed` occurred, final active DWG, result and sanitized error/status text.

## Acceptance
`LOCAL_PASS` requires all applicable cells for both managers on the same exact candidate, zero duplicate live managers, fail-closed veto behavior, no cross-DWG mutation, normal terminal release/reopen, and zero owned residue. A hosted build/preflight SUCCESS is only `SOURCE_PREPARED`, never `LOCAL_PASS`.
