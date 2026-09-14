# 2D Takeoff PDF version admission

This increment hardens the P0 CostX/Autodesk-style 2D sheet-ingestion boundary after the header-line admission work in #7033/#7056.

## Behavior

A PDF drawing is admitted only when its header version is one of the currently supported canonical generations: PDF 1.0 through 1.7, or PDF 2.0. Headers such as `%PDF-0.9`, `%PDF-1.8`, `%PDF-2.1`, or arbitrary future/invalid major versions fail closed before a `DrawingSheet2D` or SHA-256 evidence record is published.

The existing header end-of-line requirement and terminal `%%EOF` validation remain unchanged. The public `Qs2DSheetIngestor.IngestPdf` signature and persisted sheet/evidence contracts are unchanged.

## Workflow compatibility

Accepted sheets continue through the existing calibrated takeoff and package path:

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

This change does not introduce a second PDF parser, quantity authority, formula engine, classification system, revision model, snapshot model, or estimate model. Raster PNG/JPEG ingestion is unaffected.

## Validation

`Qs2DSheetIngestionPdfHeaderSmoke` now covers the supported range boundaries (1.0 and 2.0) and deterministic rejection of unsupported 0.9, 1.8, 2.1, and 9.9 headers. `scripts/preflight-2d-takeoff-pdf-version.py` guards source ordering and the regression coverage so version admission remains before evidence publication.
