# Dependency-impact transient root known-Count stability

## Boundary

This contract applies to caller-controlled root semantic element-id sequences consumed by `DependencyImpactPlanner.Plan(...)`. It is deterministic Core dependency/data-integrity behavior and does not require licensed BricsCAD runtime evidence.

## Required behavior

- Bind deterministic Count evidence at admission from generic/read-only/non-generic collection surfaces.
- Use explicit enumerator traversal so after every successful `MoveNext()` all supported Count surfaces are rebound before advertised-count/project-cap guards and before `IEnumerator.Current`.
- Rebound all supported Count surfaces again immediately after each `Current` read and before blank/canonical/duplicate validation or root retention.
- Reject transient Count growth, shrink, negative values, and cross-interface conflict at either caller-controlled `MoveNext` or `Current`, even if the source restores Count before the next traversal checkpoint.
- Preserve advertised-count over-yield and under-yield errors, project semantic-element cap, blank/canonical/duplicate validation, stable counted and streaming inputs, deterministic root sort, project freshness and dependency-topology checks.

## Regression evidence

`DependencyImpactPlannerTransientKnownCountSmoke` separately tracks `MoveNext`, `Current`, and post-Current Count reads. MoveNext-hostile sources still prove rejection before `Current` (`CurrentReads == 0`). Current-hostile sources expose a one-read transient Count change exactly from the `Current` accessor and prove the immediate post-Current rebound observes it before semantic retention.

Run auto-discovered feature guards and the deterministic Core smoke suite on the exact candidate SHA. Merge only after protected current-candidate `preflight + core` succeed and exact protected-main ancestry is verified.
