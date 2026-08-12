# Work claim — Polygon scanline cross overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-polygon-scanline-cross-overflow-20260812-0041`
- Registered: `2026-08-12T00:41:00+07:00`
- Baseline main SHA: `36de2e4c93b9194257f2a4a813f9cdf3ea4ec649`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `PolygonScanlineClipper` evaluate polygon area and orientation determinants without requiring each large component product to be finite independently.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolygonScanlineClipper.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defects

`NormalizeAndValidate` was origin-relative, but its triangle cross still evaluated `ax * by - ay * bx` directly. `Orientation` repeated the same raw determinant on edge deltas. For nearly parallel finite vectors around `1e160`, the component products can overflow while the determinant after cancellation remains finite. The validator therefore rejected representable simple polygons solely because of intermediate multiplication overflow.

## Implementation

- `4fce6a653e5438fe21bb18a8841b6d619284f0d5` — add one private scale-safe `CrossFinite` helper and use it in both signed-area accumulation and orientation checks; keep compensated summation, `Epsilon`, half-open scanline parity and simple-polygon policy unchanged.
- `0bee3359bc9fdd9b79847783f5f07f0cc4c314c0` — add a focused four-vertex finite polygon around `1e160` whose raw orientation/area component products overflow but whose determinants and area remain representable.

## Validation performed

- Re-fetched target source after claim registration and confirmed both raw determinant paths remained before editing.
- Re-fetched committed source and confirmed area and orientation now share the scale-safe helper without tolerance changes.
- Re-fetched the regression and confirmed normalization preserves all four finite vertices and canonical `PolylineMetrics.Area` remains finite and positive.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No scanline half-open parity rule, intersection deduplication, vertex/segment caps, area tolerance, self-intersection policy, multi-region topology, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Polygon normalization/simple-topology checks no longer fail solely on avoidable determinant product overflow, focused regression is integrated on `main`, and this claim is closed.
