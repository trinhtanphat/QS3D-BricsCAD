# Work claim — Quantity XLSX standard row snapshot integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-xlsx-row-snapshot-20260812-1059`
- Registered: `2026-08-12T10:59:00+07:00`
- Baseline main SHA: `53b99cd5b89ef722bc7d51215801a4ee190a456c`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`XlsxQuantityExporter.Export(...)` validated the caller-owned standard `IReadOnlyList<QuantityReportRow>` before I/O, including mutable `ElementIds` / `SourceHandles`, but then passed the original rows to `ExportCore(...)`. After directory/temp-file creation, `BuildSheet(rows)` re-read row scalars and derived provenance text from the same mutable objects. The serialized worksheet could therefore differ from the values that passed preflight, or fail only after filesystem side effects began.

## Reserved scope

- standard `XlsxQuantityExporter.Export(...)` path in `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- `tests/QS3D.Core.SmokeTests/QuantityXlsxRowSnapshotSmoke.cs`
- this claim file for close-out

## Implemented contract

- Capture the existing standard worksheet row bound once.
- Read each caller-owned row index once before any path/directory/temp-file mutation.
- Copy every standard-sheet-emitted scalar plus `ElementIds` / `SourceHandles` provenance into detached `QuantityReportRow` values.
- Run existing standard text/numeric validation on the detached snapshot.
- Serialize only the detached standard snapshot.
- Preserve existing worksheet schema, XML sanitization, row/text/numeric limits, package validation and atomic replace semantics.

## Exclusions

- `ExportEd2(...)`, ED2 identity/parity rules, CHI_TIET/TONG_HOP semantics and ED2 worksheet schema were not changed in this claim.
- No Quantity builders/math/UI/commands/Health changes.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion evidence

- Claim registration: `9ea748b2fde921248287e0eeaae3e86aca1beb3b`.
- Source branch fix: `3909face52cd89177b96b8f4be722699f95c4ab8`.
- Focused smoke source: `d4a35c18744e743c4dc86f9792dce8d128aa16a9`.
- PR: `#794`.
- Squash integration on `main`: `33956d1cd4e8c4cc4a3243c838fa9cf55bb524ae`.
- Post-merge readback confirmed standard `Export(...)` uses `SnapshotStandardRows(rows)` and passes only the detached snapshot to `ExportCore`.
- Post-merge readback confirmed `QuantityXlsxRowSnapshotSmoke` rejects any second caller-row indexed read or caller-list enumeration.

## Validation boundary

Focused smoke coverage was added and read back from `main`, but it was not executed in this remote session. No GitHub Actions, local .NET build, BricsCAD V25/V26 runtime, release or signing PASS is claimed.