# Work claim — Wall-pier path canonical area

- Status: `COMPLETED`
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

After `WallFootprintEngine` was hardened in `be7f3ef1956994beb326c53520b1b59ec9f4ea9b`, long finite diagonal footprints can now reach `WallPierPathProfilePlanner.PolygonArea`. That private helper still evaluated raw `ax * by - ay * bx`, so the same representable footprint could fail one layer later even though canonical `PolylineMetrics.Area` succeeds.

## Implementation

- `b76cfa2a0308ea4dd65a7b4cb8be92e78b232ec0` — replace only private `PolygonArea` arithmetic with canonical `PolylineMetrics.Area`, preserving positive-area and downstream quantity checks.
- `7f5400d1df5b389c8fbc878558bf27640627ee55` — add end-to-end `WallPierPathProfilePlanner.Plan` coverage for a `1e160` diagonal path with `1e145` thickness and unit height, asserting finite profile area, perimeter, volume and lateral area.

## Validation performed

- Re-fetched target source after claim registration and confirmed the private raw cross implementation was still present.
- Re-fetched committed source and confirmed `PolygonArea` now delegates to canonical `PolylineMetrics.Area` only.
- Re-fetched the smoke fixture and confirmed the regression runs through the public `Plan` path rather than a private helper.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No wall-pier path generation, profile/chamfer/miter policy, perimeter/volume/lateral-area formulas, Family/native authoring, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

The now-reachable Wall-pier path layer no longer reintroduces raw determinant overflow after the shared footprint engine succeeds, focused regression is integrated on `main`, and this claim is closed.
