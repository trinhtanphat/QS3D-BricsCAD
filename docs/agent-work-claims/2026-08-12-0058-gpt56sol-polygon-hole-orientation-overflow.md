# Work claim — Polygon hole orientation overflow

- Status: `COMPLETED`
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

Outer and hole loops are individually normalized by the hardened polygon clipper, but region topology had its own raw `Orientation`: `(b.X-a.X)*(c.Y-a.Y) - (b.Y-a.Y)*(c.X-a.X)`. Large finite near-parallel boundaries could therefore overflow intermediate products during `OnSegment`/`BoundariesIntersect`, causing numeric failure before the intended strict-inside/intersect-touch policy was evaluated.

## Implementation

- `ccb6e89159193b198dcda43e61988b71aa38f958` — replace raw outer/hole orientation arithmetic with finite deltas plus a scale-safe determinant helper while preserving `Epsilon` and topology policy.
- `a3127c3a63bc2610a3be3803bf470b7a252cdd84` — add focused regression that invokes the private segment-intersection topology primitive on long finite near-parallel crossing segments, proving the orientation path returns the intended intersection result rather than numeric overflow.

## Validation performed

- Re-fetched committed source and confirmed `Orientation` now delegates to scale-safe `CrossFinite`.
- Re-fetched the regression and confirmed it treats a reflected `TargetInvocationException` as failure, so an old determinant overflow cannot count as pass.
- A separate multiply-before-divide overflow was identified in `LocatePoint`; it is explicitly outside this claim and was not mixed into this source change.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No hole count/total-vertex/segment caps, scanline subtraction, point-in-polygon parity/interpolation, strict-inside/nesting policy, multi-island topology, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Outer/hole and hole-pair orientation topology no longer fails solely on avoidable determinant product overflow, focused regression is integrated on `main`, and this claim is closed.
