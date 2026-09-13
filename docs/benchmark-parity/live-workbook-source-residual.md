# CostX Live Workbook source/dependency residual stability

## Problem

The live workbook engine already used compensated summation for dependency cascades, but it finalized that dependency total before combining it with the direct BIM element or drawing-handle quantity. That two-stage boundary can discard low-order residuals. A representative hostile case is a direct source contribution of `1` combined with dependency contributions `1e16` and `1`: aggregating the dependencies first can round their intermediate result before the source is added, while one deterministic compensated pass across all three contributions preserves the representable final `10000000000000002`.

A workbook cell or BOQ line could therefore carry a numerically incomplete value while its source linkage, revision, stale/fresh state, and trace all looked valid.

## Behavior

`LiveWorkbookRefreshEngine2` now feeds the direct authoritative source quantity and dependency values into the same deterministic compensated aggregation pass. Dependency ordering remains ordinal/deterministic, trace entries remain unchanged, and non-finite aggregation fails closed as `LiveWorkbookFreshness.Error` while retaining the previous accepted value.

The existing multiplier and offset contract is applied after the combined finite aggregate. Source revision comparison, stale propagation, conflict handling, missing-source handling, BOQ/cell linkage, and `ToNextBinding()` behavior are unchanged.

## Compatibility

This is a numerical-correctness hardening within the existing public contract. No enum, constructor, property, binding identifier, source identifier, workbook/BOQ identity, or refresh result shape changes. Consumers do not need a schema migration.

Existing workbooks can observe a corrected recalculated value only in floating-point edge cases where the former split aggregation lost a low-order but representable combined residual. That change is intentional and deterministic.
