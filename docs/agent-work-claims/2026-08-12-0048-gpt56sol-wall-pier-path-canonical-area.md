# Work claim — Wall-pier path canonical area

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-pier-path-canonical-area-20260812-0048`
- Registered: `2026-08-12T00:48:00+07:00`
- Baseline main SHA: `21fc29ab9dec575e326000520547d362a9eab109`
- Priority: evidence-driven Core numeric hardening after upstream WallFootprint repair

## Reserved scope

Make `WallPierPathProfilePlanner` use canonical `PolylineMetrics.Area` for generated footprint area after `WallFootprintEngine` has successfully produced the footprint.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallPierPathProfilePlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

After `WallFootprintEngine` was hardened in `be7f3ef1956994beb326c53520b1b59ec9f4ea9b`, long finite diagonal footprints can now reach `WallPierPathProfilePlanner.PolygonArea`. That private helper still evaluates raw `ax * by - ay * bx`, so the same representable footprint can now fail one layer later even though canonical `PolylineMetrics.Area` succeeds.

## Explicit exclusions

- No wall-pier path generation, profile/chamfer/miter policy, perimeter/volume/lateral-area formulas, Family/native authoring, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Replace only private polygon area arithmetic with canonical `PolylineMetrics.Area` while preserving existing positive-area and downstream quantity checks.
- Add end-to-end `WallPierPathProfilePlanner.Plan` smoke coverage using the same `1e160` diagonal / `1e145` thickness scale with a finite height, asserting finite profile area, perimeter, volume and lateral area.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

The now-reachable Wall-pier path layer no longer reintroduces raw determinant overflow after the shared footprint engine succeeds, focused regression is integrated on current `main`, and this claim is marked `COMPLETED`.
