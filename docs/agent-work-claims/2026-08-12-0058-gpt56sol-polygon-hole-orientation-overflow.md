# Work claim — Polygon hole orientation overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-polygon-hole-orientation-overflow-20260812-0058`
- Registered: `2026-08-12T00:58:00+07:00`
- Baseline main SHA: `472ac7740a5233c3242af2c5a5652efaaf3ac301`
- Priority: evidence-driven Core polygon-hole topology hardening during owner-requested `continue all`

## Reserved scope

Make `PolygonRegionScanlineClipper` orientation determinants scale-safe for outer/hole containment and hole-pair intersection topology.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolygonRegionScanlineClipper.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

Outer and hole loops are individually normalized by the hardened polygon clipper, but region topology has its own raw `Orientation`: `(b.X-a.X)*(c.Y-a.Y) - (b.Y-a.Y)*(c.X-a.X)`. Large finite near-parallel boundaries can therefore overflow intermediate products during `OnSegment`/`BoundariesIntersect`, causing numeric failure before the intended strict-inside/intersect-touch policy is evaluated.

## Explicit exclusions

- No hole count/total-vertex/segment caps, scanline subtraction, point-in-polygon parity/interpolation, strict-inside/nesting policy, multi-island topology, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Replace only orientation determinant arithmetic with a private scale-safe finite cross helper; preserve `Epsilon`, strict-inside, intersection/touch and nesting rules.
- Add a long finite strip outer boundary containing two thin near-parallel holes that individually stay inside but cross each other; require the explicit `holes 0 and 1 intersect/touch` rejection rather than numeric overflow.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Outer/hole and hole-pair topology validation no longer fails solely on avoidable orientation product overflow, focused regression is integrated on current `main`, and this claim is marked `COMPLETED`.
