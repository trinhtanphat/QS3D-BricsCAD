# Work claim — semantic sheet catalog read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-sheet-catalog-readonly`
- Registered: `2026-08-12T09:24:00+07:00`
- Baseline main SHA: `13ef47e7a637a4b4b9904f07f66daff29f9f161d`
- Priority: `Documentation/Core API integrity discovered during requested continue-all review.`

## Confirmed defect

`SemanticSheetPlanner.BuildCatalog()` advertises `IReadOnlyList<SemanticSheetPlan>` but returns a sorted array. Arrays implement `IList<T>` and allow indexed replacement, so callers can replace published catalog entries after duplicate-ID/number validation and deterministic ordering.

## Reserved scope

Make only the outer `BuildCatalog()` result genuinely read-only while preserving catalog ordering, view-index reuse, duplicate validation, input bounds, plan objects, placements, and single-sheet `Build()` behavior. Add a focused isolated smoke regression.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetPlanner.cs` — only `BuildCatalog` returned collection wrapper
- `tests/QS3D.Core.SmokeTests/SemanticSheetCatalogReadonlySmoke.cs` — new focused regression

## Coordination

The prior semantic-sheet view-index reuse lane is `COMPLETED`; earlier lanes bounded definitions/views/placements. This claim does not modify those algorithms or capacities.

## Validation plan

- Build a two-sheet catalog and preserve deterministic number ordering.
- Cast the published result to `IList<SemanticSheetPlan>` and require indexed replacement, Add, and Remove to throw `NotSupportedException`.
- Verify rejected mutations preserve count/order.
- Re-read latest main and current source blob immediately before write; no GitHub Actions; no build/runtime PASS claims.

## Completion condition

Claim is on main before source mutation; source and regression commits are pushed with exact SHA guards; claim closes `COMPLETED` with evidence and no dangling ownership.
