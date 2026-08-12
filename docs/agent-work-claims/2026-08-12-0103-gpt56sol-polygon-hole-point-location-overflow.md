# Work claim — Polygon hole point-location overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-polygon-hole-point-location-overflow-20260812-0103`
- Registered: `2026-08-12T01:03:00+07:00`
- Baseline main SHA: `34a9cea7d52c1afede22abb22d4ae8766ba28f1a`
- Priority: evidence-driven Core polygon-hole topology hardening during owner-requested `continue all`

## Reserved scope

Make `PolygonRegionScanlineClipper.LocatePoint` evaluate ray/edge intersection X without multiply-before-divide overflow for finite long diagonal boundaries.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolygonRegionScanlineClipper.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`LocatePoint` computed `a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y)`. For a finite point vertically between endpoints of a long diagonal edge, the interpolation ratio is in `[0,1]` and the final X is representable, but the intermediate product `deltaY * deltaX` could overflow before division. A valid strictly-contained hole could therefore fail with `OverflowException` instead of reaching the intended containment topology.

## Implementation

- `198df88b4ee48bb977f1e1dc0f4292cd035624ea` — compute finite edge deltas, divide the vertical offset by the edge Y delta first, then multiply the horizontal delta by that finite ratio before reconstructing intersection X.
- `3c9e67fc5a1a919c21bd5730e8d735cc1f82e2c5` — add public `NormalizeAndValidate` coverage with a `1e160` diagonal outer strip and a smaller strictly-contained diagonal hole, requiring successful one-hole normalization with finite coordinates.

## Concurrency handling

- The first claim creation attempt received HTTP 409 while `main` advanced through an unrelated ElementInstance planning commit.
- Re-fetched current `main`, checked for a competing point-location claim, then registered this scope from the new baseline without force.

## Validation performed

- Re-fetched committed source and confirmed ratio-first interpolation plus finite guards are present in `LocatePoint`.
- Re-fetched the public regression and confirmed it exercises the full outer/hole normalization path rather than a private helper.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No orientation determinant, hole count/vertex/segment caps, scanline subtraction, parity rule, strict-inside/nesting policy, multi-island topology, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

A representable point-in-polygon intersection can no longer fail solely because interpolation multiplies before dividing, focused regression is integrated on `main`, and this claim is closed.
