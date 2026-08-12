# Work claim — Polyline area finite-product cancellation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-polyline-area-cancellation-20260812-1316`
- Registered: `2026-08-12T13:16:00+07:00`
- Baseline main SHA: `3de60ce39149fee75f01bd6d4751967f6ab5c035`
- Priority: evidence-driven Core numeric correctness during owner-requested `continue all`

## Confirmed defect

`PolylineMetrics.SignedArea(...)` currently routes every triangle determinant through independently normalized vectors. That avoids the previously fixed raw-product overflow, but it can erase a representable determinant when the two vectors have very different magnitudes and are nearly parallel: the normalized component ratios can round to the same binary64 value even though the original finite products remain finite and their subtraction is non-zero.

A deterministic finite repro is formed by vectors `(1e46, 2.1485982218963585e45)` and `(0.01, 0.0021485982218963583)`. Their raw products are finite and their binary64 determinant is `-2.4758800785707605e27`; the current independently-normalized expression rounds to exactly zero, so `SignedArea` silently reports zero for a non-degenerate triangle.

## Reserved scope

- `src/QS3D.Core/Geometry/PolylineMetrics.cs` — `CrossFinite(...)` finite-product compatibility path only.
- `tests/QS3D.Core.SmokeTests/PolylineAreaCrossOverflowSmoke.cs` — extend the existing auto-registered numeric regression with the finite-product cancellation case.
- this claim file.

## Intended contract

- Preserve the existing overflow-safe normalized fallback when either raw component product or their subtraction is non-finite.
- When both raw component products and their determinant are finite, preserve that direct finite determinant instead of unnecessarily normalizing it away.
- Preserve area sign, compensated summation, length behavior, topology semantics and existing true-overflow rejection.

## Coordination

The earlier `polyline area cross overflow` claim is `COMPLETED` and addressed the opposite numeric regime: component products overflow while the determinant remains representable. Current recent claim/commit inspection shows no active `PolylineMetrics.cs` lane. Grid, Curtain, Rebar-family and Release #34 claims are disjoint.

## Validation boundary

Extend the existing module-initializer smoke with the deterministic asymmetric-scale triangle, assert the raw finite determinant is non-zero, and require `SignedArea` to equal half that determinant. Source/readback validation only; no GitHub Actions dispatch and no BricsCAD runtime PASS claim.
