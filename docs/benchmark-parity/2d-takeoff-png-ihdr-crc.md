# 2D Takeoff PNG IHDR CRC admission

Issue: #6943  
Lane: P0 CostX/Autodesk-style 2D Takeoff

## Purpose

Raster sheet ingestion already validates the PNG signature, canonical first `IHDR` chunk, dimensions and terminal `IEND` shape. The authoritative pixel dimensions used by calibration and downstream takeoff must also belong to an intact IHDR chunk.

`Qs2DSheetIngestor` now validates the PNG-specified CRC-32 over the `IHDR` type and 13-byte IHDR data before reading/publishing width and height. A payload whose IHDR bytes or stored CRC were corrupted is rejected before it can become a `DrawingSheet2D` source.

## Compatibility

Public ingestion APIs and the returned `IngestedDrawingSheet2D` contract are unchanged. Valid PNG files are accepted as before. Legacy synthetic fixtures that used placeholder/zero IHDR CRC bytes must be updated to carry the canonical CRC; real PNG encoders already provide it.

PDF and JPEG admission are unchanged by this carrier. Terminal IEND handling remains under the existing structure fence and can be hardened independently without widening this reservation.

## Quantity/evidence safety

Pixel dimensions are part of the raster evidence boundary because calibrated length/area quantities depend on the admitted drawing geometry. IHDR CRC validation therefore occurs before width/height extraction so corrupted raster metadata cannot silently influence the workflow Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate.

## Validation

`Qs2DSheetIngestionPngStructureSmoke` now builds a valid IHDR CRC and proves that both an IHDR data-bit mutation and a stored-CRC mutation fail closed. `scripts/preflight-2d-takeoff-png-ihdr-crc.py` enforces the validation ordering and required regression coverage.
