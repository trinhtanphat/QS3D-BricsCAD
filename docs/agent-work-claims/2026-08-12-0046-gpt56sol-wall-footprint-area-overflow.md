# Work claim — Wall footprint area overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-footprint-area-overflow-20260812-0046`
- Registered: `2026-08-12T00:46:00+07:00`
- Baseline main SHA: `9798088227a699deec52139543ec6edbd4d10cda`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `WallFootprintEngine` use the canonical scale-safe signed-area metric for its generated footprint instead of a raw-product duplicate area implementation.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallFootprintEngine.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`SignedAreaRelative` triangulates relative to the first footprint point but still evaluates each cross as `ax * by - ay * bx`. A long diagonal centerline around `1e160` with a much smaller finite thickness can produce a finite wall footprint area around `1e305` while the nearly parallel long triangle vectors each form component products around `1e320`. The current raw products overflow even though the final area is representable.

## Explicit exclusions

- No centerline cleaning, miter/bevel construction, self-intersection policy, segment intersection math, perimeter, native Wall/WallPier authoring, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Route `SignedAreaRelative` through the now scale-safe canonical `PolylineMetrics.SignedArea` while preserving the caller's absolute-area and degeneracy checks.
- Add focused smoke coverage for a long finite diagonal centerline plus finite thin thickness whose footprint area is representable but old triangle products overflow.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Wall footprint area generation no longer fails solely on avoidable determinant product overflow, regression is integrated on current `main`, and this claim is marked `COMPLETED`.
