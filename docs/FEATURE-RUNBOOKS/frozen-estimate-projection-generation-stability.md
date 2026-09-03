# Frozen estimate projection generation stability

## Scope

Issue #5488 hardens `FrozenEstimateProjection.Create` against caller-controlled counted collections that preserve every exposed `Count` while changing ordered `EstimateLine` content between enumerations.

## Failure boundary

When an input exposes an authoritative known count, the projection must replay the source before publication and require exact ordered equality of the frozen row semantics captured by the admitted traversal. If any projected field changes, a row disappears/appears, or order changes while Count remains constant, creation fails closed before a `FrozenEstimateProjection` is returned.

The equality contract covers every immutable `FrozenEstimateProjectionRow` field: estimate line identity, measurement provenance, rate-book/rate-item provenance, rate timestamp/version, cost code/unit/currency, measured/commercial/estimating quantities, commercial reason, unit rate, and final amount. Object reference identity is not the contract.

Inputs with no authoritative known count remain single-pass compatible and are never replayed.

Existing 10,000-line capacity, negative/conflicting/changing Count, traversal-cardinality, duplicate line ID, null-line, deterministic sort and projection semantics remain authoritative.

## Deterministic validation

`FrozenEstimateProjectionGenerationStabilitySmoke` covers same-count replacement, same-count reorder, stable counted replay, raw streaming single-pass behavior and exact projected-row stability.

`scripts/preflight-frozen-estimate-projection-generation-stability.py` is auto-discovered by aggregate feature source guards and binds production + regression structure.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core/cost correctness; no licensed BricsCAD evidence is required or claimed.
