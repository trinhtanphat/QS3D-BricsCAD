# Quantity report totals known-Count integrity

Lane-Key: `issue-4888`

## Purpose

`QuantityReportTotals.FromRows` is a Core reporting boundary that accepts caller-controlled row enumeration while publishing commercial quantity totals. Existing coverage binds supported deterministic Count evidence to traversal length, rejects the first item beyond an admitted Count before `IEnumerator.Current`, and requires Count evidence to remain stable through traversal. This follow-up closes the remaining acceptance boundary: a caller-controlled `IEnumerator.Current` must not be able to mutate admitted Count metadata and begin null/row validation or quantity accumulation before cardinality integrity is rejected.

## Contract

1. Snapshot every supported `ICollection<QuantityReportRow>`, `IReadOnlyCollection<QuantityReportRow>`, and non-generic `ICollection` Count surface before enumeration.
2. Preserve initial negative/conflicting Count rejection and exact under-yield rejection.
3. Traverse explicitly in `Count -> MoveNext -> Count -> admitted Count guard -> Current -> Count -> validation/arithmetic` order.
4. Rebind supported Count surfaces immediately after each successful `Current` read and before null validation, row Count arithmetic, or any compensated quantity accumulation.
5. Rebind supported Count surfaces after completed traversal and fail closed if the effective Count or the set of deterministic Count surfaces changed, or if rebound evidence is negative/conflicting.
6. Preserve compensated quantity arithmetic, precision-loss/overflow guards, null-row diagnostics, checked row Count arithmetic, and pure-streaming enumerable behavior.

## Deterministic validation

`QuantityReportTotalsKnownCountIntegritySmoke` proves N+1 overrun is rejected after the N+1 `MoveNext` but before N+1 `Current`, under-yield still fails, post-traversal drift/negative/conflict fail closed, stable multi-interface Count remains accepted, and a pure streaming source remains supported.

`QuantityReportTotalsCurrentCountAcceptanceSmoke` adds a hostile counted enumerable whose `Current` access mutates Count while returning a null row. The required outcome is the Count-integrity `InvalidOperationException`, not the competing null-row `ArgumentException`, proving cardinality drift is rejected before semantic row acceptance starts. A stable counted control remains accepted.

`preflight-quantity-report-totals-known-count-integrity.py` is auto-discovered by aggregate source guards and pins the full traversal ordering, post-Current Count rebound, post-traversal rebind, smoke registration, and this runbook.

## Runtime boundary

This is deterministic Core reporting correctness. Licensed BricsCAD runtime is `NOT_APPLICABLE`; no `LOCAL_PASS` or private-DWG evidence is required or claimed.

## Landing boundary

Exact-head Shared branch CI must pass. Reconcile current `main` non-force if it advances, keep the diff limited to the four reserved paths, then require protected current-candidate `preflight` and `core` SUCCESS before expected-head merge and exact protected-main verification.