# Agent work claim — rectangular column rebar overlap guard

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T21:59:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `c6afb191c60469231893db6ca99e0831515a0131`
- Priority: source-safe Core/Rebar geometry hardening; prevent rectangular column bar centers from being placed closer than one bar diameter.

## Confirmed defect

`RectangularRebarLayoutPlanner.Plan(...)` validates host size, cover, diameter and total bar count, but it does not validate adjacent center spacing on either perimeter direction. A small section with a high `BarsAlongWidth` or `BarsAlongDepth` value can therefore return centers closer than the physical bar diameter.

The defect is product-reachable: `ColumnRebarSolidBuilder` passes the resolved rectangular bar grid directly into this planner and then creates one vertical `Solid3d` cylinder at every returned center. Its post-plan checks cap only bars per element/batch; they do not reject overlapping neighboring cylinders.

## Reserved scope

- `src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs`
- `tests/QS3D.Core.SmokeTests/RectangularRebarOverlapRegressionSmoke.cs`
- this claim file for close-out

## Functional contract

- preserve existing host/cover/count validation and perimeter ordering;
- compute the center-to-center spacing implied by `BarsAlongWidth` and `BarsAlongDepth` inside the usable centerline envelope;
- fail closed when either adjacent spacing is less than one bar diameter;
- allow equality (bars tangent but not overlapping), consistent with the existing beam longitudinal rebar overlap invariant;
- preserve all CAD/native, semantic, persistence and ownership code unchanged.

## Validation target

- behavioral Core smoke rejects width-direction overlap;
- behavioral Core smoke rejects depth-direction overlap;
- behavioral Core smoke retains a normal valid layout and a tangent/non-overlapping boundary case;
- use the established net8 Core smoke `[ModuleInitializer]` registration pattern;
- no GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Completion condition

Planner rejects physically overlapping rectangular column layouts, focused behavioral regression is merged on current `main`, source/test are re-fetched after concurrent updates, and this claim is marked `COMPLETED` with exact implementation/test SHAs.
