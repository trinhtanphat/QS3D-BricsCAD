# Material Usage aggregation precision

## Purpose

Material Usage schedule rows aggregate source-safe quantity evidence by floor, material, component, category and family. A grouped deliverable must preserve finite non-negative contributions that are representable in the final binary64 result while continuing to fail closed when a contribution is actually swallowed by floating-point precision and the final compensated value still cannot represent that loss.

## Contract

`MaterialUsageScheduleBuilder` keeps isolated compensated state for `LengthM`, `AreaM2`, `VolumeM3` and `MassKg` for each grouping key. The public `MaterialUsageRow` is finalized only after all contributing elements have been consumed. This prevents an intermediate `1e16 + 1` rounding event from rejecting or discarding a later contribution that makes `1e16 + 1 + 1 = 10000000000000002` representable.

The implementation preserves checked `ElementCount`, grouping/order, material/family/category/unit semantics, `ElementIds`, `SourceHandles`, lazy quantity fallback rules, finite/non-negative validation and overflow/precision-loss refusal. The accumulator distinguishes an actually swallowed addend from an ordinary representational residual produced while adding decimal binary64 inputs such as `2.8 + 1.6`; the latter must continue to publish the normal binary64 result `4.4`. If a non-zero contribution was swallowed and a material final compensation still cannot alter the representable result, the builder fails closed; harmless sub-half-ULP residuals follow the established quantity-reporting rounding contract.

## Deterministic validation

`MaterialUsageAggregationPrecisionSmoke` covers high-dynamic-range length, area, volume and mass aggregation, ordinary decimal aggregation, input order, count/provenance behavior, final-unrepresentable refusal, invalid input and overflow. `scripts/preflight-material-usage-aggregation-precision.py` locks the compensated-group/finalization, swallowed-contribution distinction and provenance contract and is auto-discovered by the aggregate source-guard runner.

This package is Core-only and does not require licensed BricsCAD or Excel runtime evidence. Normal exact-head Shared CI, current-main reconciliation, protected PR `preflight` + `core`, strict freshness and expected-head merge rules still apply.
