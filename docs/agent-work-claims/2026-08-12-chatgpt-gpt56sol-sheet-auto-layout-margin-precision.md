# Work claim — Semantic sheet auto-layout margin precision

- Status: `COMPLETED`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Baseline main SHA: `1d490e927fddcc4e15d44a17d34719b8a823d227`
- Claim commit: `9ada91bbe8c3cc0eb0c6b15981496f60454e712e`
- Source fix: `f14847c036496fede1056deb3765d8618a6d1deb`
- Regression: `0da04265ca480a2ecb5e017c052a26b074d2a4b7`
- Priority: evidence-driven Core documentation layout hardening

## Proven defect

`SemanticSheetAutoLayoutPlanner` computed usable paper size with chained subtraction such as `PaperWidthMm - MarginLeftMm - MarginRightMm`. At a large finite paper width like `1e16`, a positive `1 mm` right margin is below the local double ULP, so subtraction can return the unchanged paper width. A full-width view was then accepted and emitted to `SemanticSheetPlanner`, which correctly fits it inside the physical paper but has no knowledge of the auto-layout margin contract. The generated layout therefore consumed the configured right margin instead of failing closed.

The same risk applied to bottom margin / reserved-bottom subtraction. This is distinct from the completed semantic-sheet bounds lane because the sheet planner validates physical paper bounds, not auto-layout reserved margins.

## Implemented scope

- Usable width now retreats the right and left paper boundaries one configured margin at a time.
- Usable height now retreats bottom margin, reserved-bottom area and top margin one configured amount at a time.
- Any positive finite configured amount that does not produce a representably smaller boundary fails closed instead of silently disappearing.
- Item ordering, page packing, cursor behavior, per-sheet cap, numbering and result construction remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutMarginPrecisionSmoke.cs` is auto-registered and covers:

- `PaperWidthMm = 1e16`, `MarginRightMm = 1`, full-width view => fail closed;
- `PaperHeightMm = 1e16`, `ReservedBottomMm = 1`, full-height view => fail closed;
- ordinary 200 x 100 mm paper with 10 mm margins still places the first 50 x 30 mm view at `(10, 10)`.

Source commit readback shows only the intended usable-size calculation and `RetreatEdge` helper. Combined status for the regression commit returned no statuses. `main` at regression readback was `0da04265ca480a2ecb5e017c052a26b074d2a4b7`.

No GitHub Actions were dispatched and no local .NET or licensed BricsCAD runtime PASS is claimed from this hosted session.

## Excluded scope

- No changes to `SemanticSheetPlanner`, `SemanticSchedulePlacementPlanner`, pagination policy, numbering, native BricsCAD placement, UI, or exporters.
- No GitHub Actions dispatch; no licensed BricsCAD runtime claim.

## Collision check

Recent auto-layout lanes for enumeration bounds, per-sheet caps, readonly results and number-prefix length were `COMPLETED`; no auto-layout margin-precision claim existed before registration.

## Completion condition

Satisfied: current `main` cannot silently consume a positive configured auto-layout margin/reserved area because floating-point subtraction lost it, focused CAD-independent regression coverage is present, and this claim is `COMPLETED`.
