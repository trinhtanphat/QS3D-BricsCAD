# P0 CostX / Autodesk-style 2D sheet ingestion

Issue: #6539  
Ownership-Key: `benchmark.takeoff.sheet-ingestion-v1`

## Purpose

`Qs2DSheetIngestor` closes the source-integrity gap before the existing calibrated 2D takeoff engine. It admits actual PDF or raster payload bytes, validates their basic format boundary, computes a deterministic SHA-256 content fingerprint, and returns the canonical `DrawingSheet2D` used by `CalibratedTakeoffEngine2D`.

The quantity authority is unchanged:

`PDF/image payload -> admitted DrawingSheet2D -> calibration -> count/length/area markup -> classification + zone + layer evidence -> package -> Drawing/BIM aggregation -> formula -> inventory -> estimate`.

## PDF admission

`IngestPdf(...)` requires a non-empty `%PDF-` payload and a 1-based page number. It records byte length and SHA-256 source identity. Page rendering and PDF object interpretation deliberately remain an outer-host responsibility; Core does not claim raster fidelity from a header check.

## Raster admission

`IngestRaster(...)` accepts PNG and JPEG payloads. PNG dimensions are read from the IHDR header. JPEG dimensions are read from a supported Start Of Frame marker. Malformed segment lengths, missing frame dimensions, zero dimensions, empty payloads and unknown image signatures fail closed.

## Evidence and revision compatibility

The admitted object exposes the existing `DrawingSheet2D` contract, so current calibration, markup, source reference, source handle, zone/layer, revision compare, package readiness and estimate workflows continue without a parallel quantity engine. The source SHA-256 provides a stable external evidence key for adapters or persistence layers that need to prove which binary sheet payload was admitted.

Changing the payload changes the fingerprint even when logical sheet id, filename and revision text are unchanged. This lets outer package/document stores detect silent source replacement before reusing prior markup evidence.

## Host boundary

Core performs deterministic format admission and source identity only. A desktop/web host may use a PDF renderer or image decoder to display pixels and obtain interactive markup coordinates. Such a host must preserve the admitted payload fingerprint and canonical sheet identity when handing measurements to `CalibratedTakeoffEngine2D`.

Hosted CI validates deterministic parsing/admission and quantity integration; it is not evidence of PDF visual rendering fidelity, OCR accuracy, GPU behavior or proprietary renderer compatibility.
