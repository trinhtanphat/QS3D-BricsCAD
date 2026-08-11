# Work claim — Grid LINE point projection overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-line-point-projection-overflow-20260812-0117`
- Registered: `2026-08-12T01:17:00+07:00`
- Baseline main SHA: `df8ee6865e9fcd3e1b80ba6abc535098a960af03`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `GridIntersectionPlanner.IsOnLineSegment` avoid `length^2` and raw dot-product overflow when validating representable points on very long LINE references.

## Concrete defect

The collinear/shared-endpoint LINE path eventually called `IsOnLineSegment`, which evaluated `px*dx + py*dy` and `dx*dx + dy*dy`. For a valid LINE around `1e160`, `length` remains finite while `length^2` overflows near `1e320`. A representable shared endpoint could therefore fail with `OverflowException` even though membership is exactly decidable from projection onto the already finite unit direction.

## Implementation

- `9b2dddb24db783a0710b648cf883e7ec401a218c` — preserve the existing perpendicular cross-distance test, then project the point offset onto the finite unit LINE direction with scale-safe `DotWithUnit`; replace length-square parameter bounds with the mathematically equivalent distance-domain tolerance `min(tolerance, length)` and a separate beyond-end residual comparison.
- `777652ef603bd3b28f76fc7b1606f8cf2c1d2fac` — add public coverage for two collinear `1e160` LINEs sharing exactly one endpoint with explicit `1e-15` tolerance, requiring one finite exact shared-endpoint intersection and deterministic pair identity.

## Validation performed

- Re-fetched committed source and confirmed `IsOnLineSegment` no longer forms a raw dot product or `length^2`.
- Re-fetched the smoke fixture and confirmed the public `FindIntersections` path exercises the collinear/shared-endpoint branch.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No LINE/LINE determinant/cross-tolerance logic, LINE/ARC or ARC/ARC math, ambiguity policy, default tolerance, curve validation/cardinality, identity/ownership, native V25 inspection, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Representable points on long Grid LINEs no longer fail solely on dot/length-square overflow while off-segment tolerance semantics remain equivalent, focused regression is integrated on `main`, and this claim is closed.
