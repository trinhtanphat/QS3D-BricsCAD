# Coordination Unified XLSX bounded writer

Carrier: #6222 / PR #6223  
Lane: C02 Quantity / Estimating / Measurement / XLSX / Export

## Root cause

`CoordinationUnifiedWorkbookExporter` previously admitted Excel-scale CLASHES/DUPLICATES rows, materialized projected rows and entire worksheet XML in managed memory, then created the temporary XLSX ZIP before `XlsxPackageValidator` ran. Hostile but semantically valid cells could therefore amplify managed memory and temporary output before package validation failed closed.

## Production contract

- CLASHES and DUPLICATES are bounded to `MaxExportDataRowsPerSheet` before snapshot/export work.
- TRACE_MODEL is bounded to the checked sum of the two admitted source sheets.
- Every XML package entry is bounded by `MaxExportXmlEntryBytes` while bytes are written.
- Aggregate uncompressed XML is fail-closed by `MaxExportXmlTotalBytes` through `ReserveExportXmlBytes`.
- The physical XLSX archive is wrapped by `BoundedArchiveWriteStream` and cannot exceed `MaxExportWorkbookBytes`, including central-directory writes during `ZipArchive.Dispose`.
- Worksheet XML is streamed by `WriteSheet`; whole-sheet `StringBuilder` materialization is forbidden.
- ZIP entry names/order are fixed and timestamps are canonicalized to 1980-01-01 UTC.
- UTF-8 is emitted without BOM, XML cell values remain escaped, and invariant ordinal/provenance ordering is preserved.
- Package validation remains mandatory before `AtomicFileCommit.ReplaceWithoutBackup`; failures preserve an existing destination and cleanup the carrier-owned `.<guid>.tmp` file.

## Regression

`CoordinationUnifiedWorkbookSmoke.RejectsOversizedWorksheetBeforeArchiveCommit` builds 1,100 semantically valid duplicate rows whose comments are each at the Excel 32,767-character cell limit. The writer must reject the worksheet through its byte budget, preserve a pre-existing destination sentinel byte-for-byte, and leave no owned temp file.

## Validation / merge gates

1. Focused preflight `scripts/preflight-coordination-unified-xlsx-bounded-write.py` must pass.
2. Targeted Core smoke must compile and execute the hostile oversized-workbook regression plus normal clash/duplicate round-trip coverage.
3. Required exact-head preflight + Core CI must be terminal GREEN on a head containing latest protected `main`.
4. Reservation-v2/path collision and review threads must be clean.
5. Merge only with an expected-head guard; never bypass rulesets or relabel licensed runtime evidence.

Runtime classification: `REMOTE_SAFE` deterministic Core/export code. No licensed BricsCAD runtime PASS is required or claimed for this carrier.
