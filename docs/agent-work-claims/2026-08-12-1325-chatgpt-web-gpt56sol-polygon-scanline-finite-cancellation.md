# Work claim — Polygon scanline finite determinant cancellation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-polygon-scanline-finite-cancellation-20260812-1325`
- Registered: `2026-08-12T13:25:00+07:00`
- Baseline main SHA: `0c21bc71c6d10481a5d8a4d2368d11c2e8673467`
- Priority: evidence-driven Core geometry numeric correctness during owner-requested `continue all`

## Confirmed defect

`PolygonScanlineClipper.CrossFinite(...)` always normalizes its two determinant vectors independently. That protects the previously fixed overflow case, but when both raw products are finite it can lose a real determinant through ratio rounding.

Deterministic binary64 repro: `A=(1e46, 2.1485982218963585e45)` and `B=(0.01, 0.0021485982218963583)`. `A.X*B.Y - A.Y*B.X` is the finite non-zero value `-2.4758800785707605e27`, while the independently normalized determinant rounds to exactly zero. Therefore the simple three-vertex polygon `(0,0), A, B` is incorrectly rejected by `NormalizeAndValidate(...)` as zero-area even though its determinant is far above the fixed `Epsilon` tolerance.

## Reserved scope

- `src/QS3D.Core/Geometry/PolygonScanlineClipper.cs` — `CrossFinite(...)` finite-product compatibility path only.
- `tests/QS3D.Core.SmokeTests/PolygonScanlineCrossOverflowSmoke.cs` — extend the existing auto-registered determinant regression with the finite-product cancellation triangle.
- this claim file.

## Intended contract

- Return the direct determinant when both raw products and their subtraction are finite.
- Retain the current independently-normalized fallback only when raw multiplication/subtraction becomes non-finite.
- Preserve `Epsilon`, compensated area summation, self-intersection/orientation policy, half-open scanline parity, intersection deduplication and segment bounds.

## Coordination

The earlier polygon scanline cross-overflow claim is `COMPLETED` and addressed the opposite numeric regime (overflowing products with representable determinant). Recent claim/commit inspection shows no newer live `PolygonScanlineClipper.cs` claim. Current Bulk/Family, Beam Stirrup, Grid Naming and release/preflight lanes are disjoint.

## Validation boundary

Extend the existing ModuleInitializer smoke with the asymmetric-scale triangle, assert its raw determinant is finite/non-zero and require `NormalizeAndValidate` plus `PolylineMetrics.Area` to preserve a finite positive area. Source/readback validation only; no GitHub Actions and no BricsCAD runtime PASS claim.
