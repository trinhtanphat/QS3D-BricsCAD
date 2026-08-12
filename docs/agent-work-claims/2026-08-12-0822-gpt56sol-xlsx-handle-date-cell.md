# Work claim — XLSX Handle reader date-cell semantics

- Status: `RELEASED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-date-cell-20260812-0822`
- Registered: `2026-08-12T08:22:00+07:00`
- Released: `2026-08-12T08:31:00+07:00`
- Baseline main SHA: `1f0d8c2b165ca1f756fa13f484f7ee51c8489873`
- Claim commit: `0662bb4f25e76d70be80cecc0ea4c781c9bf0af5`
- Regression commit already on main: `bc72d58f6787d6f59d226ed9c986fe1504a6d5d0`
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

The owner subsequently requested `continue all` and explicitly asked to commit/push every remaining unfinished change to `main`. At takeover time this reservation and its focused regression commit were already on `main`, but no Date-cell source fix or completion commit existed and no open PR/Date-cell branch was present. The owner-coordinated successor is `docs/agent-work-claims/2026-08-12-0831-chatgpt-web-gpt56sol-xlsx-handle-date-cell-takeover.md`. This original reservation is explicitly released rather than silently treated as abandoned.

## Completion condition

Released to the owner-coordinated successor. No source implementation is claimed by this original session beyond the already-pushed regression source listed above.
