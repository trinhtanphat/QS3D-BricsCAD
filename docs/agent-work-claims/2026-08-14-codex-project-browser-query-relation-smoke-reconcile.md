# Work claim — Project Browser query relation smoke reconciliation

- Status: `COMPLETED`
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

## Completion record

- Claim PR `#1220` merged as `d8e75edc4918a223a6450eb401efc0217bbf1267`.
- Test commit `3a87215ba` merged through PR `#1222` as `de4cb8e4ab280f3b7ce1406d9775b48525555b71`.
- The one reserved smoke now verifies padded Family/Floor/Zone setter writes store their canonical values and remain matched by the filtered query. Whitespace-only Family input stores the supported empty optional relation and also remains matched. Canonical and case-insensitive reference controls remain intact.
- Core Release build PASS with `0 warnings / 0 errors`. Project Browser family/category integrity, Project Browser workspace schema and identity ambiguity gates PASS; generic and manual-only policy gates PASS. Missing, filtered-unmatched, invalid-filter, unassigned and duplicate-identity fixtures remain present and unchanged.
- Full Core smoke advances beyond this query smoke and stops at the next independent fixture drift: `ProjectBrowserReferenceCanonicalitySmoke` still expects padded Floor/Zone setter values to survive and be rejected by the unfiltered planner. This lane did not edit or absorb that blocker.
- No production, focused gate, LOCAL, issue `#1005`, BricsCAD/native/private data or GitHub Actions surfaces were changed/run.
