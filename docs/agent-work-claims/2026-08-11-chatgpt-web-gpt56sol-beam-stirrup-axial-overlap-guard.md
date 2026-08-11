# Agent work claim — beam stirrup axial overlap guard

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T22:15:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `f674b91cd6948a11786d3bd3ed88084bd20f7b88`
- Priority: source-safe Core/Rebar geometry hardening; prevent adjacent beam stirrup solids from overlapping along the beam axis.

## Confirmed defect

`BeamStirrupLayoutPlanner.Plan(...)` delegates station placement to the generic `LinearRebarLayoutPlanner`, but does not apply a beam-stirrup physical collision rule afterward. Both spacing-driven and count-driven inputs can therefore return adjacent stirrup station centers closer than the stirrup diameter. For example, D8 at 4 mm creates station spacing below the 8 mm solid diameter.

The defect is product-reachable because `BeamStirrupSolidBuilder` creates a separate stirrup solid loop at every returned station. Beam longitudinal reinforcement already applies a specialized one-diameter center-spacing invariant after using the same generic linear planner.

## Reserved scope

- `src/QS3D.Core/Rebar/BeamStirrupLayoutPlanner.cs`
- `tests/QS3D.Core.SmokeTests/BeamStirrupAxialOverlapRegressionSmoke.cs`
- this claim file for close-out

## Functional contract

- preserve existing end/section cover, bend radius, hook, sagitta and linear layout guards;
- when more than one station exists, require actual station center spacing to be at least one physical stirrup diameter;
- enforce the rule for both spacing-driven and explicit-count station layouts;
- allow exact one-diameter tangent spacing;
- do not modify the generic `LinearRebarLayoutPlanner` or CAD/native builder.

## Validation target

- behavioral Core smoke rejects spacing-driven D8 at 4 mm overlap;
- behavioral Core smoke rejects an explicit-count layout whose resulting station spacing is below D8 diameter;
- behavioral Core smoke retains normal spacing;
- behavioral Core smoke retains exact one-diameter tangent spacing;
- use the established net8 Core smoke `[ModuleInitializer]` pattern;
- no GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Completion condition

Beam stirrup planner fails closed on physical axial overlap for both input modes, focused behavioral regression is merged on current `main`, source/test are re-fetched after concurrent updates, and this claim is marked `COMPLETED` with exact implementation/test SHAs.
