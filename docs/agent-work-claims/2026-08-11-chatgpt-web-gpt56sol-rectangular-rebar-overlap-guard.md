# Agent work claim — rectangular column rebar overlap guard

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T21:59:00+07:00
- Completed: 2026-08-11T22:06:00+07:00
- Status: `COMPLETED`
- Baseline main SHA: `c6afb191c60469231893db6ca99e0831515a0131`
- Priority: source-safe Core/Rebar geometry hardening; prevent rectangular column bar centers from being placed closer than one bar diameter.

## Confirmed defect

`RectangularRebarLayoutPlanner.Plan(...)` validated host size, cover, diameter and total bar count, but did not validate adjacent center spacing on either perimeter direction. A small section with a high `BarsAlongWidth` or `BarsAlongDepth` value could therefore return centers closer than the physical bar diameter.

The defect is product-reachable: `ColumnRebarSolidBuilder` passes the resolved rectangular bar grid directly into this planner and then creates one vertical `Solid3d` cylinder at every returned center. Its post-plan checks cap only bars per element/batch; they do not reject overlapping neighboring cylinders.

## Implemented

- `50301c73893197dffda610e2d21928937418d4cd` — `fix(rebar): reject overlapping rectangular column bars`
  - computes width- and depth-direction center spacing inside the usable centerline envelope;
  - rejects either direction when adjacent centers are closer than one physical bar diameter;
  - preserves tangent equality, existing cover/count guards and perimeter ordering;
  - leaves CAD/native, semantic, persistence and ownership code unchanged.
- `b1a4b68d79b2a080b0ba39cbefc7069020de6913` — `test(core): guard rectangular column bar overlap`
  - rejects width-direction overlap;
  - rejects depth-direction overlap;
  - retains a normal valid layout;
  - retains the exact one-diameter tangent boundary.

## Validation evidence

- Re-fetched `src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs` from newer `main` (`d4bcda9bb2128040fab1975d2c37427845ca83ec`); the spacing guards and committed blob remain intact.
- Re-fetched `tests/QS3D.Core.SmokeTests/RectangularRebarOverlapRegressionSmoke.cs` from the same newer tree; the focused public-planner behavioral smoke remains intact.
- The regression uses the repository's established net8 Core smoke `[ModuleInitializer]` registration pattern.
- Concurrent main updates were on Plan-to-3D/updater and did not overwrite the reserved Core/Rebar surfaces.
- No GitHub Actions workflow was dispatched and no smoke executable run is claimed from this connector-only lane.
- No BricsCAD V25 runtime claim is required for the pure Core planner invariant; the product reachability was source-confirmed in `ColumnRebarSolidBuilder`.

## Reserved scope honored

- Changed only `RectangularRebarLayoutPlanner.cs`, the focused Core smoke file, and this claim close-out.
- Did not modify `ColumnRebarSolidBuilder`, CAD/native generation, persistence, UI, updater, Plan-to-3D or other concurrent lanes.

## Completion

Completed. Rectangular column reinforcement can no longer return adjacent perimeter bar centers closer than one bar diameter; implementation and regression are present on `main` with exact SHAs recorded above.
