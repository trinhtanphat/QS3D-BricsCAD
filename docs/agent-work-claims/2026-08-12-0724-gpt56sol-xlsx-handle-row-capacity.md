# Work claim — XLSX Handle reader worksheet row capacity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-row-capacity-20260812-0724`
- Registered: `2026-08-12T07:24:00+07:00`
- Baseline main SHA: `eb73f30cdf79140db64d25075a07e07d4c96b828`
- Priority: P2 evidence-driven remote-safe XLSX input-integrity hardening

## Confirmed defect

`XlsxHandleReader` enforces the XLSX 16,384-column capacity but has no corresponding 1,048,576-row capacity. `ReadHandleLookup(...)` accepts requested row numbers above Excel's worksheet limit, worksheet `<row r>` declarations are not globally rejected above that limit, and `ColumnIndex(...)` accepts cell references such as `A1048577` when they match the containing row.

## Reserved scope

Enforce Excel's 1,048,576-row capacity for requested row numbers, worksheet row declarations and parsed cell references. Preserve current column bounds, worksheet selection, shared-string validation, handle/identity parsing and XML/size guards.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleWorksheetRowCapacitySmoke.cs`
- this claim file

## Excluded scope

- No XLSX exporter changes.
- No column-limit changes.
- No BLT/ED2 handle parsing policy changes.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- Reject API request row 1,048,577 before opening/reading workbook content.
- Reject worksheet row declaration `r="1048577"` even when another requested row is valid.
- Reject a cell reference above the row capacity.
- Preserve a valid row at exactly 1,048,576.
- Re-read current source/test after SHA-guarded integration and preserve concurrent history.

## Coordination

The immediately preceding shared-string-index claim is completed and this claim does not reopen that behavior. Recent search found no independent `XlsxHandleReader` row-capacity owner.

## Completion condition

Completed only when row capacity is enforced at API, worksheet-row and cell-reference boundaries, focused regression source is present on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
