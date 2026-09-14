# 2D Takeoff JPEG SOF frame admission

Issue: #6990  
Lane: P0 CostX/Autodesk-style 2D Takeoff

## Purpose

JPEG raster sheet ingestion already requires an SOI marker, supported start-of-frame marker, positive dimensions, and terminal EOI marker. The frame metadata itself must also be structurally valid before pixel dimensions become drawing evidence.

`Qs2DSheetIngestor` now validates the supported SOF segment before width/height publication: the frame must include the precision/component-count fields, sample precision must be non-zero, at least one component must be declared, and the SOF segment length must equal `8 + 3 * componentCount` as defined by the JPEG frame-header structure.

## Compatibility

The public PDF/JPEG/PNG ingestion APIs and `IngestedDrawingSheet2D` contract are unchanged. Canonical JPEG drawings are accepted as before. Synthetic or malformed JPEG fixtures whose SOF length does not match their component descriptors now fail closed.

Scale calibration, length/area/count markup, zones/layers, revision overlay/compare, quantity extraction, package UX, classification, formulas, inventory, snapshots and estimates are otherwise unchanged.

## Quantity/evidence safety

Raster pixel dimensions feed drawing calibration and become part of the provenance used by downstream takeoff evidence. Rejecting malformed SOF metadata prevents forged or structurally inconsistent width/height values from entering Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate workflows.

## Validation

`Qs2DSheetIngestionJpegStructureSmoke` proves a canonical one-component frame remains accepted and that zero precision, zero components and SOF length/component mismatch fail closed. `scripts/preflight-2d-takeoff-jpeg-sof-structure.py` guards the production validation order and deterministic smoke coverage.
