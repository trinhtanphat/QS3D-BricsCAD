# Commercial QS workbook bounded-write runbook

Issue: #6213  
Lane: C02 Quantity / Estimating / XLSX / Commercial export  
Runtime: REMOTE_SAFE deterministic Core/export; no licensed BricsCAD claim.

## Problem

`CommercialQsWorkbook.Export` already wrote to an owned temporary path and validated the generated XLSX before atomic destination replacement, but the writer previously fed an ordinary `FileStream` directly to `ZipArchive`. `XlsxPackageValidator` limits required XML entries to 32 MiB only after the complete temporary package exists. The commercial model admits up to 10,000 variations and 10,000 tender bids, and canonical text may approach the Excel 32,767-character cell limit, so semantically admitted input could create excessive temporary output before post-write validation.

## Writer contract

The exporter now enforces four independent bounds while preserving existing commercial/revision semantics:

- worksheet data rows: at most 10,002 rows, enough for the admitted 10,000 tender bid results plus package and award rows;
- one XML entry: at most 32 MiB, aligned with `XlsxPackageValidator`;
- total uncompressed XML across the fixed workbook package: at most 64 MiB;
- final ZIP bytes: at most 64 MiB through the stream consumed by `ZipArchive`, followed by a completed-file length check.

Worksheet XML is streamed into a per-entry bounded buffer rather than assembled in one unbounded `StringBuilder`. Each completed entry reserves its byte count from the shared uncompressed budget before it is copied into the ZIP. The ZIP writes through `BoundedArchiveWriteStream`, so central-directory/output growth is also fail-closed.

## Invariants that must remain unchanged

- Fixed entry names, worksheet order and DOS epoch timestamps remain deterministic.
- Cells remain inline strings with XML escaping; commercial decimals remain invariant-culture Core projections rather than spreadsheet formulas.
- Tender package/evaluation/award revision and currency provenance validation remains in `CommercialQsWorkbookSnapshot`.
- `XlsxPackageValidator.Validate(...)` still parses every required XML entry with DTD processing prohibited before publication.
- `AtomicFileCommit.ReplaceWithoutBackup(...)` remains the only destination publication point; every failure path deletes only the owned temporary package.

## Regression

`CommercialQsWorkbookSmoke.ExportIsByteStableAndCarriesAllCommercialSections` continues to require byte-identical output for identical validated inputs and verifies variation, IPC, final-account, tender/award and CVR provenance.

`OversizedWorksheetFailsWithoutPublishingDestination` uses admitted rows whose cells are individually within Excel's text limit but whose worksheet XML exceeds the writer entry budget. Export must throw `InvalidDataException`, leave a pre-existing destination byte-for-byte unchanged and leave no owned temporary package behind.

## Qualification

Before merge:

1. run the focused auto-discovered preflight `scripts/preflight-commercial-qs-workbook-bounded-write.py`;
2. run the complete Core smoke suite, including the hostile-output regression;
3. run aggregate protected preflight and Core on the exact PR head;
4. reconcile latest protected `main` non-force if it advanced and repeat exact-head CI;
5. require clean Reservation-v2/path ownership and zero unresolved review blockers;
6. merge only through the repository's guarded PR path and verify the exact merge/main SHA.

Do not weaken `XlsxPackageValidator`, raise output budgets merely to make a hostile regression pass, or replace the bounded writer with a post-write-only file-size check.
