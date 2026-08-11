# Work claim — Polygon scanline cross overflow

- Status: `ACTIVE`
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

`NormalizeAndValidate` is already origin-relative, but its triangle cross still evaluates `ax * by - ay * bx` directly. `Orientation` repeats the same raw determinant on edge deltas. For nearly parallel finite vectors around `1e160`, the component products can overflow while the determinant after cancellation remains finite. The validator therefore rejects representable simple polygons solely because of intermediate multiplication overflow.

## Explicit exclusions

- No scanline half-open parity rule, intersection deduplication, vertex/segment caps, area tolerance, self-intersection policy, multi-region topology, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Add one private scale-safe determinant helper and use it for both signed-area crosses and orientation tests, keeping current compensated area summation and `Epsilon` comparisons unchanged.
- Add a focused simple quadrilateral with coordinates around `1e160` whose raw determinant products overflow but finite area/orientations remain representable; assert normalization succeeds and stays finite.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Polygon normalization/simple-topology checks no longer fail solely on avoidable determinant product overflow, regression is integrated on current `main`, and this claim is marked `COMPLETED`.
