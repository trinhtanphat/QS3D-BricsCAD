# C02 quantity accumulator canonicalization

## Scope
Issue #6970 removes five independent compensated-sum implementations from 2D quantity aggregation, 2D takeoff workflow, live workbook dependency aggregation, Cubicost reviewed inventory aggregation, and takeoff-package estimated-cost publication. All five now delegate numeric authority to `QuantityReportMath.FiniteAccumulator` while keeping their surface-specific validation and error behavior.

## Proven regression
The deterministic positive-only sequence `[1e16, 3, 2, 1e-16]` is intentionally used because the former local algorithm returned `10000000000000006` while the canonical finite accumulator returns `10000000000000004`. This demonstrates order/normalization drift without relying on signed cancellation.

## Required invariants
- Input order remains deterministic; this change does not reorder evidence or grouping keys.
- 2D aggregation and takeoff keep their existing finite-input labels and reject non-finite quantities.
- Live workbook converts non-finite/overflow aggregation failure to its existing unusable/fail-closed result instead of publishing a partial value.
- Cubicost keeps explicit invalid-input and overflow diagnostics and does not change source/evidence cardinality.
- Takeoff-package estimated cost uses the same canonical finite authority and preserves evidence cardinality plus fail-closed overflow behavior.
- Zero normalization remains owned by the canonical accumulator.
- Provenance/evidence collections are not deduplicated or re-counted by this change.
