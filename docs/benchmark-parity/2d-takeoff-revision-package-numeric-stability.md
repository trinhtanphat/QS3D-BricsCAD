# Revision package numeric stability

Revision package and group quantity projections must preserve representable low-order residuals across high-dynamic-range cancellation. The review projection uses the canonical `QuantityReportMath.FiniteAccumulator` rather than a local Kahan variant, while continuing to reject non-finite input/output and overflow.

Regression coverage includes previous `1e16` versus current `1e16 + 1`, proving signed package and Classification/Zone/Layer/Unit group delta remains `1`. Added/Removed/Changed/Unchanged counts and markup evidence cardinality remain independent from numeric accumulation.

This is host-neutral Core behavior. It does not establish licensed BricsCAD runtime evidence.
