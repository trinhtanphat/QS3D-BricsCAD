# Work claim — ED2 Quantity XLSX row snapshot integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-ed2-xlsx-row-snapshot-20260812-1103`
- Registered: `2026-08-12T11:03:00+07:00`
- Baseline main SHA: `a0c40b9b7b5503ba8abb39289e6a8505a95760a7`
- Priority: P1 export parity / filesystem atomicity

## Confirmed defect

`XlsxQuantityExporter.ExportEd2(...)` validated and cross-checked caller-owned `detailRows` / `summaryRows`, including mutable `ElementIds` and `SourceHandles`, then passed the same external row objects to `ExportCore(...)`. After directory/temp-file creation, `BuildEd2Sheet(...)` re-read those mutable rows and derived provenance text. A changing or hostile caller could therefore serialize CHI_TIET/TONG_HOP data different from the values that passed identity/numeric/provenance parity, or fail only after filesystem side effects began.

## Reserved scope

- ED2 path in `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- `tests/QS3D.Core.SmokeTests/Ed2QuantityXlsxRowSnapshotSmoke.cs`
- this claim file for close-out

## Implemented contract

- Capture the existing CHI_TIET and TONG_HOP worksheet row bounds once.
- Read each caller-owned detail/summary row index exactly once before ED2 validation, parity checks, path resolution, directory creation or temp-file creation.
- Deep-copy every ED2-relevant scalar plus `ElementIds` / `SourceHandles` into detached `QuantityReportRow` values.
- Run all existing ED2 text, numeric, drawing-fingerprint, element-ID, CAD-handle, identity, aggregate, density and mass parity checks only against detached snapshots.
- Serialize only those same validated snapshots.
- Preserve ED2 workbook/sheet schema, XML sanitization, row/text/numeric limits, package validation and atomic replacement semantics.

## Exclusions

- Standard `XlsxQuantityExporter.Export(...)` remains the completed PR #794 behavior and was not semantically changed in this claim.
- No Quantity builders/math/UI/commands/Health changes.
- No ED2 semantic/parity rules were changed beyond stabilizing their input.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion evidence

- Claim registration: `b6e9aa70bf433e8bfd560ff99ee18539d8250dae`.
- Source branch fix: `6259e9f723305a3f636966b1df6a86e0511be8d9`.
- Focused smoke source: `9bf407ac75dc9afa027b93450e95e0bf782af539`.
- PR: `#799`.
- Squash integration on `main`: `15583036eb19dcb1a24a9d3c4b1288bc35456d88`.
- Post-merge readback confirmed `ExportEd2(...)` creates `detailSnapshot` / `summarySnapshot`, performs all subsequent scope/parity checks on those snapshots, and passes the same snapshots to `ExportCore`.
- Post-merge readback confirmed `Ed2QuantityXlsxRowSnapshotSmoke` uses valid matching CHI_TIET/TONG_HOP data and rejects caller-list enumeration or any second indexed row read.

## Validation boundary

Focused smoke coverage was added and read back from `main`, but it was not executed in this remote session. No GitHub Actions, local .NET build, BricsCAD V25/V26 runtime, release or signing PASS is claimed.