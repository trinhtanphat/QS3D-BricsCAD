# Trade Cost aggregation precision

`TradeCostAnalysisService` must preserve every non-negative `TradeCostItem.Cost` contribution exactly across a bounded trade group whenever the representable final total fits `decimal`.

The production boundary is an exact base-10 accumulator. Each decimal contributes its unsigned 96-bit coefficient and scale; the accumulator aligns coefficients with integer powers of ten and does not round while rows are being collected. Only after the complete trade group is known is the exact coefficient normalized and converted back to `decimal`.

This makes a representable final total independent of intermediate decimal scale loss. A regression case is `10000000000000000000000000000m + 0.5m + 0.5m`, whose exact final total is `10000000000000000000000000001m`. The result must be order-independent for the same trade group.

The correction does not weaken existing fail-closed arithmetic. A final exact coefficient greater than the 96-bit decimal maximum, including `decimal.MaxValue + 1m`, must still throw `OverflowException`. Negative costs remain rejected by `TradeCostItem` and by the internal aggregate contract.

Existing commercial semantics remain unchanged: duplicate item codes fail closed, trade-code grouping remains case-insensitive with deterministic display casing and ordering, item counts use checked arithmetic, CFA division still uses `CostDecimalMath.DividePreservingNonZero`, and caller-controlled input retains the existing Count stability checks and maximum-entry bounds.

Validation is deterministic Core-only: the smoke covers recoverable high-dynamic-range totals, input-order permutations, independent trade groups, ordinary totals, and unrepresentable final overflow. This feature requires no licensed BricsCAD runtime and no private DWG acceptance.
