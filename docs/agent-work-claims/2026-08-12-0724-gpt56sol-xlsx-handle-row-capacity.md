# Work claim — XLSX Handle reader worksheet row capacity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-row-capacity-20260812-0724`
- Registered: `2026-08-12T07:24:00+07:00`
- Baseline main SHA: `eb73f30cdf79140db64d25075a07e07d4c96b828`
- Priority: P2 evidence-driven remote-safe XLSX input-integrity hardening

## Confirmed defect

`XlsxHandleReader` enforced the XLSX 16,384-column capacity but had no corresponding 1,048,576-row capacity. `ReadHandleLookup(...)` accepted requested row numbers above Excel's worksheet limit, worksheet `<row r>` declarations were not globally rejected above that limit, and `ColumnIndex(...)` accepted cell references such as `A1048577` when they matched the containing row.

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

## Validation implemented

- API row 1,048,577 is rejected before file lookup.
- Worksheet row declarations are validated globally and reject values above 1,048,576, missing values and invalid row numbers.
- Cell-reference row tokens above 1,048,576 are rejected.
- Focused smoke preserves acceptance of the final valid row 1,048,576.
- Source commit readback confirms the implementation diff is limited to row-capacity enforcement.
- Regression commit remains an ancestor of current `main`; subsequent comparison showed no overlap with the reader/smoke files.

## Integration commits

- Claim: `23c58043ca5b32c1edb4dcf9149f2bb2229369be`
- Source fix: `ce1003795ade54fae937a6145ef5ca0bb220c991`
- Focused smoke: `73639b94cc01ed1a3e1f5dec8865bd73d96fec88`

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

The immediately preceding shared-string-index claim is completed and this claim did not reopen that behavior. No independent `XlsxHandleReader` row-capacity owner was found before registration.

## Completion condition

Completed: row capacity is enforced at API, worksheet-row and cell-reference boundaries, focused regression source is present on current `main`, and exact integration SHAs are recorded above.
