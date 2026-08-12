# Work claim — XLSX Handle reader Boolean cell semantics

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-boolean-cell-20260812-0805`
- Registered: `2026-08-12T08:05:00+07:00`
- Baseline main SHA: `ff0030d631379071b62d69ae2e238dcd5c5ce387`
- Priority: P2 evidence-driven remote-safe XLSX cell-semantics hardening

## Confirmed defect

`XlsxHandleReader.ReadCells(...)` currently leaves every non-inline/non-shared cell's raw `<v>` text unchanged. SpreadsheetML cells with `t="b"` are Boolean cells whose value `0`/`1` means FALSE/TRUE. In an explicitly discovered `CAD Handle (hex)` column, a Boolean TRUE cell therefore produces raw text `1`, which `AddHexHandles(...)` accepts as CAD Handle `1` instead of rejecting the non-handle cell type.

## Reserved scope

Honor Boolean cell semantics while reading values: require Boolean `<v>` to be exactly `0` or `1`, normalize those to `FALSE`/`TRUE`, and thereby prevent them from being interpreted as hexadecimal Handle tokens. Preserve shared strings, inline strings, numeric/default cells, formula string results and all package/identity guards.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleBooleanCellSmoke.cs`
- this claim file

## Excluded scope

- No generalized cell-type validator or date/formula policy redesign.
- No XLSX exporter changes or BLT/ED2 handle semantics changes beyond correct Boolean interpretation.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- `t="b"><v>1</v>` in an explicit Handle column must no longer synthesize Handle `1` and must fail the Handle token path.
- `t="b"><v>0</v>` likewise must not synthesize Handle `0`.
- Invalid Boolean values such as `2` must fail closed as malformed XLSX cell content.
- A normal numeric/default Handle cell remains compatible with existing lexical Handle parsing.
- Re-read source/test after SHA-guarded integration and preserve concurrent history.

## Evidence

Microsoft Learn documents that SpreadsheetML Boolean cells use `t="b"` and values `0`/`1`, corresponding to FALSE/TRUE; Office restricts Boolean cell values accordingly.

## Coordination

Recent search found no active XLSX Handle Boolean-cell owner. This claim is limited to Boolean value semantics and does not reopen prior package/precedence work.

## Completion condition

Completed only when Boolean cells cannot be synthesized into CAD Handles, malformed Boolean values fail closed, focused regression source is on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
