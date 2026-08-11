# Agent work claim — column tie axial overlap guard

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T22:11:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `2617eb4d66bc4db73be605dbcc35879ac341b8c8`
- Priority: source-safe Core/Rebar geometry hardening; prevent adjacent column tie solids from occupying overlapping axial ranges.

## Confirmed defect

`ColumnTieLayoutPlanner.Plan(...)` validates positive diameter/spacing and ensures computed spacing does not exceed the requested maximum, but it does not require center-to-center spacing to be at least one tie diameter. A notation/property combination such as D8 at 4 mm can therefore produce multiple horizontal tie centers only 4 mm apart even though each tie has an 8 mm physical diameter.

The defect is product-reachable because `ColumnTieSolidBuilder` creates one horizontal tie `Solid3d` at every elevation returned by this planner. The generic linear/spacing policy is not expected to infer physical collision rules, while beam longitudinal reinforcement already applies an explicit one-diameter collision invariant at its specialized planner boundary.

## Reserved scope

- `src/QS3D.Core/Rebar/ColumnTieLayoutPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ColumnTieAxialOverlapRegressionSmoke.cs`
- this claim file for close-out

## Functional contract

- preserve all existing section cover, vertical range, maximum spacing and tie-count guards;
- when more than one tie is generated, require actual center-to-center spacing to be at least one physical tie diameter;
- allow exact one-diameter tangent spacing;
- preserve the valid single-tie collapsed-range case where `ActualSpacingM == 0`;
- do not modify CAD/native builder, quantities, persistence, UI or unrelated planners.

## Validation target

- behavioral Core smoke rejects D8 ties whose actual spacing is below 8 mm;
- behavioral Core smoke retains normal valid spacing;
- behavioral Core smoke retains exact one-diameter tangent spacing;
- behavioral Core smoke retains the single-tie collapsed vertical range;
- use the established net8 Core smoke `[ModuleInitializer]` pattern;
- no GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Completion condition

Column tie planner fails closed on physical axial overlap, focused behavioral regression is merged on current `main`, source/test are re-fetched after concurrent updates, and this claim is marked `COMPLETED` with exact implementation/test SHAs.
