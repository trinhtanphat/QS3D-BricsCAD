# Work claim — Wall-pier path area overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-pier-path-area-overflow-20260812-0044`
- Registered: `2026-08-12T00:44:00+07:00`
- Baseline main SHA: `ad4f2f304fc449ba7ce59b5b904675a68d1fdc48`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `WallPierPathProfilePlanner` use the canonical scale-safe polygon metric for footprint area instead of maintaining a raw-product duplicate implementation.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallPierPathProfilePlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

The private `PolygonArea` helper triangulates relative to an origin but still computes `ax * by - ay * bx` directly. As with the already hardened canonical `PolylineMetrics.SignedArea`, nearly parallel finite vectors around `1e160` can have overflowing component products while the final determinant/area remains finite. Wall-pier path profiles can therefore reject a representable footprint solely because this duplicate area implementation is numerically weaker than the canonical metric.

## Explicit exclusions

- No wall-pier centerline/footprint generation, terminal chamfer geometry, miter policy, perimeter/volume/lateral-area semantics, native V25 authoring, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Route private footprint-area calculation through `PolylineMetrics.Area`, preserving positive-area validation and all downstream quantity formulas.
- Add focused smoke coverage for a finite large-coordinate polygon through the area path where raw cross products overflow but canonical area remains finite.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Wall-pier path area uses the canonical hardened polyline metric and no longer fails solely on duplicate raw-product overflow, focused regression is integrated on current `main`, and this claim is marked `COMPLETED`.
