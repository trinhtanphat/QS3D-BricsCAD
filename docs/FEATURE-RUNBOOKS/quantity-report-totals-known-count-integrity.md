# Quantity report totals known-Count integrity

Lane-Key: `issue-4513`

## Purpose

`QuantityReportTotals.FromRows` is a Core reporting boundary that accepts caller-controlled row enumeration while publishing commercial quantity totals. Completed #3902 bound supported deterministic Count evidence to traversal length. This follow-up closes two remaining observation-integrity gaps: the first item beyond an admitted Count must be rejected before `IEnumerator.Current` is observed, and deterministic Count evidence must remain stable through completed traversal.

## Contract

1. Snapshot every supported `ICollection<QuantityReportRow>`, `IReadOnlyCollection<QuantityReportRow>`, and non-generic `ICollection` Count surface before enumeration.
2. Preserve initial negative/conflicting Count rejection and exact under-yield rejection.
3. Traverse explicitly in `MoveNext -> admitted Count guard -> Current -> validation/arithmetic` order.
4. Rebind supported Count surfaces after exact traversal and fail closed if the effective Count or the set of deterministic Count surfaces changed, or if rebound evidence is negative/conflicting.
5. Preserve compensated quantity arithmetic, precision-loss/overflow guards, null-row diagnostics, checked row Count arithmetic, and pure-streaming enumerable behavior.

## Deterministic validation

`QuantityReportTotalsKnownCountIntegritySmoke` proves N+1 overrun is rejected after the N+1 `MoveNext` but before N+1 `Current`, under-yield still fails, post-traversal drift/negative/conflict fail closed, stable multi-interface Count remains accepted, and a pure streaming source remains supported.

`preflight-quantity-report-totals-known-count-integrity.py` is auto-discovered by aggregate source guards and pins traversal ordering, post-traversal rebind, smoke registration, and this runbook.

## Runtime boundary

This is deterministic Core reporting correctness. Licensed BricsCAD runtime is `NOT_APPLICABLE`; no `LOCAL_PASS` or private-DWG evidence is required or claimed.

## Landing boundary

Exact-head Shared branch CI must pass. Reconcile current `main` non-force if it advances, keep the diff limited to the four reserved paths, then require protected current-candidate `preflight` and `core` SUCCESS before expected-head merge and exact protected-main verification.
