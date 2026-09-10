# Customer workbook bounded publication

Carrier: #6375 / C02.

`QsCustomerWorkbookExporter` must not retain all worksheet XML payloads at once or publish an unbounded XLSX archive. Customer-workbook output is hostile-input facing and therefore requires explicit per-entry, aggregate-uncompressed and final-archive byte ceilings in addition to Excel row/cell limits.

Required behavior:
- snapshot and revalidate caller Count channels before filesystem publication;
- preserve semantic scope, trace/provenance identity, invariant numeric formatting and formula-safe text;
- bound each worksheet XML entry, aggregate uncompressed XML and final ZIP bytes;
- use deterministic entry order and fixed ZIP timestamps;
- reject oversized accepted payloads without replacing an existing destination and without owned temp residue;
- validate malformed/oversized text before commit and preserve package validation.

Validation: run `python scripts/preflight-customer-workbook-bounded-publication.py`, focused customer-workbook preflights/smokes, then the full deterministic Core smoke suite. Merge only after current-main reconciliation and fresh exact-head protected Shared preflight+core GREEN.
