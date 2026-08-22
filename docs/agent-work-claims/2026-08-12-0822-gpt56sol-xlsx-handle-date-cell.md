# Work claim — XLSX Handle reader date-cell semantics

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-date-cell-20260812-0822`
- Registered: `2026-08-12T08:22:00+07:00`
- Baseline main SHA: `1f0d8c2b165ca1f756fa13f484f7ee51c8489873`
- Priority: P2 evidence-driven remote-safe XLSX cell-semantics hardening

## Confirmed defect

`XlsxHandleReader.ReadCells(...)` preserves raw `<v>` text for a SpreadsheetML `t="d"` Date cell. A malformed/date-typed lexical value such as `1A` in an explicit `CAD Handle (hex)` column can therefore flow into `AddHexHandles(...)` and be accepted as CAD Handle `1A` even though the cell is semantically a Date cell.

## Reserved scope

Preserve Date-cell semantics by converting `t="d"` values to unmistakably non-handle diagnostic text before Handle/legacy heuristics. Do not redesign numeric/default Excel date serial handling, formula-string results or general date parsing. Preserve shared strings, inline strings, Boolean/error semantics and package/identity guards.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleDateCellSmoke.cs`
- this claim file

## Excluded scope

- No generalized cell-type validator or date parser.
- No changes to default/numeric cells or formula-string cells.
- No XLSX exporter or BLT/ED2 semantics changes beyond preventing typed Date cells from becoming Handles.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- `t="d"><v>1A</v>` in an explicit Handle column must not synthesize Handle `1A`.
- A Date-typed unrelated `$123` cell must not activate legacy decimal fallback.
- A normal default/numeric Handle cell remains compatible.
- Re-read source/test after SHA-guarded integration and preserve concurrent history.

## Evidence

Microsoft Learn/Open XML documentation defines `CellValues.Date` / `t="d"` as a Date cell type, distinct from ordinary numeric/string values.

## Coordination

Recent current-main search found no active XLSX Handle date-cell owner. The error-cell claim is completed. This claim is limited to typed Date semantic preservation.

## Completion condition

Completed only when typed Date cells cannot be synthesized into CAD Handles or legacy `$decimal` fallback, focused regression source is on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
