# Work claim — rebar XLSX cell text limit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-xlsx-cell-text-limit-20260812-0141`
- Registered: `2026-08-12T01:41:00+07:00`
- Baseline main SHA: `a6f5e86730d509453633f99c6dc5476bec0730df`
- Priority: evidence-driven remote-safe XLSX compatibility hardening during owner-requested `continue all`

## Reserved scope

Fail closed before filesystem mutation when any BBS/Rebar XLSX inline-string data cell exceeds Excel's 32,767-character cell-content limit.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`

## Excluded scope

- Completed Rebar XLSX XML sanitization, null-row preflight, and worksheet row-limit behavior.
- CSV export, rebar schedule/reporting business logic, ownership, Quantity XLSX, or native BricsCAD surfaces.
- Shared `XlsxXmlText` implementation.
- GitHub Actions and local/V25 qualification.

## Validation plan

- Validate all emitted BBS text fields during existing row preflight.
- Accept exactly 32,767 characters and reject 32,768 characters.
- Rejection identifies the worksheet row and field before destination directory/file/temp-package mutation.
- Preserve ordinary export, XML sanitization, numeric serialization, and row limits.
- Re-read exact PR diff and moving `main` before integration; do not dispatch Actions.

## Completion condition

Exporter + focused regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
