# Work claim — Wall footprint area overflow

- Status: `COMPLETED`
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

`SignedAreaRelative` triangulated relative to the first footprint point but still evaluated each cross as `ax * by - ay * bx`. A long diagonal centerline around `1e160` with a much smaller finite thickness can produce a finite wall footprint area around `1e305` while the nearly parallel long triangle vectors each form component products around `1e320`. The raw products overflowed even though the final area is representable.

## Implementation

- `be7f3ef1956994beb326c53520b1b59ec9f4ea9b` — route `SignedAreaRelative` through canonical scale-safe `PolylineMetrics.SignedArea`, preserving the caller's absolute-area and degeneracy checks.
- `21e59125833495e9cbbd94b1ab3dd1b7f7304dea` — add focused smoke coverage for a straight diagonal centerline of `1e160` with thickness `1e145`, asserting finite four-vertex footprint, length, area and perimeter without a bevel join.

## Validation performed

- Re-fetched target source after claim registration and confirmed the raw-product private area helper remained before editing.
- Re-fetched committed source and confirmed wall footprint signed area now delegates to canonical `PolylineMetrics.SignedArea`.
- Re-fetched the smoke fixture and confirmed it exercises the representable determinant-cancellation footprint.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No centerline cleaning, miter/bevel construction, self-intersection policy, segment intersection math, perimeter, native Wall/WallPier authoring, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Wall footprint area generation no longer fails solely on avoidable determinant product overflow, focused regression is integrated on `main`, and this claim is closed.
