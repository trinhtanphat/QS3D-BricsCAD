# Customer workbook snapshot Count integrity

Carrier: #5064 / Lane-Key `issue-5064`.

## Contract

`QsCustomerWorkbookExporter.Export` treats every Count channel exposed by each caller-provided detail/summary row source as admission evidence, not only `IReadOnlyList<QuantityReportRow>.Count`. The exporter must bind the read-only, generic `ICollection<T>`, and non-generic `ICollection` channels that are actually present, require them to agree and remain within the Excel row bound, and revalidate the same admitted channels around every caller-controlled row indexer and again before filesystem publication.

This closes two deterministic races left by the earlier single-channel snapshot guard: conflicting Count interfaces can otherwise be accepted at admission, and a secondary Count channel can drift transiently during an indexer then recover before the final `IReadOnlyList.Count` rebound. Both states must fail closed without replacing or creating output.

Existing quantity evidence/provenance validation, detail-vs-summary semantic-scope checks, trace generation and atomic commit behavior remain unchanged.

## Deterministic regression

`QsCustomerWorkbookSnapshotCountIntegritySmoke` retains shrink/growth regressions and adds hostile multi-interface sources that:

- expose a conflicting generic Count at admission and must fail before the first row indexer;
- change only `ICollection<QuantityReportRow>.Count` during the first row indexer, then restore it after the first Count read, proving transient drift is caught immediately;
- expose stable agreeing read-only/generic/non-generic Count channels and must still export successfully.

Every refusal preserves an existing destination byte-for-byte and leaves no temp/output residue.

## Source guard

Run:

```bash
python scripts/preflight-customer-workbook-snapshot-count-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused guard requires the multi-channel known-count contract, revalidation immediately around indexers, final pre-publication revalidation, hostile regression evidence, and rejects the former single-channel final `source.Count != admittedCount` pattern.

Runtime classification: `NOT_APPLICABLE`; this is deterministic Core quantity/export correctness and does not require licensed BricsCAD or a private DWG.
