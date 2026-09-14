# 2D Takeoff JPEG marker-stream validation

This P0 CostX/Autodesk-style 2D Takeoff hardening makes JPEG sheet ingestion validate marker semantics before frame dimensions can become quantity evidence.

## Contract

Before a supported SOF marker, the parser accepts ordinary length-delimited JPEG metadata segments and the standalone TEM marker. It rejects byte stuffing (`FF 00`), restart markers (`FFD0`-`FFD7`), nested SOI, premature EOI/SOS, and raw bytes outside a declared segment. SOF structure/component validation and terminal EOI validation remain unchanged.

This is intentionally fail-closed: malformed marker streams cannot publish pixel width/height, source hash, drawing evidence, calibration inputs, or downstream takeoff quantities.

## Compatibility

Public PDF/PNG/JPEG ingestion APIs do not change. Standards-compatible metadata segments remain supported, and legal TEM-before-SOF input is now handled explicitly instead of being misread as a length-delimited segment. Entropy-coded scan parsing remains outside this lightweight dimension/evidence boundary; the ingestor only needs a structurally valid pre-SOF marker stream plus the existing terminal EOI guarantee.

## Workflow impact

The hardened source evidence continues through the existing workflow without schema changes: **Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate**. Scale calibration, length/area/count markup, zones/layers, revision compare, snapshots and estimate integration keep their existing contracts.

## Validation

Run `python scripts/preflight-2d-takeoff-jpeg-marker-stream.py` and the `QS3D.Core.SmokeTests` project, then require repository Shared/Hybrid exact-head CI before merge.
