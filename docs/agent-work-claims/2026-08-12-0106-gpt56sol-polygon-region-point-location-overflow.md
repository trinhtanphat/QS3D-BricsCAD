# Work claim — Polygon multi-region point-location overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-polygon-region-point-location-overflow-20260812-0106`
- Registered: `2026-08-12T01:06:00+07:00`
- Baseline main SHA: `92ba1466706748f4b77060b744c4f50df49faa53`
- Priority: evidence-driven Core multi-region topology hardening during owner-requested `continue all`

## Reserved scope

Make `PolygonRegionSetTopology.LocatePoint` evaluate ray/edge intersection X without multiply-before-divide overflow for finite long diagonal island boundaries.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolygonRegionSetTopology.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`LocatePoint` computed `a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y)`. For a finite point vertically between endpoints of a long diagonal edge, the interpolation ratio is bounded and the final intersection X can be finite while the intermediate product overflows. Cross-island nesting detection could therefore throw `OverflowException` instead of reaching the explicit nested-island policy.

## Implementation

- `12bb2359e01d88bb91151db4c0b3ccc3cbccc312` — compute finite point/edge deltas, divide the vertical offset by edge Y first, then multiply edge X by the finite ratio before reconstructing intersection X.
- `17dfdaf5f6a1cd5a2b47c4e5ad42d676e24e144d` — add public multi-region coverage with a smaller long diagonal island strictly nested inside a larger long diagonal island; require the explicit `overlap or are nested` policy rejection rather than numeric overflow.

## Validation performed

- Re-fetched committed source and confirmed ratio-first point-location interpolation is present in the cross-island topology layer.
- Re-fetched the regression and confirmed it requires the intended nesting/ownership-policy error, so an `OverflowException` cannot count as pass.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No orientation determinant, island count/vertex caps, region IDs, hole semantics, tagged scanline output, nesting/ownership policy, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Representable cross-island point location no longer fails solely because interpolation multiplies before dividing, focused regression is integrated on `main`, and this claim is closed.
