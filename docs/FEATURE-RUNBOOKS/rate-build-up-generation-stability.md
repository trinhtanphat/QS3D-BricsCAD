# Rate build-up generation stability

## Scope

`CostRateBuildUp` accepts both counted component collections and raw streaming enumerables. Counted inputs are admitted against authoritative Count evidence and must prove the same ordered semantic generation before commercial totals are published.

The replay identity is the exact component state: `ResourceCode`, `Description`, `Unit`, `QuantityPerBillUnit`, and `UnitRate`. Same-Count replacement or reordering must fail closed. Raw streaming inputs without authoritative Count remain single-pass compatible.

## Deterministic evidence

`RateBuildUpGenerationStabilitySmoke` covers same-count replacement rejection, stable counted admission plus one replay, and streaming single-pass compatibility. `scripts/preflight-rate-build-up-generation-stability.py` is auto-discovered and pins the production/regression contract.

Runtime classification: `NOT_APPLICABLE`; this is deterministic Core/commercial correctness and does not claim licensed BricsCAD execution.
