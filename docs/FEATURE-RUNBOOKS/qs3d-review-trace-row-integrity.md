# QS3D Review trace worksheet row integrity

## Scope

This Core-only contract covers worksheet-row discovery in `Qs3dReviewWorkbookTraceReader` for the traceable Review Workbook sheets. Trace resolution needs exactly the canonical header row (`r=1`) and one requested data row, but every declared worksheet row remains structural input and must be validated.

## Integrity contract

The reader performs one direct pass over worksheet `row` elements. Each row must declare a canonical integer row number in the XLSX range 1..1,048,576. Missing, malformed, zero, negative or over-limit row metadata fails closed even when the bad row is unrelated to the requested trace row.

The pass retains at most the unique header row and requested target row. Duplicate or missing header/target rows remain errors. Whole-sheet row materialization and repeated `FindUniqueRow` list scans are prohibited.

## Deterministic qualification

`Qs3dReviewWorkbookTraceRowIntegritySmoke` proves stable header/target resolution, duplicate header/target rejection, and fail-closed behavior for unrelated malformed and out-of-range trailing rows. `preflight-qs3d-review-trace-row-integrity.py` pins the single-pass, all-row-validation source contract and rejects regression to `ToList()`/list rescans.

Runtime classification: NOT_APPLICABLE. This is deterministic Core XLSX parsing/data integrity; hosted CI must not be described as licensed BricsCAD `LOCAL_PASS`.
