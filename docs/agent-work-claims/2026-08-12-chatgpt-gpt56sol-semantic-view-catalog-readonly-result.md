# Work claim — semantic view catalog read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-view-catalog-readonly`
- Registered: `2026-08-12T09:31:00+07:00`
- Baseline main SHA: `cd398601829106d2e4c1dc9b90398cf21297b14a`
- Priority: `Documentation/Core API integrity discovered during requested continue-all review.`

## Confirmed defect

`SemanticViewPlanner.BuildCatalog()` advertises `IReadOnlyList<SemanticViewPlan>` but returns a sorted array. Arrays implement `IList<T>` and permit indexed replacement, so callers can replace published catalog entries after duplicate identity validation and deterministic ordering.

## Reserved scope

Make only the outer `BuildCatalog()` result genuinely read-only while preserving view filtering, catalog ordering, duplicate ID/name validation, input bounds, `SemanticViewPlan` contents, and single-view `Build()` behavior. Add a focused isolated smoke regression.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs` — only `BuildCatalog` returned collection wrapper
- `tests/QS3D.Core.SmokeTests/SemanticViewCatalogReadonlySmoke.cs` — new focused regression

## Coordination

Earlier semantic-view catalog work bounded lazy definition enumeration. This claim does not modify bounds, filters, category/reference semantics, or project element indexing.

## Validation plan

- Build a two-view catalog and preserve deterministic name ordering.
- Cast the published result to `IList<SemanticViewPlan>` and require indexed replacement, Add, and Remove to throw `NotSupportedException`.
- Verify rejected mutations preserve count/order.
- Re-read latest main and source blob before write; no GitHub Actions; no build/runtime PASS claims.

## Completion condition

Claim is on main before source mutation; source and regression commits are pushed with exact SHA guards; claim closes `COMPLETED` with evidence and no dangling ownership.
