# 2D Takeoff PNG ingestion structure

## Purpose

CostX/Autodesk-style 2D takeoff depends on the drawing source being identified before calibration, markup and quantity evidence are trusted. PNG ingestion therefore fails closed when the byte stream only mimics a PNG signature without the required first `IHDR` chunk.

## Admission contract

`Qs2DSheetIngestor.IngestRaster` keeps the existing PNG/JPEG API. For PNG payloads it now requires:

- the canonical eight-byte PNG signature;
- enough bytes for the complete fixed-size first `IHDR` chunk;
- first-chunk length exactly 13 bytes;
- first-chunk type exactly `IHDR`;
- positive width and height from the IHDR data;
- the existing terminal `IEND` structural check.

A signature-prefixed payload with another chunk type, a non-13 IHDR length, or a truncated IHDR is rejected before a `DrawingSheet2D` can enter calibration or quantity extraction.

## Compatibility

Valid PNG drawing sheets and existing JPEG/PDF ingestion behavior are unchanged. No new public API is introduced, and existing source references, SHA-256 provenance, scale calibration, markup, zone/layer and downstream package workflows remain compatible.

This is an admission-hardening change only; it does not decode or rasterize image pixels and does not replace a rendering library.

## Validation

`Qs2DSheetIngestionPngStructureSmoke` is module-initialized and covers a canonical IHDR plus wrong chunk type, wrong IHDR length and truncated IHDR refusal. `scripts/preflight-2d-takeoff-png-structure.py` locks the production and smoke contract into aggregate source validation.
