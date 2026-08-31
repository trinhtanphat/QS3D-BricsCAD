# Dependency-impact transient root known-Count stability

## Boundary

This contract applies to caller-controlled root semantic element-id sequences consumed by `DependencyImpactPlanner.Plan(...)`. It is deterministic Core dependency/data-integrity behavior and does not require licensed BricsCAD runtime evidence.

## Required behavior

- Bind deterministic Count evidence at admission from generic/read-only/non-generic collection surfaces.
- Use explicit enumerator traversal so after every successful `MoveNext()` all supported Count surfaces are rebound before advertised-count/project-cap guards and before `IEnumerator.Current`.
- Reject transient Count growth, shrink, negative values, and cross-interface conflict even if the source would restore Count when `Current` is read or before final rebound.
- Preserve advertised-count over-yield and under-yield errors, project semantic-element cap, blank/canonical/duplicate validation, stable counted and streaming inputs, deterministic root sort, project freshness and dependency-topology checks.

## Regression evidence

`DependencyImpactPlannerTransientKnownCountSmoke` separately tracks `MoveNext` and `Current`. Hostile sources make Count unstable immediately after successful `MoveNext()` and restore only when `Current` is observed, so rejection must occur with `CurrentReads == 0`.

Run auto-discovered feature guards and the deterministic Core smoke suite on the exact candidate SHA. Merge only after protected current-candidate `preflight + core` succeed and exact protected-main ancestry is verified.
