# Quantity geometry explanation aggregation precision

Lane-Key: `issue-4657`

## Contract

`QuantityGeometryExplanation` exposes `GrossFormworkArea`, `DeductionFormworkArea`, and `NetFormworkArea` by aggregating the corresponding values across `FormworkFaces`. These public explanation totals must preserve finite non-negative contributions that temporarily fall below the running binary64 spacing when the eventual grouped result is representable.

The aggregation therefore maintains isolated compensated state for each property evaluation instead of fail-fast pairwise assignment. This does not permit silent commercial rounding loss: finalization remains fail-closed when a non-zero compensation is material but cannot be represented, including the `2^53 + 1` boundary. Sum/compensation overflow and non-finite or negative inputs remain failures.

Existing explanation validation remains authoritative for face nullability, gross/deduction/net reconciliation, measurement trace consistency, volume reconciliation, and configured tolerances. The numeric correction changes only how the three cross-face formwork totals are accumulated.

## Deterministic regression

`QuantityGeometryExplanationAggregationPrecisionSmoke` self-registers in the Core smoke executable and covers:

- large-first `1e16 + 1 + 1 -> 10000000000000002` for Gross and Net totals plus `Validate`;
- small-first ordering of the same values;
- compensated Deduction total with valid per-face reconciliation;
- ordinary Gross/Deduction/Net selector isolation;
- strict final-unrepresentable `2^53 + 1` refusal;
- non-finite, null collection, and null-entry refusal.

Focused source contract:

```text
python scripts/preflight-quantity-geometry-explanation-aggregation-precision.py
```

Full repository acceptance still requires the normal Shared CI `preflight` and `core` jobs on the exact candidate before merge. Licensed BricsCAD runtime is not applicable to this deterministic Core reporting package.
