# Quantity report input traversal bound

## Scope

`QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` is a public Core reporting boundary. It must remain deterministic for both counted collections and count-less streaming inputs without allowing caller-controlled enumeration to grow report state without a finite ceiling.

Runtime classification: `NOT_APPLICABLE`. This contract is deterministic Core/report correctness and does not establish licensed BricsCAD `LOCAL_PASS`.

## Invariant

The supported input ceiling is **10,000 elements**.

For inputs exposing any supported Count contract (`ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`):

- negative or conflicting Count contracts remain fail-closed;
- Count greater than 10,000 is rejected at admission, before enumeration begins;
- the admitted Count remains stable throughout traversal and after each `Current` read;
- over-yield and under-yield remain fail-closed.

For every successful traversal step, ordering is intentionally:

`Count stability -> MoveNext -> Count stability -> known-count overrun -> 10,000 ceiling -> Current -> Count stability -> semantic acceptance`

This ordering means a count-less stream may contribute exactly 10,000 elements, but after the 10,001st successful `MoveNext` the builder throws **before observing `Current`**. No 10,001st element may enter duplicate-ID checks, grouping, provenance, count aggregation, or quantity accumulation.

## Preserved behavior

The bound must not change:

- duplicate element-ID rejection;
- grouping identity/order;
- Floor/Category/Family/Material semantics;
- element/source-handle provenance;
- checked row counts;
- compensated finite quantity accumulation;
- existing Count drift/agreement checks;
- read-only result publication;
- normal count-less streaming inputs within the ceiling.

Failure occurs before a result is returned, so callers never receive a partial report.

## Deterministic regression

`QuantityReportInputBoundSmoke` covers:

1. a known Count of 10,001 rejected before `GetEnumerator`;
2. a count-less overrun where `MoveNext` succeeds 10,001 times but `Current` is read exactly 10,000 times;
3. a count-less exact-boundary stream of 10,000 entries that remains accepted;
4. an ordinary short streaming control.

`preflight-quantity-report-input-bound.py` pins the source ordering and ceiling so future refactors cannot move the streaming guard after `Current` or remove admission rejection.

## Acceptance

Before merge, require current exact-head Shared CI `preflight` and `core` success, latest-main reconciliation/collision scan, protected PR checks, expected-head merge, and exact protected-main verification under the repository's current rules.
