# Work claim — Project Browser query relation smoke reconciliation

- Status: `ACTIVE`
- Agent: `codex-project-browser-query-relation-smoke-reconcile-20260814` (`/root/fix_source_reconcile_desync`)
- Registered: `2026-08-14T15:38:00+07:00`
- Baseline main SHA: `7fdbf55506ed8d3c1029facf905a6d6221bfd395`
- Priority: next deterministic Core full-smoke blocker after completed relation setter persistability

## Confirmed fixture drift

`ProjectBrowserQueryReferenceCanonicalitySmoke` still expects filtered queries to reject padded Family/Floor/Zone setter assignments and whitespace-only Family assignment. Completed `ProjectElement` source now trims every supported optional relation setter before the query planner can inspect it; whitespace-only input becomes the empty optional relation, which Project Browser supports as unassigned.

Canonical and case-varied relations remain reachable and valid. Missing references, family/category mismatch, filtered-unmatched integrity, invalid filter IDs, unassigned grouping and duplicate semantic identity remain covered by the existing Project Browser planner/query smokes and are not changed by this lane.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryReferenceCanonicalitySmoke.cs`
- this claim only

Replace only the four stale rejection expectations with assertions that padded supported writes persist canonical values and retain the filtered query match, while whitespace-only Family persists empty and retains the supported unassigned query match. Preserve the canonical and case-insensitive reference controls.

## Explicit exclusions

- no production query, relation writer, persistence/schema or health behavior changes;
- no focused gate change because no focused gate references this smoke or the stale padded-setter expectation;
- no LOCAL runner/probe/docs, issue `#1005`, BricsCAD/native/private data, GitHub Actions, release or packaging work;
- no edits to other Project Browser smokes; report the next full-smoke blocker rather than absorbing it.

## Validation

- Core Release build and full deterministic Core smoke;
- focused Project Browser family/category integrity and workspace/query gates available in `scripts/`;
- generic and manual-only policy gates;
- readback that missing, case-insensitive, invalid-filter, unmatched-corruption, unassigned and duplicate-identity query coverage remains present.
