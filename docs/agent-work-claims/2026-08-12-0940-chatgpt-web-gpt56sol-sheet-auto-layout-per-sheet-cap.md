# Agent Work Claim — Semantic Sheet Auto Layout Per-Sheet Placement Cap

- Status: `ACTIVE`
- Owner: ChatGPT web / GPT-5.6 Sol
- Started: 2026-08-12 09:40 +07:00
- Start commit observed: `ce1481fbb4a9db57f3bf5efc42189341f86ac8b7`
- Related roadmap/issue: Documentation layer / semantic sheet auto layout

## Purpose

Make automatic sheet pagination honor the semantic sheet contract of at most 128 view placements per sheet, even when additional small views still fit geometrically on the same paper.

## Allowed scope

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- focused `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutSmoke.cs` regression coverage
- this claim file

## Excluded scope

- semantic sheet persistence/schema changes
- manual `SemanticSheetPlanner` placement validation semantics
- native BricsCAD Layout/PaperSpace mutation
- quantity/reporting/UI/ribbon/updater/licensing
- BricsCAD runtime qualification

## Proven defect

`SemanticSheetAutoLayoutPlanner` accepts up to 10,000 requested views and `PageState.TryPlace(...)` currently limits a page only by geometry. `SemanticSheetDefinition`, however, snapshots at most `SemanticSheetPlanner.MaxPlacements` (128) placements and fails closed when a 129th entry is present. Therefore 129 sufficiently small views can all be accepted into one auto-layout page and then fail late while materializing the sheet instead of producing a second sheet.

## Contract

- A `PageState` that already contains 128 placements must reject further placement attempts so the outer auto-layout loop opens the next page.
- Preserve existing geometric packing, deterministic item ordering and 10,000-item request bound.
- Add focused smoke coverage with 129 tiny unique views that all fit geometrically on one large page; require two sheets with placement counts 128 and 1.

## Overlap note

Recent auto-layout work covered bounded available-view enumeration and read-only result exposure and is completed. No active/recent claim for 128-per-sheet auto-layout pagination was found. Re-read latest `main`, claim, target source and smoke immediately after registration before implementation.
