# Work claim — Polygon multi-region point-location overflow

- Status: `ACTIVE`
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

`LocatePoint` currently computes `a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y)`. For a finite point vertically between endpoints of a long diagonal edge, the interpolation ratio is bounded and the final intersection X can be finite while the intermediate product overflows. Cross-island nesting detection can therefore throw `OverflowException` instead of reaching the explicit nested-island policy.

## Explicit exclusions

- No orientation determinant, island count/vertex caps, region IDs, hole semantics, tagged scanline output, nesting/ownership policy, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Compute finite edge deltas, divide vertical offset by edge Y delta first, then multiply the horizontal delta by that ratio before reconstructing X.
- Add public multi-region coverage with a smaller long diagonal island strictly nested inside a larger long diagonal island; require the explicit `overlap or are nested` policy rejection rather than numeric overflow.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Representable cross-island point location no longer fails solely because interpolation multiplies before dividing, focused regression is integrated on current `main`, and this claim is marked `COMPLETED`.
