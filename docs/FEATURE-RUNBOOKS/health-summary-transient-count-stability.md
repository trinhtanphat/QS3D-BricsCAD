# Health summary transient known-Count stability

## Contract

`HealthSummary` accepts caller-controlled `IEnumerable<ModelHealthIssue>` input, including streaming sources and collections that expose one or more supported Count surfaces. When Count metadata is present, it is part of the input integrity boundary rather than a capacity hint.

The implementation binds the supported Count-source set and exact value before traversal, revalidates that evidence immediately before and after each `MoveNext`, admits known-count and hard-cap cardinality before reading `IEnumerator.Current`, revalidates after terminal `MoveNext`, and revalidates before publication. Negative, conflicting, oversized, transiently changed, or source-set-changed Count evidence fails closed.

A pure streaming source with no supported Count surface remains single-pass and is governed by `MaxIssueCount`. Existing null diagnostic, severity, readiness, and immutable publication behavior remain unchanged.

## Deterministic acceptance

`HealthSummaryBoundedInputSmoke` preserves the historical cap/known-count/streaming contract. `HealthSummaryTransientCountStabilitySmoke` proves N+1 `Current` is not observed for an advertised Count=N and covers transient growth, shrink, negative and cross-interface conflict plus a stable multi-interface control.

`scripts/preflight-health-release-readiness.py` and `scripts/preflight-health-summary-transient-count-stability.py` pin the ordering so caller-controlled traversal cannot regress to a `while (MoveNext())` shape that checks known Count only after `Current` becomes observable.

Licensed BricsCAD runtime is not applicable to this deterministic Core diagnostics package.
