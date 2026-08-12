# Work claim — Semantic sheet auto-layout margin precision

- Status: `ACTIVE`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Baseline main SHA: `1d490e927fddcc4e15d44a17d34719b8a823d227`
- Priority: evidence-driven Core documentation layout hardening

## Proven defect

`SemanticSheetAutoLayoutPlanner` computes usable paper size with chained subtraction such as `PaperWidthMm - MarginLeftMm - MarginRightMm`. At a large finite paper width like `1e16`, a positive `1 mm` right margin is below the local double ULP, so subtraction can return the unchanged paper width. A full-width view is then accepted and emitted to `SemanticSheetPlanner`, which correctly fits it inside the physical paper but has no knowledge of the auto-layout margin contract. The generated layout therefore consumes the configured right margin instead of failing closed.

The same risk applies to bottom margin / reserved-bottom subtraction. This is distinct from the completed semantic-sheet bounds lane because the sheet planner validates physical paper bounds, not auto-layout reserved margins.

## Reserved scope

Guard usable-width/usable-height subtraction so each positive configured margin/reserved amount must produce a representably smaller boundary. Preserve item ordering, page packing, per-sheet caps, numbering, cursor behavior, and normal-coordinate layouts.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutMarginPrecisionSmoke.cs`
- this claim file

## Excluded scope

- No changes to `SemanticSheetPlanner`, `SemanticSchedulePlacementPlanner`, pagination policy, numbering, native BricsCAD placement, UI, or exporters.
- No GitHub Actions dispatch; no licensed BricsCAD runtime claim.

## Validation plan

- `PaperWidthMm = 1e16`, `MarginRightMm = 1`, full-width view: must fail closed because the positive right margin is not representable by direct subtraction at that scale.
- Equivalent bottom/reserved margin precision loss must fail closed.
- Ordinary finite paper/margin layout remains valid with unchanged first placement coordinates.
- Re-fetch current `main` and source blob immediately before product write; never force-push.

## Collision check

Recent auto-layout lanes for enumeration bounds, per-sheet caps, readonly results and number-prefix length are `COMPLETED`. No auto-layout margin-precision claim was found immediately before registration.

## Completion condition

Current `main` cannot silently consume a positive configured auto-layout margin/reserved area because floating-point subtraction lost it, focused CAD-independent regression coverage is present, and this claim is `COMPLETED`.
