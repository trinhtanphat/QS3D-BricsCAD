# DeepCost known-Count traversal stability

## Scope

This contract covers caller-controlled enumerable materialization in `DeepCostWorkflows.cs`: rate-reference edges, build-up analysis rates, trade-analysis items, BQ library entries, and BQ project-import entries.

## Required traversal contract

When a source exposes a supported Count surface, QS3D binds the admitted Count before traversal and must rebind that same Count immediately before every caller-controlled `MoveNext()` and again after every successful move before reading `Current`. Transient growth, shrink, negative Count, or conflicting Count metadata therefore fails closed before the affected item is observed.

Pure streaming `IEnumerable<T>` sources remain supported. Stable counted sources retain exact under-yield/overrun validation. Existing maximum-entry/maximum-edge limits, null/duplicate validation, deterministic ordering, replace-existing semantics, arithmetic correctness, and post-traversal validation remain unchanged.

## Deterministic regression

`DeepCostTransientCountSmoke` uses hostile counted enumerables whose successful `MoveNext()` exposes a temporary invalid Count. Each affected DeepCost surface must reject the source with zero `Current` reads for the affected item. Stable counted controls and pure streaming controls must continue to succeed.

## Validation

Run the auto-discovered `scripts/preflight-deep-cost-known-count-stability.py` through aggregate preflight and run the full `QS3D.Core.SmokeTests` Release harness. Hosted Core/static validation is authoritative for this package; no licensed BricsCAD runtime claim is required.
