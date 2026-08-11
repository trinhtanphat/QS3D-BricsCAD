# Agent work claim — linear rebar physical spacing guard

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T22:25:00+07:00
- Completed: 2026-08-11T22:29:00+07:00
- Status: `COMPLETED`
- Baseline main SHA: `aef0b062c68cdb33f5a28f0a830858a230f3be59`
- Priority: Core/Rebar physical geometry invariant; reject linear distributions whose adjacent physical bars overlap.

## Confirmed defect

`LinearRebarLayoutPlanner` used `DiameterMm` to place bar centers inside a cover envelope, but for layouts with two or more bars did not require `ActualSpacingM` to be at least one bar diameter. Count-driven or spacing-driven inputs could therefore return overlapping physical bars. Specialized beam planners had downstream guards, while slab/wall mesh planners consume this linear result directly.

## Implemented

- `d2de259a80f9775898a89613cde8e59d14cb709b` — `fix(rebar): reject overlapping linear bars`
  - derives physical diameter in meters alongside the existing radius/cover envelope calculation;
  - preserves singleton `Count=1` with centered offset and zero spacing;
  - for every multi-bar layout rejects actual center spacing below one physical bar diameter;
  - applies equally to explicit-count and spacing-driven distributions;
  - allows exact one-diameter tangent spacing.
- `58d63df92cce6f59ed7b10bb9dbeecf25d38eecf` — `test(core): guard linear rebar physical spacing`
  - rejects count-driven sub-diameter spacing;
  - rejects spacing-driven sub-diameter spacing;
  - retains normal spacing;
  - retains exact tangent spacing;
  - retains singleton behavior.

## Propagated protection

The existing rectangular slab, polygonal slab and rectangular wall mesh planners all obtain direction offsets/actual spacing through `LinearRebarLayoutPlanner`, so the generic guard prevents in-plane parallel-bar overlap before those planners create mesh placements. Existing specialized beam/column collision checks remain valid defense-in-depth.

## Validation evidence

- Re-fetched `src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs` from newer `main` (`12524e100f54fb46b0875598eb27200363d78b20`); the committed one-diameter physical-spacing guard remains intact.
- Re-fetched `tests/QS3D.Core.SmokeTests/LinearRebarPhysicalSpacingRegressionSmoke.cs` from the same newer tree; the five focused public-planner cases remain intact.
- Reviewed historical `scripts/preflight-geometry-completion.py` integration: it guards the linear planner by stable contract tokens (`MaxBars`, `usableSpanM`, `ActualSpacingM`, `OffsetsM`) and does not depend on the old exact radius expression, so the equivalent `diameterM / 2` formulation does not invalidate that static gate.
- Existing linear-layout smoke expected values remain mathematically unchanged because `diameterMm / 2000` and `(diameterMm / 1000) / 2` are equivalent.
- No GitHub Actions workflow was dispatched and no smoke executable run is claimed from this connector-only lane.
- No BricsCAD V25 runtime PASS is claimed or required for this pure Core invariant.

## Reserved scope honored

- Changed only `LinearRebarLayoutPlanner.cs`, the focused Core smoke file, and this claim close-out.
- Did not modify slab/wall planners, CAD/native builders, persistence, UI, quantity rules, updater, documentation tooling or other concurrent lanes.

## Completion

Completed. Generic linear rebar distribution now fails closed before returning physical bar centers closer than one diameter, protecting all current callers while preserving valid/tangent/singleton layouts; exact implementation/test SHAs are recorded above.
