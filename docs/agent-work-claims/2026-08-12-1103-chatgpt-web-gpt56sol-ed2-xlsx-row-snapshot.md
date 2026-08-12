# Work claim — ED2 Quantity XLSX row snapshot integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ed2-xlsx-row-snapshot-20260812-1103`
- Registered: `2026-08-12T11:03:00+07:00`
- Baseline main SHA: `a0c40b9b7b5503ba8abb39289e6a8505a95760a7`
- Priority: P1 export parity / filesystem atomicity

## Confirmed defect

`XlsxQuantityExporter.ExportEd2(...)` validates and cross-checks caller-owned `detailRows` / `summaryRows`, including mutable `ElementIds` and `SourceHandles`, then passes the same external row objects to `ExportCore(...)`. After directory/temp-file creation, `BuildEd2Sheet(...)` re-reads those mutable rows and derived provenance text. A changing or hostile caller can therefore serialize CHI_TIET/TONG_HOP data different from the values that passed identity/numeric/provenance parity, or fail only after filesystem side effects have begun.

## Reserved scope

- ED2 path in `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- a new focused smoke file for ED2 row-snapshot integrity
- this claim file for close-out

## Contract

- Capture the existing CHI_TIET and TONG_HOP worksheet row bounds once.
- Read each caller-owned detail/summary row index exactly once before ED2 validation, parity checks, path resolution, directory creation or temp-file creation.
- Deep-copy every ED2-relevant scalar plus `ElementIds` / `SourceHandles` into detached `QuantityReportRow` values.
- Run all existing ED2 text, numeric, drawing-fingerprint, element-ID, CAD-handle, identity, aggregate, density and mass parity checks only against detached snapshots.
- Serialize only those same validated snapshots.
- Preserve ED2 workbook/sheet schema, XML sanitization, row/text/numeric limits, package validation and atomic replacement semantics.

## Exclusions

- Standard `XlsxQuantityExporter.Export(...)` is already completed and must not be semantically changed in this claim.
- No Quantity builders/math/UI/commands/Health changes.
- No changes to ED2 semantic/parity rules beyond making their input stable.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Validation plan

Add a focused smoke with valid one-row CHI_TIET and TONG_HOP scopes backed by hostile `IReadOnlyList<QuantityReportRow>` implementations that allow one indexed read and reject enumeration/second reads. `ExportEd2(...)` must succeed from the detached snapshots and leave each external row list at exactly one indexed read.

## Coordination

The standard Quantity XLSX snapshot lane is completed in PR #794 / `33956d1cd4e8c4cc4a3243c838fa9cf55bb524ae`. No open PR existed at ED2 claim registration time, and no separate ED2 snapshot claim was found.

## Completion condition

ED2 source fix and focused smoke source are integrated on current `main`, read back after merge, and this claim is marked `COMPLETED` with exact SHA/PR evidence and remote validation boundaries.