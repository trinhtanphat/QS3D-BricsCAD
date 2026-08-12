# Agent work claim — Semantic Sheet catalog view-index reuse

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Baseline main SHA observed: `73fe36cd3c046421d12cc5215cdf29623b836488`
- Priority: P1 — bounded catalog performance/allocation hardening

## Confirmed defect

`SemanticSheetPlanner.BuildCatalog(...)` already materializes and validates `availableViews` once, but then calls public `Build(definition, views)` for every sheet. Public `Build(...)` materializes the same view list again and rebuilds the same case-insensitive view index on every iteration. At the existing caps of 10,000 sheets and 10,000 views, catalog planning can therefore perform O(sheetCount × viewCount) redundant view enumeration/index allocation even though the validated view set is immutable for the batch.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticSheetPlanner.cs` only for extracting/reusing the already-validated view index
- focused static regression preflight
- this claim file

## Contract

1. Public `Build(...)` retains existing validation/error behavior and still bounds/validates its `availableViews` input once.
2. `BuildCatalog(...)` materializes and indexes `availableViews` once, then reuses that index for every sheet.
3. Sheet geometry, overlap, IDs/numbers, title block, placement ordering and catalog bounds remain unchanged.
4. No native Layout/Viewport/Table behavior or BricsCAD V25/V26 qualification claim changes.

## Validation/closure

Use a private core path receiving the validated view index plus a focused source preflight preventing `BuildCatalog(...)` from calling public `Build(...)` in its loop. No GitHub Actions or licensed BricsCAD runtime PASS claim.
