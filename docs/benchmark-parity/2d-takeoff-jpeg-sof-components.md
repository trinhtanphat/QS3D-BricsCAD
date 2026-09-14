# 2D Takeoff JPEG SOF component admission

## Purpose

Raster drawing evidence must not publish dimensions from a malformed JPEG frame. The SOF admission boundary therefore validates each component descriptor after the existing frame precision/count/length checks and before width or height are returned to `IngestRaster`.

## Admission rules

Each declared SOF component must have a non-zero identifier that is unique within the frame. Horizontal and vertical sampling factors must each be in the JPEG frame range 1..4. The quantization-table selector must be in 0..3. A violation fails closed with `InvalidOperationException`; no `IngestedDrawingSheet2D`, source hash, pixel dimensions, calibration result, markup quantity, or downstream evidence is published.

The prior JPEG structure fixture used placeholder bytes for its component descriptor. It is normalized in this change to a canonical `id=1, sampling=0x11, table=0` descriptor so the earlier frame-structure regression remains valid under the stronger boundary.

## Compatibility

No public API changes are introduced. Valid PDF, PNG, and JPEG ingestion continues through the existing `Qs2DSheetIngestor`. Existing scale calibration, length/area/count markup, zones/layers, revision compare, package UX, classification, formula, inventory, snapshot, and estimate contracts are unchanged.

The evidence chain remains:

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

This change only strengthens the drawing-ingestion trust boundary at the start of that chain. Deterministic smoke coverage includes a canonical three-component frame and rejection of zero/duplicate identifiers, zero or out-of-range sampling factors, and an out-of-range quantization-table selector.
