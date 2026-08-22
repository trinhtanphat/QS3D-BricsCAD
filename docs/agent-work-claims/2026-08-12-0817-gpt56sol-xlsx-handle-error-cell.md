# Work claim — XLSX Handle reader error-cell semantics

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-error-cell-20260812-0817`
- Registered: `2026-08-12T08:17:00+07:00`
- Baseline main SHA: `d5af18a12c77725d52430da4798051e65366091a`
- Priority: P2 evidence-driven remote-safe XLSX cell-semantics hardening

## Confirmed defect

`XlsxHandleReader.ReadCells(...)` currently leaves an XLSX cell with `t="e"` (SpreadsheetML error cell) as its raw `<v>` text. In an explicitly discovered `CAD Handle (hex)` column, a malformed/error-typed value whose lexical text looks hexadecimal, such as `1A`, is therefore accepted by `AddHexHandles(...)` as a CAD Handle even though the cell is semantically an Excel error value.

## Reserved scope

Preserve error-cell type semantics by converting `t="e"` values to unmistakably non-handle diagnostic text before downstream header/handle heuristics. Do not attempt to enumerate or validate every historical/new Excel error code. Preserve shared strings, inline strings, Boolean semantics, default/numeric cells, formula string results and package/identity guards.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleErrorCellSmoke.cs`
- this claim file

## Excluded scope

- No generalized cell-type validator and no date/formula/numeric policy redesign.
- No whitelist of Excel error tokens.
- No XLSX exporter changes or BLT/ED2 semantics changes beyond preventing error cells from becoming Handles.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- `t="e"><v>1A</v>` in an explicit Handle column must not synthesize Handle `1A` and must fail the explicit Handle token path.
- An error-typed unrelated cell containing `$123` must not activate legacy decimal fallback.
- A normal default/numeric Handle cell remains compatible.
- Re-read source/test after SHA-guarded integration and preserve concurrent history.

## Evidence

Microsoft Learn/Open XML documentation defines `CellValues.Error` / `t="e"` as an error cell type, distinct from numeric/string cell values.

## Coordination

Recent current-main search found no active XLSX Handle error-cell owner. The exact-header-precedence claim is completed. This claim is limited to error-cell semantic preservation.

## Completion condition

Completed only when error cells cannot be synthesized into CAD Handles or legacy `$decimal` fallback, focused regression source is on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
