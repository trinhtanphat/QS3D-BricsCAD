# 2D Takeoff PNG IHDR semantic admission

Issue #7091 hardens the P0 CostX/Autodesk-style 2D drawing-ingestion boundary for PNG sheets.

## Evidence boundary

A PNG drawing is admitted only after the signature, exact first `IHDR` chunk shape, `IHDR` CRC, semantic `IHDR` fields, positive dimensions, and terminal `IEND` CRC have all passed. Semantic validation is intentionally performed before width/height are returned to `IngestRaster`, so an impossible image header cannot become a `DrawingSheet2D`, SHA-256 source evidence, package input, or downstream quantity provenance.

Supported PNG semantics follow the PNG IHDR contract:

- color type 0: bit depth 1, 2, 4, 8, or 16;
- color type 2: bit depth 8 or 16;
- color type 3: bit depth 1, 2, 4, or 8;
- color type 4 or 6: bit depth 8 or 16;
- compression method 0 only;
- filter method 0 only;
- interlace method 0 (none) or 1 (Adam7).

Reserved/invalid color types, unsupported color-type/bit-depth pairs, nonzero compression/filter methods, and interlace values above 1 fail closed even when the IHDR CRC is internally consistent.

## Compatibility

This change does not alter public ingestion APIs, calibration, length/area/count markup, zones/layers, revision compare/overlay, quantity extraction, classification, formula evaluation, snapshots, inventory, or estimating contracts. Existing standards-compliant PNG drawings remain accepted, including Adam7-interlaced images.

The downstream benchmark workflow remains:

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

The only behavioral change is that malformed PNG headers are rejected earlier, before they can supply dimensions or immutable source evidence to that workflow.

## Validation

`Qs2DSheetIngestionPngStructureSmoke` exercises accepted canonical color models plus fail-closed invalid color type, bit-depth pairing, compression, filter, and interlace cases. The malformed cases are generated with a matching IHDR CRC so the smoke proves the semantic guard itself. `scripts/preflight-2d-takeoff-png-ihdr-semantics.py` binds source ordering and deterministic smoke registration.
