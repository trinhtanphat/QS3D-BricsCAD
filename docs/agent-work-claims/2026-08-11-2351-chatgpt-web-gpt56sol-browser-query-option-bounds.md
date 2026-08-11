# Work claim — ProjectBrowserQueryOptions bounded input enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-query-option-bounds`
- Registered: `2026-08-11T23:51:00+07:00`
- Baseline main SHA: `9fd71f120b6460c42587a675194497a88d970d91`
- Priority: P1 — public query-option construction must not bypass the browser's declared filter cardinality guards.

## Confirmed defect

`ProjectBrowserQueryOptions` currently materializes `categories`, `floorIds`, and `zoneIds` with `new List<T>(IEnumerable<T>)`. The later `ProjectBrowserQueryPlanner.Build(...)` path limits floor/zone filters to 10,000 and validates categories, but those limits run only after the options object already exists. A very large or non-terminating enumerable can therefore consume unbounded time/memory inside the public options constructor and never reach the planner guards.

This is the same resource-boundary class the repository is hardening elsewhere: validation after eager unbounded materialization is not an effective bound.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryOptionsBoundSmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- Constructor materialization of category, floor-id, and zone-id filters is bounded before appending each next item.
- Category input is bounded by the number of currently defined `ElementCategory` values; floor/zone filters retain the planner's 10,000-entry contract.
- `null`, normal finite inputs, ordering, duplicates, and downstream semantic validation remain otherwise unchanged.
- No Project Browser UI/native behavior changes.

## Coordination

Recent ACTIVE work covers grid spatial enumeration, interchange null validation, ProjectSession recovery, dependency-impact bounds, and other unrelated lanes. No recent claim was found for `ProjectBrowserQueryPlanner.cs` / `ProjectBrowserQueryOptions` bounded construction.

## Validation plan

- Add an auto-registered Core smoke proving category input above the defined-category count and floor/zone input above 10,000 fail in the constructor.
- Assert exact-limit finite inputs remain accepted and preserve ordering.
- Re-fetch the source before write, use its current blob SHA, review published diffs, and close this claim with exact SHAs.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Completion condition

No public Project Browser query-option collection can be eagerly materialized beyond its resource contract before planner validation, focused regression is on `main`, and this claim is closed.
