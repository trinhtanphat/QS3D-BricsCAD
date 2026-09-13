# 2D takeoff sheet source-kind validation

`Qs2DSheetIngestor` now checks recognizable source-reference extensions against the ingestion mode before publishing a `DrawingSheet2D`.

- `IngestPdf` rejects `.png`, `.jpg`, and `.jpeg` references.
- `IngestRaster` rejects `.pdf` references.
- Query strings and URI fragments do not affect extension recognition.
- Opaque, extensionless, and unrecognized references remain accepted for compatibility; payload signature validation remains authoritative for actual PDF/PNG/JPEG bytes.

This prevents contradictory provenance such as a PDF payload being published as evidence sourced from `A101.png`, while avoiding a breaking change for document stores that use opaque IDs rather than filenames.

## Workflow impact

The validation occurs at sheet ingestion, before calibration, markup extraction, revision comparison, package classification, quantity/formula evaluation, inventory publication, or estimate calculation. Existing `DrawingSheet2D` construction remains unchanged for compatibility; callers ingesting file payloads should use `Qs2DSheetIngestor`.

## Migration

If a caller intentionally used a misleading recognized extension, rename the source reference to match the payload type or switch to an opaque/extensionless document identifier. No change is required for matching `.pdf`, `.png`, `.jpg`, `.jpeg`, URI references with query/fragment suffixes, or existing opaque references.
