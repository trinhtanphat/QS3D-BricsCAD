# 2D Takeoff PDF header-line admission

This increment hardens the P0 CostX/Autodesk-style 2D sheet-ingestion boundary for PDF drawings.

## Behavior

A PDF payload must begin with `%PDF-x.y` and the version token must be followed immediately by a PDF end-of-line marker (`LF`, `CR`, or `CRLF`). Payloads that append spaces or arbitrary data directly to the version token fail closed before a `DrawingSheet2D` or its SHA-256 evidence is published. Existing terminal `%%EOF` validation remains in place.

The public `Qs2DSheetIngestor.IngestPdf` API is unchanged. Valid PDF sheet admission therefore continues to feed the existing calibrated takeoff path without changing quantity semantics or persisted contracts.

## Workflow compatibility

The evidence remains source-bound by SHA-256 and page number and continues through the established benchmark flow:

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

No formula, classification, quantity, revision, snapshot, estimate, raster PNG/JPEG, or BricsCAD-host API is changed by this increment.

## Validation

`Qs2DSheetIngestionPdfHeaderSmoke` covers LF, CR and CRLF valid headers plus malformed inline-data, space-delimited and truncated version-header cases. `scripts/preflight-2d-takeoff-pdf-header-line.py` guards production ordering and smoke registration.
