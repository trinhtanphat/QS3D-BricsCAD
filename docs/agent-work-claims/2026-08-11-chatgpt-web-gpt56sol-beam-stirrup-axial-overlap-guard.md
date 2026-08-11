# Agent work claim — beam stirrup axial overlap guard

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T22:15:00+07:00
- Completed: 2026-08-11T22:21:00+07:00
- Status: `COMPLETED`
- Baseline main SHA: `f674b91cd6948a11786d3bd3ed88084bd20f7b88`
- Priority: source-safe Core/Rebar geometry hardening; prevent adjacent beam stirrup solids from overlapping along the beam axis.

## Confirmed defect

`BeamStirrupLayoutPlanner.Plan(...)` delegated station placement to the generic `LinearRebarLayoutPlanner`, but did not apply a beam-stirrup physical collision rule afterward. Both spacing-driven and count-driven inputs could therefore return adjacent stirrup station centers closer than the stirrup diameter. D8 at 4 mm is a concrete example.

The defect is product-reachable because `BeamStirrupSolidBuilder` creates a separate stirrup solid loop at every returned station. Beam longitudinal reinforcement already applies a specialized one-diameter center-spacing invariant after using the same generic linear planner.

## Implemented

- `8326e4d0e819d65da7f7605f2acdd6cd5ed13e69` — `fix(rebar): reject overlapping beam stirrups`
  - derives the physical stirrup diameter in meters after station planning;
  - rejects any multi-station layout whose actual center spacing is below one stirrup diameter;
  - applies equally to spacing-driven and explicit-count station layouts;
  - preserves the generic linear planner, cover, bend radius, hook, sagitta and native/CAD boundaries.
- `014633a7b79cb333b9770ba69447f64b22ed9e13` — `test(core): guard beam stirrup axial overlap`
  - rejects spacing-driven D8 at 4 mm;
  - rejects count-driven D8 layout with sub-diameter station spacing;
  - retains normal spacing;
  - retains exact one-diameter tangent spacing.

## Validation evidence

- The first production write attempt hit a concurrent SHA race and GitHub rejected it with 409; no force update was used. Source was re-fetched and the patch was then applied on the current blob.
- Re-fetched `src/QS3D.Core/Rebar/BeamStirrupLayoutPlanner.cs` from newer `main` (`b7198497d1858467c4a7c59849285fdf9daa75b4`); the committed one-diameter axial guard remains intact.
- Re-fetched `tests/QS3D.Core.SmokeTests/BeamStirrupAxialOverlapRegressionSmoke.cs` from the same newer tree; both input-mode regressions and valid boundaries remain intact.
- Concurrent main updates were on Floor/Level, documentation and other unrelated lanes and did not overwrite the reserved Rebar surfaces.
- No GitHub Actions workflow was dispatched and no smoke executable run is claimed from this connector-only lane.
- No BricsCAD V25 runtime PASS is claimed; product reachability is source-established by the existing beam stirrup CAD builder.

## Reserved scope honored

- Changed only `BeamStirrupLayoutPlanner.cs`, the focused Core smoke file, and this claim close-out.
- Did not modify `LinearRebarLayoutPlanner`, `BeamStirrupSolidBuilder`, persistence, UI, quantity settings, Floor/Level, documentation, updater or other concurrent lanes.

## Completion

Completed. Beam stirrup station layouts can no longer place physical stirrup centers closer than one bar diameter, regardless of whether spacing or count drives the layout; exact implementation/test SHAs are recorded above.
