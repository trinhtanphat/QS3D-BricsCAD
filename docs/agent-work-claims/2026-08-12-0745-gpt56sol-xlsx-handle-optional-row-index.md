# Work claim — XLSX Handle reader optional row-index compatibility

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-optional-row-index-20260812-0745`
- Registered: `2026-08-12T07:45:00+07:00`
- Baseline main SHA: `da9b49ba52f735caaff1c56e4ea0bd204b75edc7`
- Priority: P1 corrective compatibility hardening from spec verification

## Confirmed compatibility defect

The preceding worksheet row-capacity hardening globally rejected every `<row>` whose `r` attribute was missing. ISO/IEC 29500 SpreadsheetML defines `CT_Row.r` as optional, so rejecting an unrelated row without `r` was broader than the intended capacity invariant.

## Reserved scope

Refine only the global worksheet row-capacity validation so a missing `r` attribute is ignored there, while a present row index that is malformed, zero or above 1,048,576 still fails closed. Preserve requested-row bounds, processed-row validation, cell-reference bounds, shared-string hardening and workbook metadata guards.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleWorksheetRowCapacitySmoke.cs`
- this claim file

## Excluded scope

- No inference of target/header positions from rows that omit `r`.
- No optional-cell-reference redesign.
- No XLSX exporter, BLT/ED2 semantics, UI/native/runtime or GitHub Actions work.

## Validation implemented

- The global scan now skips only rows whose `r` attribute is absent.
- A present malformed, zero or over-capacity row index remains rejected by the row-capacity path.
- Rows actually processed by `ReadCells(...)` still require a valid explicit row number, so no positional inference was introduced.
- Focused smoke adds an unrelated row without `r` beside valid addressed header/target rows and requires the target handle to remain readable.
- Existing API, over-capacity worksheet-row, cell-reference and final-valid-row regressions remain intact.
- Source diff was re-read and is limited to the global row scan compatibility correction; the smoke commit remains an ancestor of current `main` with no overlap in subsequent commits.

## Integration commits

- Claim: `53f3b93452265eef4add40e633f2a74cd790a7f3`
- Source correction: `89bfa236585ed2eb3aa6312c02bedb6931a96ecd`
- Focused smoke update: `0696f3cbcf602e140c3cad23282160641f2e659d`

## Evidence

Microsoft Learn reproduces the ISO/IEC 29500 `CT_Row` schema with `r` declared `use="optional"`.

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Completion condition

Completed: optional unrelated row indices no longer cause global rejection, present invalid/over-capacity row indices remain fail-closed, focused regression source is on current `main`, and exact integration SHAs are recorded above.
