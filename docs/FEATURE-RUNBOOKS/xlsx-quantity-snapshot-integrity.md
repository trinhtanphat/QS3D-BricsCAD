# Quantity XLSX snapshot integrity

Carrier: #4596  
Lane-Key: `issue-4596`

## Contract

`XlsxQuantityExporter` must fail closed if a caller-controlled row collection changes cardinality while Standard or ED2 rows are being snapshotted. The exporter must also reject provenance-list Count drift while copying `ElementIds` or `SourceHandles` into the detached snapshot.

The stability checks belong to the in-memory snapshot phase, before `ExportCore` creates directories, temporary package bytes, or replaces an existing destination. A rejected drift therefore preserves existing customer workbook bytes and cannot publish a truncated/torn semantic scope.

## Deterministic regression

`XlsxQuantityNullRowSmoke` carries adversarial `IReadOnlyList<QuantityReportRow>` implementations whose first `Count` read advertises one row and whose later read reports two. Standard export and ED2 detail export must both reject this drift with an explicit snapshot-stability error and preserve an existing destination sentinel.

The source guard `scripts/preflight-xlsx-quantity-snapshot-integrity.py` locks post-traversal Count rebinds for Standard rows, ED2 rows, and copied provenance lists.

## Acceptance

1. Source guard passes.
2. Core build and deterministic smoke suite pass.
3. Shared CI `preflight` and `core` jobs pass on the exact reconciled candidate.
4. Candidate is reconciled to current protected `main` before merge.
5. Merge uses the exact expected head and exact protected `main` is verified afterward.

Runtime classification: `NOT_APPLICABLE`; this is deterministic Core export/data-integrity work and does not claim licensed BricsCAD runtime evidence.
