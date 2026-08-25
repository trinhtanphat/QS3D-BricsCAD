# LOCAL-012 exact-SHA pre-runtime handoff

- Carrier issue: `#3936`
- Lane-Key: `issue-3936`
- Canonical branch: `agent/interactive-20260825-01a03821/issue-3936-local012-workspace-ui`
- Tested source SHA: `dffb7e334f997a981a9d918c23b67592e232d61f`
- Then-current main checked for the affected files: `ff6c298d33da2aec5e3e0f38503571899e185686`
- Source-fix handoff: `#3946`
- Outcome: `BLOCKED_SOURCE_FIX / NO_RESULT`

## Result boundary

The clean pinned V25 qualification passed the exact-SHA/clean-tree check, the manual-only CI-policy check and the generic source preflight. It then failed in `Aggregate feature preflights` before Core build, offline WPF smoke, adapter build, `NETLOAD` or any interactive runtime row.

The aggregate traceback terminated in `scripts/preflight-preflight-all-discovery.py` with the sanitized assertion:

```text
AssertionError: timeout reason must remain visible
```

A clean focused rerun of that discovery guard reproduced the same assertion. A separate read-only reconstruction of its 50 ms child-timeout scenario produced the expected `preflight-b-slow.py timeout` diagnostic once, so the guard is timing-sensitive on this supported Windows environment. That observation is diagnostic evidence for the source owner, not permission for the local worker to patch the production runner or relax the gate.

The current-main delta observed during triage did not change `scripts/preflight-all.py` or `scripts/preflight-preflight-all-discovery.py`. Issue `#3946` records the bounded remote/source repair and deterministic acceptance criteria.

## Qualification state

| Gate or runtime scope | Result |
| --- | --- |
| Exact Git SHA / clean tree | `PASS` |
| Manual-only CI policy | `PASS` |
| Generic source preflight | `PASS` |
| Aggregate feature preflights | `FAIL` (pre-runtime gate) |
| Source/Core build | `NOT COMPLETED` |
| Offline WPF smoke | `NOT RUN` |
| V25 adapter build | `NOT RUN` |
| `NETLOAD` / hosted runtime | `NOT RUN` |
| Full LOCAL-012 interactive matrix | `NOT RUN` |
| Workspace Foundation `#1760` rows | `NOT RUN` |
| Customer/release qualification | `NOT QUALIFIED` |

No plugin SHA-256 exists for this attempt because the build was never reached. No BricsCAD process was launched. Therefore this attempt supplies no product PASS/FAIL for CAD-selection bridging, PICKFIRST, stale/deleted/ambiguous IDs, Family/Instance scope, live-Family Reset, cache/document lifecycle, browser filtering/grouping/paging, save/reopen, Unicode/HiDPI, dedicated Properties, or the restored owner-requested Foundation `#1760` interaction rows.

## Resume condition

Do not retry the unchanged candidate. After `#3946` is fixed and merged, refresh current `main`, create or reconcile the same `issue-3936` carrier to that new exact clean SHA, rerun the canonical pinned V25 qualification, and only then continue the complete licensed/modeless LOCAL-012 matrix. A later runtime failure must be reported separately as sanitized exact-SHA product evidence; source/static success must never be promoted to `LOCAL_PASS`.

The raw qualification report remains under ignored local `artifacts/`. It is not committed because it contains machine-specific paths. This handoff intentionally records only allow-listed SHA, status, gate label and sanitized assertion data.
