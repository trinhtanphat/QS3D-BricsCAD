# Work claim — Polygon hole point-location overflow

- Status: `ACTIVE`
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

`LocatePoint` currently computes `a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y)`. For a finite point vertically between endpoints of a long diagonal edge, the interpolation ratio is in `[0,1]` and the final X is representable, but the intermediate product `deltaY * deltaX` can overflow before division. A valid strictly-contained hole can therefore fail with `OverflowException` instead of reaching the intended containment topology.

## Explicit exclusions

- No orientation determinant, hole count/vertex/segment caps, scanline subtraction, parity rule, strict-inside/nesting policy, multi-island topology, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Compute finite edge deltas, form the vertical interpolation ratio first, then multiply the horizontal delta by that bounded ratio before adding to the edge origin.
- Add public `NormalizeAndValidate` coverage with a long finite diagonal strip outer boundary and a smaller strictly-contained diagonal strip hole; require successful normalization with one hole rather than numeric overflow.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

A representable point-in-polygon intersection can no longer fail solely because interpolation multiplies before dividing, focused regression is integrated on current `main`, and this claim is marked `COMPLETED`.
