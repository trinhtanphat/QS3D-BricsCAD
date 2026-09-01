# Advanced cost aggregation precision

Advanced commercial calculations must preserve non-negative decimal contributions across the complete bounded aggregate whenever the final exact result is representable as `decimal`.

Two production paths share this boundary. A rate build-up must derive `DirectUnitCost` from the complete component-cost aggregate rather than reject a small contribution merely because an earlier partial decimal sum cannot represent it. A benchmark average must likewise derive its numerator from the complete exact sample sum before dividing by the sample count.

The canonical high-dynamic-range regression is `10000000000000000000000000000m + 0.5m + 0.5m`. The exact final total is `10000000000000000000000000001m`; both rate build-up and benchmark average behavior must be order-independent for permutations of those contributions.

This does not permit silent overflow or rounding-away of a final exact result. If the complete coefficient cannot be represented as `decimal`, the operation must fail closed with `OverflowException`. Existing non-negative input requirements, duplicate resource protection, maximum-entry limits, Count stability, deterministic ordering, overhead/profit behavior, benchmark median behavior and deviation calculations remain intact.

Validation is deterministic Core-only. The regression covers recoverable high-dynamic-range aggregation, input-order permutations, ordinary controls, and a final-unrepresentable rate build-up. There is no licensed BricsCAD runtime or private-DWG acceptance requirement for this package.
