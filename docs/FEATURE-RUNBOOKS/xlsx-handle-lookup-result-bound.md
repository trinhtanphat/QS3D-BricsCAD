# Xlsx handle lookup result identity materialization bound

Lane-Key: `issue-5086`

## Scope

This runbook covers the public `XlsxHandleLookupResult` constructors that accept caller-controlled handle and element-id enumerables. Runtime qualification is **NOT_APPLICABLE** because the contract is deterministic Core export/provenance materialization.

## Defect

The historical constructors used unbounded LINQ materialization for `IEnumerable<string>` handles and element IDs. A hostile or simply excessive sequence could keep enumeration running indefinitely or drive unbounded CPU/memory before a lookup result was published. This public boundary therefore lacked the finite cardinality contract already implied by the XLSX worksheet column ceiling.

## Production contract

1. Accept non-null handle and element-id enumerables.
2. Observe at most 16,384 raw identity values from each enumerable.
3. Fail closed on the first over-bound observation instead of continuing materialization.
4. Preserve whitespace filtering, trimming, case-insensitive de-duplication and first-occurrence order.
5. Preserve drawing-fingerprint normalization and existing public result properties.
6. Do not require licensed BricsCAD or workbook IO to validate this constructor boundary.

## Deterministic regression

`XlsxHandleLookupResultBoundSmoke` requires both handle and element-id sequences to reject at the first 16,385th observation and verifies stable canonicalization/de-duplication behavior. The auto-discovered `preflight-xlsx-handle-lookup-result-bound.py` pins the explicit bounded materializer and rejects regression to the historical unbounded LINQ pipeline.

## Landing

Run the focused preflight, aggregate discovered feature guards and deterministic Core smoke/build. Protected exact-head `preflight + core` must be green. If protected `main` advances, reconcile the same canonical branch non-force after collision-scanning all four reserved paths, then obtain fresh exact-head protected checks before expected-head merge.
