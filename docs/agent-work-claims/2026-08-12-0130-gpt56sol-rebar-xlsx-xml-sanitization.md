# Work claim — rebar XLSX XML sanitization

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-xlsx-xml-sanitization-20260812-0130`
- Registered: `2026-08-12T01:30:00+07:00`
- Baseline main SHA: `45991a9b38e3968f047bcd83b38f7ba6625ed186`
- Priority: evidence-driven remote-safe XLSX integrity hardening during owner-requested `continue all`

## Reserved scope

Route all BBS/Rebar XLSX inline-string cell text through the repository's XML 1.0 sanitizer so invalid control characters and malformed surrogate sequences cannot produce invalid worksheet XML.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`

## Excluded scope

- Existing completed BBS worksheet row-limit behavior.
- XLSX cell-length limits, CSV export, rebar schedule/reporting business logic, or ownership guards.
- Active `XlsxQuantityExporter` structural-limit claim and all Quantity XLSX work.
- Shared `XlsxXmlText` implementation unless a regression proves it defective.
- BricsCAD V25/Windows/native runtime qualification and GitHub Actions.

## Validation plan

- Preserve ordinary BBS XLSX output and valid Unicode/supplementary characters.
- Cover XML 1.0-invalid control text (for example U+0001) being replaced rather than emitted raw.
- Cover malformed lone surrogate replacement.
- Parse the generated worksheet XML from the XLSX package to prove well-formed output.
- Re-read exact PR diff and moving `main` before integration; do not dispatch Actions.

## Completion condition

Exporter + focused regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
