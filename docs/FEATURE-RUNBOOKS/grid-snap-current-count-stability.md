# Grid snap Current-induced Count stability

## Scope

This runbook covers the shared `GridSnapInputMaterializer` used by LINE and ARC snap planners. It is a deterministic Core integrity contract and does not require licensed BricsCAD runtime evidence.

## Invariant

When a caller enumerable exposes an admitted known `Count`, the materializer must revalidate that same Count immediately before and after caller-controlled `MoveNext()`, immediately after caller-controlled `Current` and before retaining the returned curve, and again after traversal.

The ordering preserves existing safeguards: known-count overrun and the configured curve ceiling are checked before reading excess `Current`; negative or conflicting Count surfaces fail closed; under-yield is rejected after traversal; pure streaming enumerables remain supported.

## Regression

`GridSnapCurrentCountStabilitySmoke` exercises both LINE and ARC through stable instrumented `IReadOnlyCollection<GridReferenceCurve>` sources. A one-item source must observe seven Count reads: admission, pre/post first `MoveNext`, post-`Current`, pre/post terminal `MoveNext`, and final rebound. The source guard `scripts/preflight-grid-snap-current-count-stability.py` pins the production ordering and regression registration.

## Acceptance

Run the repository deterministic smoke suite and auto-discovered feature guards. Protected PR `preflight` and `core` must both succeed on the exact current candidate before merge. Runtime classification is `NOT_APPLICABLE`; remote/static evidence must not be reported as licensed `LOCAL_PASS`.
