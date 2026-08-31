# Customer workbook snapshot Count integrity

Carrier: #4601 / Lane-Key `issue-4601`.

## Contract

`QsCustomerWorkbookExporter.Export` treats each caller-provided detail/summary `IReadOnlyList<QuantityReportRow>` cardinality as admission evidence. The exporter binds that Count once, rejects zero or Excel-over-limit cardinality, traverses exactly the admitted number of rows, and re-reads Count after detaching the snapshot. Any shrink or growth during traversal fails closed before destination filesystem mutation or workbook publication.

This preserves the existing row evidence/provenance validation and detail-vs-summary semantic-scope checks; the change only closes the caller-controlled cardinality race at the snapshot boundary.

## Deterministic regression

`QsCustomerWorkbookSnapshotCountIntegritySmoke` supplies adversarial `IReadOnlyList` implementations whose Count changes only after the first indexed row read. It covers both shrink (2 -> 1) and growth (1 -> 2), requires `InvalidDataException`, verifies an existing destination remains byte-for-byte unchanged, and requires no temp/output residue.

## Source guard

Run:

```bash
python scripts/preflight-customer-workbook-snapshot-count-integrity.py
```

The guard requires bound detail/summary counts, admitted-count traversal and post-snapshot Count rebound, while rejecting the former live `source.Count` loop/capacity pattern.

Runtime classification: `NOT_APPLICABLE`; this is deterministic Core quantity/export integrity and does not require licensed BricsCAD or a private DWG.
