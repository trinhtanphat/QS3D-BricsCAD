# QS3D Review QTO detached snapshot

## Ownership

- Lane: C02 Quantity / Export / XLSX / provenance
- Issue: #6409
- Ownership-Key: `export.qs3d-review-qto-detached-snapshot-v1`
- Runtime class: REMOTE_SAFE managed Core Export/XLSX

## Defect

`Qs3dReviewWorkbookExporter` formerly copied the outer row collection but retained caller-owned `QuantityReportRow` instances and their mutable `ElementIds` / `SourceHandles` lists. XLSX worksheet delegates run later, so caller mutation after admission could change semantic identity, CAD provenance, evidence, or numeric values in the published workbook.

## Required contract

- Capture a detached `QuantityReportRow` before semantic validation/publication.
- Copy every scalar/evidence field used by review workbook projection.
- Copy `ElementIds` and `SourceHandles` into owned lists with Count stability checks.
- Re-read the source row after capture and fail closed if scalar/list state drifted during capture.
- Validate fingerprint, semantic IDs, handles, evidence, detail/summary scope and package bounds against detached rows only.
- Never retry or reinterpret caller mutation as a new generation.
