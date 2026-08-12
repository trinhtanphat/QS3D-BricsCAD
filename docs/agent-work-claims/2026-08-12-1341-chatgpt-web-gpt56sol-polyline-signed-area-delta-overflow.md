# Work claim — Polyline SignedArea coordinate-delta overflow

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-polyline-signed-area-delta-overflow`
- Registered: `2026-08-12T13:41:00+07:00`
- Baseline main SHA: `92c2a3486b632012fcc5e368e5871616ceebdaa2`
- Priority: P0 — finite polygons with representable signed area must not fail because a translated coordinate delta overflows first.
- Task Key: `CORE-POLYLINE-SIGNED-AREA-DELTA-OVERFLOW`

## Confirmed defect

`PolylineMetrics.SignedArea(...)` triangulates around `points[0]` and calls `SubtractFinite(...)` on translated coordinates before the overflow-aware cross-product path. For finite coordinates on opposite sides of the double range, a translated delta can overflow even when the actual signed area is small and representable.

Concrete counterexample: `(-double.MaxValue, 0)`, `(double.MaxValue, 0)`, `(0, 2.2250738585072014e-308)`. The mathematical triangle area is approximately `double.MaxValue * 2.2250738585072014e-308 ~= 4`, but `double.MaxValue - (-double.MaxValue)` overflows and the current implementation throws before computing the representable cross product.

## Non-overlap check

The latest history/code search returned no PolylineMetrics/SignedArea overflow lane. Comparing the audited source snapshot `9f8398883e0408dc6f1c6a6500c5a94eb80f624f` to baseline `92c2a3486b632012fcc5e368e5871616ceebdaa2` shows no changes to `src/QS3D.Core/Geometry/PolylineMetrics.cs`.

## Reserved scope

- `src/QS3D.Core/Geometry/PolylineMetrics.cs`
- one focused Core smoke regression
- this claim file

Do not alter `Point2`, callers, polygon topology/scanline planners, BricsCAD runtime code, or finite-input validation semantics.

## Intended contract

- Preserve the existing direct translated-cross fast path when all coordinate deltas are finite.
- If translation itself overflows, compute the same triangle cross product using independent finite X/Y scaling before subtraction, so extreme anisotropic coordinates do not lose the small axis.
- Return a finite signed area whenever the computed area is representable; retain fail-closed overflow when the area/cross/sum truly exceeds supported numeric range.
- Preserve orientation/sign and normal-coordinate behavior.

## Completion condition

A focused regression proves the extreme finite triangle returns finite area near +4 and reversed winding near -4, while ordinary polygon behavior and genuine overflow remain fail-closed; merged source + smoke are read back from current `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
