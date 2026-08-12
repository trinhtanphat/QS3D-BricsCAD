# Work claim — XLSX Handle reader optional row-index compatibility

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-optional-row-index-20260812-0745`
- Registered: `2026-08-12T07:45:00+07:00`
- Baseline main SHA: `da9b49ba52f735caaff1c56e4ea0bd204b75edc7`
- Priority: P1 corrective compatibility hardening from spec verification

## Confirmed compatibility defect

The preceding worksheet row-capacity hardening globally rejects every `<row>` whose `r` attribute is missing. ISO/IEC 29500 SpreadsheetML defines `CT_Row.r` as optional. A workbook can therefore contain an unrelated row without `r` while the explicitly addressed target/header rows used by this reader remain valid. Rejecting the entire worksheet in that case is broader than the row-capacity invariant and can reject conforming input.

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

## Validation plan

- A worksheet containing valid addressed header/target rows plus an unrelated `<row>` without `r` must remain readable.
- A present malformed or over-capacity row index must still be rejected.
- Existing API/cell-reference row-capacity regressions remain intact.
- Re-read source/test after SHA-guarded integration and preserve concurrent history.

## Evidence

Microsoft Learn reproduces the ISO/IEC 29500 `CT_Row` schema with `r` declared `use="optional"`.

## Completion condition

Completed only after optional unrelated row indices no longer cause global rejection, focused regression source is updated on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
