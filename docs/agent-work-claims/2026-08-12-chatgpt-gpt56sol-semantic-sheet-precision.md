# Work claim — Semantic sheet precision-safe placement geometry

- Status: `ACTIVE`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Baseline main SHA: `afdf4d79adab062c1f77b54e31c35e5bd1459dd5`
- Priority: evidence-driven Core documentation geometry hardening

## Proven defect

`SemanticSheetPlanner.BuildValidated()` checks placement bounds with `Xmm + WidthMm` / `Ymm + HeightMm` and `Overlaps()` checks rectangle endpoints with the same addition pattern. At a large finite origin such as `1e16`, a positive finite `1 mm` extent can be lost (`1e16 + 1 == 1e16`). Two different view placements can therefore share the same large origin and positive sizes while `Overlaps()` reports false, allowing an invalid overlapping sheet plan to be published.

This is upstream and independent from the completed schedule-placement precision lane: it governs semantic view placements while the previous lane governs schedules placed around an already-built sheet.

## Reserved scope

Make semantic sheet placement bounds and pairwise overlap checks precision-safe using remaining-distance/separation comparisons instead of lossy endpoint sums. Preserve normal-coordinate placement semantics, deterministic ordering, uniqueness limits, and public data contracts.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetPlanner.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSheetPrecisionSmoke.cs`
- this claim file

## Excluded scope

- No changes to `SemanticSchedulePlacementPlanner`, view generation, native BricsCAD placement, UI, exporters, or other documentation planners.
- No GitHub Actions dispatch; no licensed BricsCAD runtime claim.

## Validation plan

- Two distinct view placements at the same `X = 1e16`, each with positive width below the local ULP, must be rejected as overlapping.
- A positive placement extent near the sheet right edge must be checked by remaining distance rather than collapsed endpoint arithmetic.
- Ordinary non-overlapping normal-coordinate placements remain valid and ordered as before.
- Re-fetch current `main` and source blob immediately before write; never force-push.

## Collision check

No recent commit/claim matching semantic-sheet precision or overlap hardening was found immediately before registration. The preceding schedule-placement precision claim explicitly excluded `SemanticSheetPlanner`.

## Completion condition

Current `main` cannot accept overlapping/out-of-bounds semantic view placements merely because finite endpoint addition lost a positive extent, focused CAD-independent regression coverage is present, and this claim is `COMPLETED`.
