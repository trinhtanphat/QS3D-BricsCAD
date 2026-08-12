# Work claim — XLSX Handle reader partial workbook metadata guard

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-partial-metadata-20260812-0728`
- Registered: `2026-08-12T07:28:00+07:00`
- Baseline main SHA: `7e0e4403173f6aef378732b5d45c350c19b81496`
- Priority: P2 evidence-driven remote-safe XLSX input-integrity hardening

## Confirmed defect

`XlsxHandleReader.ResolveWorksheet(...)` uses the strict workbook/relationship graph only when both `xl/workbook.xml` and `xl/_rels/workbook.xml.rels` exist. If exactly one exists, it silently falls back to `sheet1.xml`/the first worksheet entry. A partially corrupted or tampered XLSX package can therefore bypass its declared workbook metadata instead of failing closed.

## Reserved scope

Permit the legacy worksheet fallback only when both workbook metadata parts are absent. If exactly one of `workbook.xml` or `workbook.xml.rels` exists, reject the workbook as incomplete. Preserve strict resolution when both are present and preserve the existing metadata-free fallback for legacy/minimal workbooks.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleWorkbookMetadataSmoke.cs`
- this claim file

## Excluded scope

- No XLSX exporter changes.
- No relationship target normalization redesign.
- No BLT/ED2 handle parsing policy changes.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- Workbook XML without workbook relationships must fail closed even when `sheet1.xml` exists.
- Workbook relationships without workbook XML must fail closed even when `sheet1.xml` exists.
- A metadata-free legacy/minimal workbook must continue to use fallback successfully.
- A complete workbook + relationships pair must retain strict declared-sheet resolution.
- Re-read current source/test after SHA-guarded integration and preserve concurrent history.

## Coordination

The shared-string-index and worksheet-row-capacity claims are completed. Recent ownership search found no independent `XlsxHandleReader` relationship/metadata owner. This claim is limited to partial workbook metadata.

## Completion condition

Completed only when partial workbook metadata fails closed, both valid resolution modes remain covered, focused regression source is present on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
