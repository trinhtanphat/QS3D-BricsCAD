# Work claim — Polyline SignedArea cross-product underflow

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-polyline-cross-underflow`
- Registered: `2026-08-12T17:57:00+07:00`
- Baseline main SHA: `eb09884921da90d1ca1d02f80643d4d6334a3516`
- Priority: P0 — finite polygons with a representable non-zero subnormal signed area must not be silently collapsed to zero by intermediate multiplication underflow.

## Reserved scope

Fix `PolylineMetrics.SignedArea(...)` only for the case where `CrossFinite(...)` sees finite non-zero multiplicands whose direct products underflow to exact zero, causing the current finite fast path to return zero instead of using the existing scaled fallback. Add one focused Core smoke regression covering positive and negative orientation plus ordinary zero/area behavior.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolylineMetrics.cs`
- `tests/QS3D.Core.SmokeTests/PolylineSignedAreaCrossUnderflowSmoke.cs`
- this claim file

## Excluded scope

- coordinate-delta overflow already completed by `CORE-POLYLINE-SIGNED-AREA-DELTA-OVERFLOW`
- existing cross-product overflow/cancellation hardening
- `Point2`, polygon topology/scanline planners, callers, persistence, UI, BricsCAD runtime code
- GitHub Actions or licensed V25 runtime qualification

## Validation plan

- deterministic counterexample where raw products underflow to `0d` while scaled cross restoration yields `double.Epsilon`
- reversed winding yields `-double.Epsilon`
- legitimate zero-area and ordinary finite-area behavior remain unchanged
- read back source and smoke from current `main`; verify exact pushed commit ancestry

## Coordination

Recent history shows the prior Polyline delta-overflow claim completed at `f28794f88c24dd2275da48804e4ed6549d0ab174`; recent numeric-underflow claims target BulkEdit, QuantityMath, measured-solid and formula/semantic literal parsing, not `PolylineMetrics.CrossFinite`. No open PR currently owns this source/test scenario.

## Completion condition

The source fix and focused smoke are pushed to current `main`, read back successfully, the exact implementation SHA is recorded, and this claim is updated to `COMPLETED` with validation actually executed and any remaining LOCAL_ONLY/runtime gates stated explicitly.
