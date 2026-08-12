# Work claim — Semantic sheet precision-safe placement geometry

- Status: `COMPLETED`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Baseline main SHA: `afdf4d79adab062c1f77b54e31c35e5bd1459dd5`
- Claim commit: `a7ae1a8185a2301acce444b9299df1971bf87e81`
- Source fix: `542bbb4b1e1c9191282642ce2405688fba3a7855`
- Regression: `ce9fe36d692df48aeed884788444cb7109fe14d9`
- Priority: evidence-driven Core documentation geometry hardening

## Proven defect

`SemanticSheetPlanner.BuildValidated()` checked placement bounds with `Xmm + WidthMm` / `Ymm + HeightMm` and `Overlaps()` checked rectangle endpoints with the same addition pattern. At a large finite origin such as `1e16`, a positive finite `1 mm` extent can be lost (`1e16 + 1 == 1e16`). Two different view placements could therefore share the same large origin and positive sizes while `Overlaps()` reported false, allowing an invalid overlapping sheet plan to be published. A positive placement starting exactly at a large sheet edge could likewise pass bounds when its endpoint addition collapsed.

This lane is upstream and independent from the completed schedule-placement precision lane: it governs semantic view placements while the previous lane governs schedules placed around an already-built sheet.

## Implemented scope

- Placement bounds now compare positive extent against remaining paper distance instead of adding extent to a large origin.
- Pairwise overlap checks now compare coordinate separation with the leading rectangle extent on each axis, so same-origin positive rectangles remain overlapping even if endpoint addition would collapse.
- Normal-coordinate placement ordering, uniqueness rules, catalog bounds, public records and sheet output contracts remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/SemanticSheetPrecisionSmoke.cs` is auto-registered and covers:

- two distinct views at the same `X = 1e16` with `WidthMm = 1`, which must fail closed as overlapping;
- a placement starting at a large sheet right edge where `sheetWidth + 1 == sheetWidth`, which must fail closed as out of bounds;
- ordinary normal-coordinate non-overlapping placements remaining valid and deterministically ordered.

The source commit readback shows only the intended bounds and overlap helper changes. After concurrent commits advanced `main` to `60f0cd12c89873f3d2c2808965382dbb44d00675`, source readback still contained the precision-safe implementation with blob `36636a4a325ba500bbddd248558de4d069fe511d`.

Combined status for the regression commit returned no statuses. No GitHub Actions were dispatched and no local .NET or licensed BricsCAD runtime PASS is claimed from this hosted session.

## Excluded scope

- No changes to `SemanticSchedulePlacementPlanner`, view generation, native BricsCAD placement, UI, exporters, or other documentation planners.
- No GitHub Actions dispatch; no licensed BricsCAD runtime claim.

## Collision check

No semantic-sheet precision/overlap claim existed before registration. The preceding schedule-placement precision claim explicitly excluded `SemanticSheetPlanner`.

## Completion condition

Satisfied: current `main` cannot accept overlapping/out-of-bounds semantic view placements merely because finite endpoint addition lost a positive extent, focused CAD-independent regression coverage is present, and this claim is `COMPLETED`.
