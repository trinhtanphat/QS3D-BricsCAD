# Estimating portfolio aggregation precision

## Purpose

Protect commercial estimating totals from order-dependent decimal precision rejection when individually valid non-negative contributions have a representable complete exact aggregate.

## Production contract

`EstimatingPortfolio.PricedTotal` must aggregate every priced line amount through one complete exact aggregate before converting the final mathematical total back to `decimal`. A temporary loss of significance in pairwise decimal arithmetic must not reject a later-recoverable result such as `1e28 + 0.5 + 0.5 = 1e28 + 1`.

The same exact aggregation contract applies to the bulk-rate preview's total-before, total-after, and unit quantity values so preview arithmetic cannot disagree with `PricedTotal` solely because of accumulation order.

Unpriced lines remain excluded from `PricedTotal`. Blocked and unmatched bulk-rate rows preserve the existing preview behavior. Individual line `Amount` multiplication keeps the existing fail-closed overflow/underflow contract.

The accumulator accepts only non-negative values in this commercial path. It retains integer coefficient and decimal scale state, reduces trailing decimal zeros only at finalization, and throws when the final mathematical total cannot be represented by `decimal`.

## Deterministic regression

`EstimatingPortfolioAggregationPrecisionSmoke` covers:

- complete exact aggregate recovery for `1e28 + 0.5 + 0.5`;
- ordinary priced totals while unpriced lines remain excluded;
- bulk-rate preview total-before/total-after parity and exact unit quantity aggregation;
- fail-closed final overflow using `decimal.MaxValue + 1`.

## Validation

Run the auto-discovered `scripts/preflight-estimating-portfolio-aggregation-precision.py`, then the normal Core build/smoke lane. Protected PR `preflight` and `core` must both succeed on the exact current candidate before merge.

Runtime classification is `NOT_APPLICABLE`: this is deterministic Core/commercial numeric correctness and requires no licensed BricsCAD runtime or `LOCAL_PASS` evidence.
