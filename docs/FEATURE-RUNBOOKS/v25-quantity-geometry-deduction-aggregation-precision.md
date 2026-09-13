# V25 Quantity Geometry Deduction Aggregation Precision

## Scope

This runbook covers the native V25 `QuantityGeometryExplanationService` aggregation path owned by Issue #6517.
It does not change geometry discovery, boolean subtraction, provenance, relation classification, face identity, or evidence ordering.

## Precision contract

Native BREP contributions are non-negative drawing-unit measurements, but their enumeration order is not a numeric contract.
A pairwise binary64 fold such as `1e16 + 1 + 1` can discard a representable low-order result.
The service therefore routes gross/net component volume, per-element intersection volume, residual face area, per-element/per-face coverage, and diagnostic totals through `QuantityReportMath.FiniteAccumulator`.

Accumulator finalization canonicalizes signed zero and rejects non-finite or overflowing aggregate state. The service must fail closed rather than silently publish a corrupted finite-looking total.

## Compatibility

Quantity geometry DTOs, region keys, source handles, dependency tracking, tolerance policy, sorting, and residual-boolean semantics are unchanged.
The canonical Core accumulator is reused; no parallel numeric engine is introduced.

## Validation

Run `python scripts/preflight-v25-quantity-geometry-deduction-aggregation-precision.py`, then compile `QS3D.BricsCAD.V25` with `BRICSCAD_V25_DIR` bound to the licensed V25 install and run the broad Core smoke suite.

Licensed BREP behavior remains `LOCAL_ONLY` unless a native BricsCAD execution is actually performed on the exact candidate SHA; static/source/compile evidence is not a native runtime PASS.
