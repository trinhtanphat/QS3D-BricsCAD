# Work claim — Wall footprint intersection overflow

- Status: `ACTIVE`
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

`SegmentsIntersect` calls a raw `Cross(ax, ay, bx, by) => ax * by - ay * bx`. For long nearly parallel finite segments around `1e160`, both component products can overflow while the determinant remains finite. A true crossing can then produce `NaN`; the parallel test is skipped, `t/u` become `NaN`, and all range comparisons are false, causing `HasSelfIntersection` to miss a self-crossing centerline.

## Explicit exclusions

- No centerline cleaning, footprint area/perimeter, miter/bevel policy, tolerance values, Wall/WallPier authoring, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Replace the private determinant helper with a scale-safe finite cross implementation while preserving all current absolute tolerance and parameter-range checks.
- Add focused smoke coverage for a four-point centerline whose first and third long segments genuinely cross, whose raw determinant products overflow, and assert `Build` rejects specifically as a self-intersecting centerline.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Finite self-crossing centerlines can no longer bypass WallFootprint topology checks solely because determinant arithmetic becomes `NaN`, regression is integrated on current `main`, and this claim is marked `COMPLETED`.
