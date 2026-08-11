# Agent work claim — column tie axial overlap guard

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T22:11:00+07:00
- Completed: 2026-08-11T22:14:00+07:00
- Status: `COMPLETED`
- Baseline main SHA: `2617eb4d66bc4db73be605dbcc35879ac341b8c8`
- Priority: source-safe Core/Rebar geometry hardening; prevent adjacent column tie solids from occupying overlapping axial ranges.

## Confirmed defect

`ColumnTieLayoutPlanner.Plan(...)` validated positive diameter/spacing and ensured computed spacing did not exceed the requested maximum, but did not require center-to-center spacing to be at least one tie diameter. A D8 at 4 mm configuration could therefore return multiple horizontal tie centers only 4 mm apart even though each tie has an 8 mm physical diameter.

The defect is product-reachable because `ColumnTieSolidBuilder` creates one horizontal tie `Solid3d` at every elevation returned by this planner. The generic spacing primitive does not infer physical collision rules, while specialized reinforcement planners must enforce their own solid-envelope invariant.

## Implemented

- `8e043ea7989b6b0246bd6f2a5800a72b9e2c26e2` — `fix(rebar): reject overlapping column ties`
  - derives the physical tie diameter in meters alongside the existing radius;
  - for multi-tie layouts, rejects computed actual center spacing below one tie diameter;
  - preserves the existing requested-maximum spacing rule, cover/range/count guards, exact tangent equality and the single-tie collapsed-range case.
- `d8891c5d9b2406b7b4f5f1325e29272590a5f5a0` — `test(core): guard column tie axial overlap`
  - rejects D8 at 4 mm overlap;
  - retains normal spacing;
  - retains an exact D8-at-8-mm tangent boundary with 11 ties;
  - retains a collapsed vertical range that legitimately produces one tie with zero spacing.

## Validation evidence

- Re-fetched `src/QS3D.Core/Rebar/ColumnTieLayoutPlanner.cs` from newer `main` (`841b462765c6fa4621f08d8cf587309e0a9ebf3b`); the committed one-diameter axial guard remains intact.
- Re-fetched `tests/QS3D.Core.SmokeTests/ColumnTieAxialOverlapRegressionSmoke.cs` from the same newer tree; the focused public-planner behavioral smoke remains intact.
- Concurrent main updates were on Quantity/Floor-Level UI lanes and did not overwrite the reserved Core/Rebar surfaces.
- No GitHub Actions workflow was dispatched and no smoke executable run is claimed from this connector-only lane.
- No BricsCAD V25 runtime PASS is claimed; product reachability is established from the existing CAD builder call path.

## Reserved scope honored

- Changed only `ColumnTieLayoutPlanner.cs`, the focused Core smoke file, and this claim close-out.
- Did not modify `ColumnTieSolidBuilder`, quantities, persistence, UI, updater, Floor/Level, or other concurrent lanes.

## Completion

Completed. Multi-tie layouts can no longer return axial center spacing below the physical tie diameter; implementation and regression are present on `main` with exact SHAs recorded above.
