# Agent work claim — linear rebar physical spacing guard

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T22:25:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `aef0b062c68cdb33f5a28f0a830858a230f3be59`
- Priority: Core/Rebar physical geometry invariant; reject linear distributions whose adjacent physical bars overlap.

## Confirmed defect

`LinearRebarLayoutPlanner` uses `DiameterMm` to place bar centers inside a cover envelope, but for layouts with two or more bars it does not require `ActualSpacingM` to be at least one bar diameter. Count-driven or spacing-driven inputs can therefore return overlapping physical bars. Specialized beam planners had to add downstream guards, while slab/wall mesh planners currently consume the same linear result directly.

## Reserved scope

- `src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs`
- `tests/QS3D.Core.SmokeTests/LinearRebarPhysicalSpacingRegressionSmoke.cs`
- this claim file for close-out

## Functional contract

- keep exactly-one-of Count/SpacingMm semantics, cover envelope, deterministic endpoints, max-bar cap and near-integer spacing behavior;
- preserve `Count=1` with `ActualSpacingM=0`;
- for every layout with two or more bars, fail closed when actual center-to-center spacing is less than the physical bar diameter;
- allow exact one-diameter tangent spacing;
- apply equally to count-driven and spacing-driven layouts;
- specialized downstream guards may remain as defense-in-depth.

## Validation target

- direct Core smoke rejects count-driven sub-diameter spacing;
- direct Core smoke rejects spacing-driven sub-diameter spacing;
- direct Core smoke retains normal spacing, exact tangent spacing and singleton behavior;
- existing linear-layout smoke expectations remain unchanged;
- no GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Completion condition

Generic linear rebar distribution enforces physical non-overlap, focused behavioral regression is merged on current `main`, source/test are re-fetched after concurrent updates, and this claim is marked `COMPLETED` with exact implementation/test SHAs.
