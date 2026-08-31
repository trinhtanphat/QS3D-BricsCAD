# Rebar stock-demand known-Count integrity

## Scope

This runbook covers the deterministic Core boundary in `RebarStockDemand` where caller-controlled `IReadOnlyList<RebarCutRequirement>` inputs expose known Count evidence and are materialized into immutable stock-demand quantities.

Runtime classification: `NOT_APPLICABLE`. No licensed BricsCAD host or private DWG is required for this contract.

## Correctness contract

Before traversal, all available Count surfaces (`IReadOnlyCollection<T>`, `ICollection<T>`, and non-generic `ICollection`) must be non-negative, within the 10,000 requirement bound, and mutually consistent.

Traversal must use explicit enumeration in this order after each successful `MoveNext()`:

1. reject an admitted known-Count overrun;
2. retain the independent 10,000 safety bound;
3. only then observe `IEnumerator.Current`;
4. validate identity/null semantics and accumulate quantities.

After traversal, the materialized row count must exactly equal the admitted Count. Count surfaces must then be rebound and must still be valid, mutually consistent, and equal to the initially admitted Count before any immutable quantity state is published.

This makes Count drift, post-traversal interface conflict, negative post-traversal Count, over-yield, and under-yield fail closed.

## Deterministic acceptance

Run the registered Core smoke suite and the auto-discovered source guard:

- `tests/QS3D.Core.SmokeTests/RebarStockDemandKnownCountIntegritySmoke.cs`
- `scripts/preflight-rebar-stock-demand-known-count-integrity.py`

The adversarial smoke independently records `MoveNext` and `Current` access so Count=1/yield=2 must perform two `MoveNext` calls but only one `Current` read. It also covers under-yield, post-traversal Count drift/conflict/negative evidence, and stable counted behavior.

## Non-goals

This package does not alter rebar optimization policy, stock-bar selection, kerf policy, allowance policy, CAD runtime behavior, or licensed BricsCAD validation.
