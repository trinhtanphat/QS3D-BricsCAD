# Work claim — Quantity XLSX standard numeric preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-xlsx-standard-numeric-preflight-20260812-0730`
- Registered: `2026-08-12T07:30:00+07:00`
- Baseline main SHA: `61f3a4aa959cfcda68d2698aa3a4c71d12645417`
- Priority: evidence-driven remote-safe export atomicity hardening during owner-requested `continue all`

## Reserved scope

Fail closed on `NaN`/`Infinity` in numeric cells emitted by the standard Quantity XLSX sheet before `ExportCore()` resolves paths or mutates the filesystem.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- ED2 numeric parity semantics, which already validate their emitted floating-point values before `ExportCore()`.
- Completed Quantity XLSX row/text structural limits and XML text sanitization.
- Reporting builders/grouping, sign/domain rules beyond the existing finite-number serialization contract, other XLSX exporters, shared package validation, native BricsCAD/runtime/UI, release work or GitHub Actions.

## Confirmed defect

`Export()` currently preflights row count, null rows and text cells, then calls `ExportCore()`. Standard-sheet floating-point values are only rejected by `AppendNumberCell()` while `BuildSheet()` is executing after destination directory/temp-package creation. Since `QuantityReportRow` is public/settable, a caller can provide non-finite numeric values directly and cause filesystem mutation before export rejection.

## Validation plan

- Preflight every floating-point field emitted by standard `BuildSheet()` before `ExportCore()`.
- Reject with `ArgumentOutOfRangeException` identifying `rows`, worksheet row and field.
- Preserve current ED2 behavior and existing serializer finite guard as defense in depth.
- Smoke must demonstrate non-finite rejection before creation of a new destination directory and successful ordinary finite standard export.
- Re-read current `main`/exact PR diff before integration; do not dispatch Actions.

## Coordination

A concurrent QSDB audit-action claim landed immediately before this claim and was verified non-overlapping; the baseline above records that actual parent.

## Completion condition

Source and focused regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
