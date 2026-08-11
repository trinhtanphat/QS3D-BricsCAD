# Agent Work Claim — Semantic Sheet Auto Layout Available View Bound

- Status: `ACTIVE`
- Owner: ChatGPT remote agent
- Started: 2026-08-12 00:06 +07:00
- Start commit observed: `cdc766356e961706dc77a58b6249223f1a8c53f5`
- Related roadmap/issue: Documentation layer / #77

## Purpose

Complete the existing bounded-enumeration contract of `SemanticSheetAutoLayoutPlanner` by bounding its `availableViews` input to the same 10,000-view catalog ceiling already enforced by `SemanticViewPlanner` and the auto-layout request bound.

## Allowed scope

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- focused `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutSmoke.cs` regression
- an existing focused static preflight for auto-layout if one exists and needs tightening
- this claim file

## Excluded scope

- native Layout/PaperSpace/Viewport mutation
- semantic view schema/persistence redesign
- quantity/reporting/UI/updater/licensing work
- local V25 qualification

## Proven defect

`SemanticSheetAutoLayoutPlanner.MaterializeItemsBounded(...)` already stops at 10,001 requested items, and `SemanticViewPlanner.BuildCatalog(...)` caps semantic view catalogs at 10,000. However `SemanticSheetAutoLayoutPlanner.BuildViewIndex(...)` still enumerates arbitrary `IEnumerable<SemanticViewPlan> availableViews` without a count guard, allowing unbounded enumeration and dictionary growth before layout begins.

## Contract

- Stop enumerating `availableViews` and fail closed when a 10,001st view is observed.
- Preserve existing duplicate-id/null/id validation and the 10,000-view behavior.
- Add a focused over-enumeration regression that throws if the planner reads beyond the first over-bound entry.

## Overlap note

Existing auto-layout work already hardened the request-item enumerable; this claim is deliberately limited to the separate `availableViews` enumerable. Recent concurrent work observed targets grid, wall, regeneration and other unrelated bounds. Re-read target files before every write and never force-push shared `main`.