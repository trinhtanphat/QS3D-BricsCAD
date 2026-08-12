# Work claim — Semantic schedule placement precision-safe overlap

- Status: `ACTIVE`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Baseline main SHA: `fe8e690f9e432ef9b5661a2fa2f422a1e4457e12`
- Priority: evidence-driven Core documentation geometry hardening

## Proven defect

`SemanticSchedulePlacementPlanner` evaluates paper bounds and occupied-rectangle conflicts with endpoint additions such as `X + Width`. At large finite coordinates, a positive finite width can be smaller than the local double ULP, so `X + Width == X`. For example, an existing view around `X = 1e16` with `WidthMm = 1` can retain a positive semantic width while endpoint arithmetic collapses. The current `Conflicts()` can then report no overlap even when a generated schedule occupies the same origin, violating the planner contract that schedules must not overlap existing sheet content.

Derived edge candidates have the same failure mode: `region.X + region.Width + gap` can collapse back to `region.X`, allowing an overlap candidate to be considered.

## Reserved scope

Make schedule placement bounds/conflict arithmetic robust against finite endpoint precision collapse. Use separation/remaining-distance comparisons rather than lossy endpoint sums, and fail closed when a positive occupied extent/gap cannot advance an edge-derived candidate to a representably greater coordinate. Preserve ordinary placement ordering, margins, gaps, input bounds, and left/top-margin behavior.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSchedulePlacementPlanner.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSchedulePlacementPrecisionSmoke.cs`
- this claim file

## Excluded scope

- No changes to `SemanticSheetPlanner`, schedule catalog, native BricsCAD placement, UI, exporters, or other documentation planners.
- No GitHub Actions dispatch and no licensed BricsCAD runtime claim.

## Validation plan

- A large finite sheet containing an existing view whose positive width is below the local ULP must fail closed rather than return an overlapping schedule placement.
- A same-origin conflict at large coordinates must remain detectable via precision-safe separation arithmetic.
- Ordinary existing schedule-placement behavior remains unchanged in a normal-coordinate control.
- Re-fetch current `main` and target blob before product write; never force-push.

## Collision check

Recent schedule-placement claims for bounded enumeration and left/top margins are `COMPLETED`; no current precision-overlap claim was found immediately before registration.

## Completion condition

Current `main` cannot publish a schedule placement that overlaps occupied sheet content merely because finite endpoint additions lost positive rectangle extent, focused CAD-independent regression coverage is present, and this claim is `COMPLETED`.
