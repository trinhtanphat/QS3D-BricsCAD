# QS workbook template trace package admission

## Scope

This runbook covers the deterministic Core safety contract for `QsWorkbookTemplateTraceReader.Read`. It is not a BricsCAD runtime feature and requires no licensed-host qualification.

## Problem

Template export already rejects workbook packages larger than the canonical 128 MiB template-workbook ceiling before ZIP/XML processing. The read-only trace path historically opened the caller-supplied workbook with `ZipArchive` before applying that package-level admission, so unrelated ZIP payload or central-directory content could impose package-level work outside the existing bounded XML readers.

## Contract

- `QsWorkbookTemplateExporter.MaxTemplateWorkbookBytes` is the single canonical package byte ceiling for both export and trace-read paths.
- `QsWorkbookTemplateExporter.ValidateTemplatePackageLength(...)` owns the shared fail-closed package-length check.
- Trace reading resolves the full path, requires the workbook file to exist, reads `FileInfo.Length`, and applies the shared package-length admission before constructing `ZipArchive`.
- Negative or greater-than-128-MiB package lengths are rejected with `InvalidDataException`.
- Missing trace workbooks fail as `FileNotFoundException` before ZIP processing.
- Existing metadata/XML bounds and secure XML-reader settings remain unchanged: 4 MiB for metadata XML and 64 MiB for worksheet/shared-string XML, DTD prohibited, resolver disabled, entity expansion disabled.
- Canonical in-bound workbooks continue through the existing worksheet/shared-string/trace-key validation path.

## Deterministic regression

`QsWorkbookTemplateTracePackageBoundSmoke` proves an oversized workbook is rejected by the shared package admission before ZIP parsing. The focused source guard `scripts/preflight-qs-workbook-template-trace-package-bound.py` pins the shared ceiling/helper and call ordering so the trace reader cannot regress to opening the package first.

## Validation

Run the focused preflight, Core smoke suite and Core Release build through the repository Shared CI. Merge only after exact-head protected `preflight` and `core` are both successful, current-main freshness/collision checks remain clean, and the PR is mergeable.

Runtime classification: `NOT_APPLICABLE`.