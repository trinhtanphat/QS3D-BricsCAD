# Agent work claim — RateBook effective-time uniqueness index

Status: COMPLETED
Agent: Codex `/root/audit_performance_next`
Baseline: `d521a3f95ee0ed80f12335e2f6affa59ce21fa9d`
Branch: `agent/codex/issue81-ratebook-timestamp-index-20260815`
Implementation branch: `agent/codex/issue81-ratebook-timestamp-index-impl-20260815`
Related issue: #81

## Defect

`RateBook` currently detects ambiguous rate items by linearly scanning every
previous item in the same cost-code/unit/currency scope for each new item. A
single scope containing `N` distinct effective timestamps therefore performs
`N(N-1)/2` timestamp comparisons before the existing deterministic sort. This
is a repository-safe structural large-catalog hotspot; it is not a licensed
BricsCAD runtime benchmark claim.

## Exact scope

- Replace only the nested per-scope effective-timestamp scan in
  `src/QS3D.Core/Cost/RateBook.cs` with an indexed uniqueness reservation.
- Preserve case-insensitive scope identity, duplicate-rate-id validation, the
  existing ambiguous-effective-time exception type/message, detached immutable
  item snapshots, deterministic item ordering, and all `Resolve` behavior.
- Extend the existing registered `RateBookSmoke` coverage and add one focused
  auto-discovered source gate that prevents the nested timestamp scan from
  returning.

Expected files:

- `src/QS3D.Core/Cost/RateBook.cs`
- `tests/QS3D.Core.SmokeTests/RateBookSmoke.cs`
- `scripts/preflight-ratebook-effective-time-index.py`
- this claim record

## Exclusions

- No change to `RateBook.Resolve` lookup complexity or semantics.
- No new rate-item count cap and no change to RateItem/CostCode token, numeric,
  timestamp, currency, or version contracts.
- No changes to EstimateLine, frozen estimate projections, persistence, UI,
  BricsCAD/native/runtime, private data, release, CI, or workflow files.
- No overlap with ACTIVE/BLOCKED claims or current open PRs.

## Validation plan

- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release.
- Run the complete Core smoke suite.
- Run the focused RateBook effective-time index preflight.
- Run the aggregate remote-safe preflight and report unrelated blockers
  separately without operating GitHub Actions.

## Completion

- Claim PR: #1541; claim merge:
  `563db5ceedb829689e7b33caace82d953cc77e24`.
- Implementation commit:
  `60202d507065dbb167d3ab0e0bebe7b3a72d81b0`.
- Implementation PR: #1546; exact `main` merge:
  `8e99e2a44f1ae9cebf67ca7c165d267cc47ee1e6`.
- Exact-merge root revalidation: focused RateBook effective-time index gate
  PASS; `QS3D.Core` Release build PASS with 0 warnings / 0 errors;
  `QS3D.Core.SmokeTests` Release build PASS with 0 warnings / 0 errors; full
  Core smoke `ALL PASS`.
- The completed slice replaces the quadratic per-scope effective-time scan
  with indexed uniqueness while retaining every reserved exception, snapshot,
  ordering, identity, and `Resolve` boundary.
- Broad performance issue #81 intentionally remains OPEN for its other
  source/runtime work. No GitHub Actions, BricsCAD/native runtime, release, or
  private-data operation was performed by this lane.
