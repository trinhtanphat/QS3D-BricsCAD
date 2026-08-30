# Material Catalog / Project Tools single-instance veto-safe qualification

Status: `SOURCE_PREPARED / LOCAL_ONLY runtime matrix`

This runbook qualifies the native WPF/modeless boundary for `QS3DMATERIALS` and `QS3DPROJECTTOOLS`. Hosted CI can prove source ordering, build integrity and deterministic guards; it cannot claim licensed BricsCAD window behavior.

## Candidate identity

Record one exact pushed Git SHA, ProductVersion, BricsCAD V25 version and adapter DLL SHA-256. Do not mix evidence across binaries.

## Matrix — execute separately for Material Catalog and Project Tools

1. **same-wrapper repeat** — invoke the command twice in one live DWG. The second invocation must activate/reuse the existing window and there must be exactly one live matching manager.
2. **managed-wrapper drift, same native database** — use a safe harness that proves the managed `Document` wrapper changed while native database identity remained the same, then invoke again. Because both windows retain the construction wrapper, the old window must reach terminal `Closed` before replacement is published.
3. **wrapper-drift close veto** — force a legitimate `Closing` veto on the old manager. Reinvocation must fail closed, retain the old publication and never show/publish a second manager.
4. **cross-DWG terminal close** — with the manager open on A, activate B and invoke the same command. A's manager must terminally close before B's candidate is shown/published.
5. **cross-DWG close veto / exception** — veto or throw during old-window close. B must not receive a new manager and the previous publication remains authoritative.
6. **normal close/reopen** — user closes the window normally; subsequent invocation in the same live wrapper must open one fresh manager.
7. **show failure cleanup** — make the host reject/fail the candidate show via the approved harness. The candidate must be closed best-effort and no publication may remain.
8. **source-operation regression** — Material: require an existing project, refresh, safe custom-material edit/apply on a disposable fixture, and prove operations remain bound to the construction wrapper/project. Project Tools: refresh snapshot and dispatch a safe command on a disposable fixture, proving dispatch remains bound to its construction wrapper.
9. **document shutdown** — close/destroy the source DWG with the manager open and confirm constructor-owned `DocumentBoundWindowLifetime` closes the window without stale publication or owned UI/process residue.

## Required evidence

For every cell record candidate SHA/ProductVersion/DLL hash, DWG fixture, command, source managed-wrapper identity, native database identity when safely observable, live matching-window count before/after, whether `Closing`/`Closed` occurred, final active DWG, result and sanitized status/error text.

## Acceptance

`LOCAL_PASS` requires all applicable cells for both managers on the same exact candidate, zero duplicate live managers, fail-closed veto behavior, no cross-DWG mutation/dispatch, normal terminal release/reopen, Material existing-project behavior preserved, and zero owned residue. Hosted build/preflight SUCCESS is only `SOURCE_PREPARED`, never `LOCAL_PASS`.
