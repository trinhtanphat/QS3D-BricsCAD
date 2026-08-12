# Work claim — Polyline area finite-product cancellation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-polyline-area-cancellation-20260812-1316`
- Registered: `2026-08-12T13:16:00+07:00`
- Baseline main SHA: `3de60ce39149fee75f01bd6d4751967f6ab5c035`
- Priority: evidence-driven Core numeric correctness during owner-requested `continue all`

## Confirmed defect

`PolylineMetrics.SignedArea(...)` routed every triangle determinant through independently normalized vectors. That avoided the previously fixed raw-product overflow, but it could erase a representable determinant when the two vectors had very different magnitudes and were nearly parallel: the normalized component ratios could round to the same binary64 value even though the original finite products remained finite and their subtraction was non-zero.

A deterministic finite repro is formed by vectors `(1e46, 2.1485982218963585e45)` and `(0.01, 0.0021485982218963583)`. Their raw products are finite and their binary64 determinant is `-2.4758800785707605e27`; the previous independently-normalized expression rounded to exactly zero, so `SignedArea` silently reported zero for a non-degenerate triangle.

## Reserved scope

- `src/QS3D.Core/Geometry/PolylineMetrics.cs` — `CrossFinite(...)` finite-product compatibility path only.
- `tests/QS3D.Core.SmokeTests/PolylineAreaCrossOverflowSmoke.cs` — extend the existing auto-registered numeric regression with the finite-product cancellation case.
- this claim file.

## Intended contract

- Preserve the existing overflow-safe normalized fallback when either raw component product or their subtraction is non-finite.
- When both raw component products and their determinant are finite, preserve that direct finite determinant instead of unnecessarily normalizing it away.
- Preserve area sign, compensated summation, length behavior, topology semantics and existing true-overflow rejection.

## Coordination

The earlier `polyline area cross overflow` claim is `COMPLETED` and addressed the opposite numeric regime: component products overflow while the determinant remains representable. Current recent claim/commit inspection showed no active `PolylineMetrics.cs` lane. Grid, Curtain, Rebar-family and Release #34 claims were disjoint.

## Integration

- Claim: `676b7b1ea01266b37300b9becde349895c6cc901`
- Product fix: `97e5b3fb4edd1fb361b1a872fa291178abcc7ab0`
- Regression: `c20284d987eea92f19a3ec67b6edd55127aff1d8`
- Product diff is limited to an 8-line raw finite-product fast-path at the start of `CrossFinite(...)`; the existing normalized overflow fallback remains intact.
- The existing module-initializer smoke now covers both raw-product overflow and the asymmetric-scale finite-product cancellation repro.
- Numeric fixture review confirms raw determinant `-2.4758800785707605e27` and expected signed area `-1.2379400392853803e27`.
- No GitHub Actions dispatched and no BricsCAD/Windows runtime PASS claimed.

## Completion

Current `main` preserves finite raw determinants before normalization can erase them while retaining the earlier overflow-safe fallback, and focused auto-registered regression evidence is committed.
