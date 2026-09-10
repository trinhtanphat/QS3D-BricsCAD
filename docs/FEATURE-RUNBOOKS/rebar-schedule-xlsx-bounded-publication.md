# Rebar Schedule XLSX bounded publication

Carrier: #6334 / PR #6335  
Lane: C02 Quantity / Estimating / Measurement / Export

## Defect and failure contract

The exporter accepts Excel-scale row counts and cell text, so retaining the full 15-column worksheet XML in an unbounded `StringBuilder` can amplify hostile-but-valid input before package validation or atomic replacement. Publication must fail closed before replacing an existing workbook when bounded output limits are exceeded.

## Required production invariants

- Snapshot and revalidate the source Count before publication.
- Preserve ElementId provenance and fabrication fields.
- Preserve deterministic column/schema order and invariant `R` numeric serialization.
- Emit worksheet XML incrementally through strict UTF-8.
- Bound each XML entry at 32 MiB, aggregate uncompressed XML at 64 MiB, and the final ZIP archive at 64 MiB using checked arithmetic.
- Apply deterministic ZIP timestamps and entry order.
- On any bounded-write/package failure, leave the destination untouched and clean the owned atomic temp file.

## Regression evidence

`XlsxRebarScheduleBoundedWriteSmoke` covers strict UTF-8 worksheet round-trip and a hostile Excel-valid text case that must raise `InvalidDataException` before destination replacement. `preflight-rebar-schedule-xlsx-bounded-write.py` guards the production shape and prevents reintroduction of the eager whole-sheet publication path.

Runtime classification: REMOTE_SAFE deterministic Core/export; no licensed BricsCAD runtime result is required or claimed for this carrier.
