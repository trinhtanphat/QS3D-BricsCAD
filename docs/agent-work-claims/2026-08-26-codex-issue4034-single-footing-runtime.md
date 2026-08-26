# LOCAL-020 Móng đơn V25/V26 runtime qualification claim

- Status: ACTIVE / LOCAL_ONLY
- Lane-Key: `issue-4034-single-footing-runtime`
- Owner/session: `codex-01a03be6`
- Issue: `#4034`
- Branch: `agent/codex/issue4034-single-footing-v25-v26-runtime`
- Exact baseline/candidate: `a0e2ba70fdfe5ab1705e0a2534d0cb1d8e961cf9`
- Source feature: issue `#4019`, merged PR `#4021`, merge commit `0d489713ce3b302845a53d185bd02441a7341a89`

## Reserved scope

Run the bounded licensed BricsCAD V25/V26 runtime matrix for the merged Móng đơn workflow: Workspace tree selection, six-value Add dialog, Family creation/activation, `H2=0` box placement, `H2>0` prism-plus-frustum placement, repeated center picks until Enter/Esc, semantic/native ownership, cancel/no-residue, non-Móng-đơn Foundation routing and save/reopen continuity.

V25 and V26 use their matching assemblies and receive separate verdicts. Raw evidence remains ignored under `artifacts/`; only sanitized aggregate evidence may enter Git.

## Exclusions and stop rules

- No production source implementation or ordinary source bug fix in this local lane.
- No write or merge to `main`.
- No GitHub Actions dispatch, rerun or cancellation.
- Host contention or unavailable prerequisites produce `NO_RESULT`, never `LOCAL_PASS`.
- A general source defect is handed off with the smallest sanitized reproduction before any source change.
- `HOST_RELEASED` is justified only after Loader/DemandLoad restoration and stable zero BricsCAD processes.

## Validation plan

1. Push this registration before starting BricsCAD.
2. Reconfirm exact candidate, clean worktree, matching installed hosts and exclusive PID state.
3. Run source/build/runtime identity gates and the bounded interactive matrix.
4. Record exact assembly hashes and sanitized marker results.
5. Clean scoped residue, restore host state, close all test processes, then publish the final exact-SHA disposition by PR.
