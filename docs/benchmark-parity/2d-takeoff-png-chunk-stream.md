# 2D Takeoff PNG chunk-stream admission

## Benchmark intent

CostX/Autodesk-style 2D takeoff must not publish sheet dimensions or immutable source evidence from a structurally incomplete PNG. PNG admission therefore validates the complete chunk stream, not only the signature, IHDR and terminal bytes.

## Admission contract

After signature and IHDR semantic validation, `Qs2DSheetIngestor` walks every chunk from IHDR through IEND using overflow-safe bounds. Every chunk CRC is verified. Exactly one leading IHDR is permitted, at least one IDAT must occur before IEND, IEND must have zero data length, and no bytes may follow IEND. Ancillary chunks and multiple IDAT chunks remain compatible.

Malformed chunk lengths, truncated chunks, duplicate IHDR, missing IDAT, intermediate CRC corruption, invalid IEND structure, and trailing bytes fail closed before `DrawingSheet2D`, raster dimensions, or SHA-256 source evidence are published.

## Workflow integration

The change strengthens only the ingestion/evidence boundary. Existing calibration, length/area/count markup, zones/layers, revision overlay/compare, classification, quantity extraction, formulas, inventory and estimate APIs are unchanged. Valid evidence continues through:

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

This keeps downstream quantity and commercial evidence deterministic while preventing invalid raster containers from entering the package workflow.

## Validation

`Qs2DSheetIngestionPngStructureSmoke` covers canonical PNGs, ancillary chunks, multiple IDAT chunks, missing IDAT, duplicate IHDR, corrupt intermediate CRC, oversized chunk lengths and bytes after IEND. `scripts/preflight-2d-takeoff-png-chunk-stream.py` binds production ordering so full chunk validation completes before raster drawing/evidence publication.
