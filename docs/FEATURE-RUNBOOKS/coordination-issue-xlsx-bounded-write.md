# Coordination Issue XLSX bounded writer

Carrier: #6220 / PR #6221
Ownership-Key: `c02.coordination-issue-xlsx-bounded-write-v1`
Runtime classification: `REMOTE_SAFE` deterministic Core/export.

## Root cause

The import path already rejected workbooks above 128 MiB and XML parts above 64 MiB, but the export path built each worksheet as a whole unbounded `StringBuilder` and materialized an unbounded temporary ZIP before `XlsxPackageValidator` ran. High-cardinality, individually legal cell values could therefore consume excessive memory or temporary-file capacity before the package was rejected.

## Writer contract

- Excel row and 32,767-character cell admission remains fail-closed.
- Each exported XML part is bounded to 32 MiB uncompressed.
- Aggregate exported XML is bounded to 64 MiB uncompressed.
- The final ZIP stream is bounded to 64 MiB before post-write validation.
- Worksheets are serialized incrementally through a bounded entry stream; no whole-sheet string is constructed.
- ZIP entry order is fixed and timestamps are canonicalized to 2000-01-01T00:00:00Z.
- `XlsxPackageValidator` still validates the completed temporary package before `AtomicFileCommit.ReplaceWithoutBackup` can mutate the destination.
- Failure cleans only the owned temporary file; it must not replace an existing destination.

## Validation

The focused preflight `scripts/preflight-coordination-issue-xlsx-bounded-write.py` must fail against the pre-fix writer and pass only when entry, aggregate and final-archive bounds plus streamed sheet writing are present. Required Shared CI must be run on the exact PR head after production changes and again after any reconciliation with protected `main`.

The import provenance contract remains unchanged: exact META schema/project/drawing/revision identity, immutable issue trace columns, canonical numeric/UTC serialization and duplicate/missing IssueId rejection remain fail-closed.

## Remaining sibling audit

This carrier does not claim all XLSX/BCF/IFC exporters are covered. `CoordinationWorkbook`, `CoordinationUnifiedWorkbook`, BCF ZIP, IFC round-trip, CSV/BBS and other workbook/package surfaces remain in C02 audit scope and require independent reproduction before mutation.
