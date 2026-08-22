# Work claim — Wall footprint intersection overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-wall-footprint-intersection-overflow-20260812-0050`
- Registered: `2026-08-12T00:50:00+07:00`
- Baseline main SHA: `49a4d3a18626c38731bdfbd40146b542cb1d9332`
- Priority: evidence-driven Core topology hardening during owner-requested `continue all`

## Reserved scope

Make `WallFootprintEngine` segment-intersection determinants scale-safe so finite self-intersecting centerlines cannot bypass topology rejection through `Infinity - Infinity => NaN` arithmetic.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallFootprintEngine.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`SegmentsIntersect` called a raw `Cross(ax, ay, bx, by) => ax * by - ay * bx`. For long nearly parallel finite segments around `1e160`, both component products can overflow while the determinant remains finite. A true crossing could then produce `NaN`; the parallel test was skipped, `t/u` became `NaN`, and all range comparisons were false, causing `HasSelfIntersection` to miss a self-crossing centerline.

## Implementation

- `d440c0d46499326c320aa907a24f00dec34256e1` — replace the private determinant helper with scale-normalized finite cross evaluation while preserving existing absolute tolerance and parameter-range checks across centerline/footprint intersection and miter paths.
- `e305e95d1ac87a0f248324ed2a5592d844e727db` — add focused smoke coverage for a four-point `1e160` centerline whose first and third long segments genuinely cross while their raw determinant products overflow; require rejection specifically at centerline self-intersection validation.

## Concurrency handling

- The first source update attempt received HTTP 409 while `main` advanced through an unrelated Curtain-path claim completion.
- Re-fetched current `main` and the target blob, confirmed `WallFootprintEngine` source was unchanged, and retried without force; the retry committed successfully.

## Validation performed

- Re-fetched committed source and confirmed `SegmentsIntersect` now uses the scale-safe `Cross` helper for determinant and parameter numerators.
- Re-fetched the regression and confirmed it requires the specific `centerline self-intersects` failure rather than accepting an unrelated downstream exception.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No centerline cleaning, footprint area/perimeter, miter/bevel policy, tolerance values, Wall/WallPier authoring, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Finite self-crossing centerlines can no longer bypass WallFootprint topology checks solely because determinant arithmetic becomes `NaN`, focused regression is integrated on `main`, and this claim is closed.
