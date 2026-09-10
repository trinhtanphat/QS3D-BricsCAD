# Room Finish XLSX bounded publication

## Scope and failure model

`RoomFinishXlsxExporter` accepts deterministic, validated schedule rows but must not retain an entire hostile-but-Excel-valid worksheet as one managed string. Publication is bounded before destination replacement so a large multibyte worksheet cannot turn an otherwise valid export request into unbounded memory or archive growth.

The exporter preserves the existing 15-column `A:O` schema, known-count admission/revalidation, row snapshot stability, invariant `R` numeric formatting, XML escaping, provenance fields, package validation, and atomic replacement. This change only hardens the publication boundary.

## Output budgets

- Each XML entry, including `xl/worksheets/sheet1.xml`, is capped at 32 MiB uncompressed.
- Aggregate uncompressed XML across the package is capped at 64 MiB with checked arithmetic.
- The final ZIP stream is capped at 64 MiB while writing and checked again before validation/replace.
- UTF-8 is strict (`throwOnInvalidBytes=true`); malformed surrogate input must not be silently replaced during publication.
- ZIP entry timestamps are fixed to the DOS epoch to keep package metadata deterministic.

## Hostile regression

`RoomFinishXlsxBoundedWriteSmoke` uses repeated maximum-length XML-expanding cell text to exceed the worksheet byte ceiling without violating the 32,767-character Excel cell limit. The expected result is `InvalidDataException` before `AtomicFileCommit.ReplaceWithoutBackup`, preservation of an existing workbook sentinel, and cleanup of owned temporary files.

## Validation

Run the focused bounded-write preflight first, then Room Finish Count/snapshot/generation guards and the Core smoke suite. Cross-check representative Quantity precision, Estimating/Commercial totals, IFC/BCF package handling, CSV/BBS output/provenance, repository health, and `git diff --check`. Hosted integration authority remains fresh exact-head Shared CI; local deterministic evidence does not substitute for required protected gates.

Runtime classification: REMOTE_SAFE managed Core/export. No licensed BricsCAD runtime is required or claimed for this exporter hardening.
