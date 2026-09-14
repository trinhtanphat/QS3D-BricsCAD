# 2D Takeoff PNG IEND CRC admission

Issue: #6961  
Lane: P0 CostX/Autodesk-style 2D Takeoff

## Purpose

Raster sheet ingestion already validates the PNG signature, canonical first `IHDR` chunk and its CRC, dimensions, and terminal zero-length `IEND` chunk shape. The terminal integrity field must also be valid before the payload is admitted as drawing evidence.

`Qs2DSheetIngestor` now validates the PNG-specified CRC-32 over the terminal `IEND` chunk type. A payload whose stored IEND CRC is corrupted is rejected before it can become an `IngestedDrawingSheet2D` source.

## Compatibility

The public PDF/JPEG/PNG ingestion APIs and `IngestedDrawingSheet2D` contract are unchanged. Valid PNG files are accepted as before. Legacy synthetic PNG fixtures that used placeholder/zero IEND CRC bytes must carry the canonical IEND CRC (`AE 42 60 82`).

IHDR validation, raster dimensions, scale calibration, length/area/count markup, zones/layers, revision compare, quantity extraction and package/estimate APIs are otherwise unchanged.

## Quantity/evidence safety

The source SHA-256 identifies the exact admitted drawing bytes used by calibrated takeoff and revision evidence. Requiring a valid terminal IEND CRC closes the remaining terminal PNG integrity gap so malformed raster bytes are not admitted into Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate workflows.

## Validation

`Qs2DSheetIngestionPngStructureSmoke` emits the canonical IEND CRC and proves a stored-CRC mutation fails closed while a valid PNG remains accepted. `scripts/preflight-2d-takeoff-png-iend-crc.py` guards the production call site, canonical fixture bytes, and regression coverage.
