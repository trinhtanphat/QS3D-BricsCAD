# Work claim — Regeneration work profile DTO bounded collections

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-profile-dto-cardinality`
- Registered: `2026-08-12T00:09:00+07:00`
- Baseline main SHA: `48591255a4dda245bed178f001e89a415bb20f8a`
- Priority: P1 — public work-profile construction must enforce its declared project cardinality before materializing arbitrary enumerables.

## Confirmed defect

`RegenerationWorkProfile` is a public DTO constructor that already receives and validates `projectElementCount`, but then eagerly calls `ToList()` on `targetElementIds`, `items`, and `categories`. All three collections are summaries/scopes of that project and therefore can never validly contain more entries than `projectElementCount`. A caller can currently provide an excessively large or non-terminating enumerable and bypass the DTO's otherwise strict invariant checks before construction returns.

`projectElementCount` is an exact natural upper bound for all three collections; no new product limit is required.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfileCollectionBoundSmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- `TargetElementIds`, `Items`, and `Categories` are materialized with a stop-at-`projectElementCount + 1` guard.
- Null enumerable behavior remains `ArgumentNullException`.
- Collections at or below project cardinality preserve ordering and values.
- Existing category/scope/dirty/count validation and profiler algorithms remain unchanged.

## Coordination

The prior completed profile DTO integrity claim addressed undefined enums, dirty bits, and negative metrics only. The just-completed profiler subset-target claim addresses `ProfileSubset(...)` input, not direct public DTO construction. No recent exact collection-cardinality claim was found.

## Validation plan

- Add sentinel enumerables independently for target IDs, work items, and category summaries to prove each stops at the project cardinality boundary.
- Verify exact-cardinality finite collections remain accepted and preserve order.
- Re-fetch source before update, SHA-guard writes, inspect exact diffs, and close this claim.
- No GitHub Actions dispatch; no executable .NET or BricsCAD V25 runtime PASS claim from this hosted environment.

## Completion condition

Public regeneration work-profile construction cannot eagerly enumerate any project-scoped collection beyond the declared project cardinality, regression is on `main`, and this claim is closed.
