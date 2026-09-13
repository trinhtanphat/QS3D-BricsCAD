# 2D Takeoff raster payload completeness

Issue: #6834

## Boundary

`Qs2DSheetIngestor.IngestRaster` now rejects structurally truncated raster payloads after format/dimension detection but before publishing an `IngestedDrawingSheet2D`.

- PNG must end with a zero-length `IEND` chunk marker.
- JPEG must end with the `FF D9` EOI marker.
- Existing PNG/JPEG source-reference validation, dimension extraction, SHA-256 provenance, calibration, markup extraction, zones/layers, classification, formula, inventory, and estimate semantics are unchanged.

## Compatibility

Valid PNG/JPEG drawing sheets continue through the existing Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate workflow without API changes. Historical synthetic fixtures that supplied only enough bytes to expose dimensions must add their required format terminators.

The boundary intentionally fails closed before raster format admission so truncated bytes cannot become takeoff evidence or downstream estimate input.

## Validation

`QsTakeoffPackageUxSmoke` uses complete PNG/JPEG fixtures and verifies truncated PNG/JPEG rejection. `scripts/preflight-takeoff-raster-completeness.py` locks the source contract and validation-before-admission ordering.
