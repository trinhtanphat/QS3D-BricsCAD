# Work claim — Semantic schedule placement precision-safe overlap

- Status: `COMPLETED`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Baseline main SHA: `fe8e690f9e432ef9b5661a2fa2f422a1e4457e12`
- Claim commit: `5c5aad404a611b33154ca9cdd3a2e3938df2bebb`
- Source fix: `bd214d3b9c771f624d553dea85d969d497b7b75a`
- Regression: `835c4fdd4600fd27f44f2dbb1cafd4e547713317`
- Priority: evidence-driven Core documentation geometry hardening

## Proven defect

`SemanticSchedulePlacementPlanner` evaluated paper bounds and occupied-rectangle conflicts with endpoint additions such as `X + Width`. At large finite coordinates, a positive finite width can be smaller than the local double ULP, so `X + Width == X`. For example, an existing view around `X = 1e16` with `WidthMm = 1` can retain a positive semantic width while endpoint arithmetic collapses. The old `Conflicts()` could then report no overlap even when a generated schedule occupied the same origin, violating the planner contract that schedules must not overlap existing sheet content.

Derived edge candidates had the same failure mode: `region.X + region.Width + gap` could collapse back to `region.X`, allowing an overlap candidate to be considered.

## Implemented scope

- Paper right/bottom margins now use guarded retreat arithmetic so a positive margin/reserved amount cannot silently disappear at the local floating-point precision.
- Bounds checks use remaining-distance comparisons (`extent <= limit - start`) rather than lossy endpoint sums.
- Occupied-edge candidates advance extent and gap separately and fail closed when either positive contribution cannot produce a representably greater coordinate.
- Rectangle conflicts use separation-versus-extent/gap comparisons instead of `start < otherStart + otherExtent + gap` endpoint sums.
- Ordinary placement ordering, margins, gaps, input enumeration bounds, and left/top-margin filtering remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/SemanticSchedulePlacementPrecisionSmoke.cs` is auto-registered and covers:

- `origin = 1e16`, where `origin + 1 == origin`, with an existing view at that origin and positive width `1 mm`; schedule placement must fail closed rather than publish the same-origin overlap.
- A normal 300 x 200 mm empty-sheet control still places a 50 x 30 mm schedule at the default `(10, 10)` origin.

The source commit diff was read back and contains only the intended arithmetic/bounds/conflict hardening in `SemanticSchedulePlacementPlanner.cs`. Current `main` at regression readback was `835c4fdd4600fd27f44f2dbb1cafd4e547713317`, and the planner source blob remained `e242309a21001652fef52d5205d05663fbf6fd6f` with the intended helpers present.

Combined status for the regression commit returned no statuses. No GitHub Actions were dispatched and no local .NET or licensed BricsCAD runtime PASS is claimed from this hosted session.

## Excluded scope

- No changes to `SemanticSheetPlanner`, schedule catalog, native BricsCAD placement, UI, exporters, or other documentation planners.
- No GitHub Actions dispatch and no licensed BricsCAD runtime claim.

## Collision check

Prior schedule-placement claims for bounded enumeration and left/top margins were `COMPLETED`; no precision-overlap claim existed before registration.

## Completion condition

Satisfied: current `main` cannot publish a schedule placement that overlaps occupied sheet content merely because finite endpoint additions lost positive rectangle extent, focused CAD-independent regression coverage is present, and this claim is `COMPLETED`.
