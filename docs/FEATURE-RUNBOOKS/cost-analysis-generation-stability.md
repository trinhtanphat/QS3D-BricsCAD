# Cost analysis generation stability

## Scope

`BuildUpAnalysisService.Analyze` and `TradeCostAnalysisService.Analyze` accept both counted collections and raw streaming enumerables. Counted inputs are admitted against an authoritative Count, so publication must fail closed if an ordered semantic replay exposes a different same-Count generation.

For build-up analysis the semantic state is `RateCode` + `UnitRate`. For trade analysis it is `ItemCode` + `TradeCode` + `Cost`. Replacement or reordering of counted inputs is rejected before analysis results are returned.

Raw streaming inputs without authoritative Count remain single-pass compatible and are not replayed.

## Deterministic evidence

`CostAnalysisGenerationStabilitySmoke` covers same-count replacement for both services, stable counted replay, and streaming compatibility. `scripts/preflight-cost-analysis-generation-stability.py` is auto-discovered by the aggregate feature guard and pins the production + regression contract.

Runtime classification: `NOT_APPLICABLE`; this is pure Core behavior and does not claim licensed BricsCAD execution.
