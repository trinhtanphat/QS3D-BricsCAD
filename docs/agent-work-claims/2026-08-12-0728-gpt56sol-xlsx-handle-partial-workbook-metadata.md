# Work claim — XLSX Handle reader partial workbook metadata guard

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-partial-metadata-20260812-0728`
- Registered: `2026-08-12T07:28:00+07:00`
- Baseline main SHA: `7e0e4403173f6aef378732b5d45c350c19b81496`
- Priority: P2 evidence-driven remote-safe XLSX input-integrity hardening

## Confirmed defect

`XlsxHandleReader.ResolveWorksheet(...)` used the strict workbook/relationship graph only when both `xl/workbook.xml` and `xl/_rels/workbook.xml.rels` existed. If exactly one existed, it silently fell back to `sheet1.xml`/the first worksheet entry, allowing partially corrupted workbook metadata to be bypassed.

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

## Validation implemented

- Workbook XML without workbook relationships now fails closed even when `sheet1.xml` exists.
- Workbook relationships without workbook XML likewise fail closed.
- Focused smoke preserves the metadata-free fallback path.
- Complete workbook metadata is proven to follow its declared relationship: `sheet1` contains handle `1A`, while the declared relationship targets `sheet2` containing `2B`, and the expected result is `2B`.
- Source commit readback confirms the implementation diff is exactly the partial-metadata guard.
- Regression commit remains an ancestor of current `main`; the subsequent commit touched only Preview Review.

## Integration commits

- Claim: `532af2b8cb54116ce52b2be2f7b7c8a0fe9ac6a9`
- Source fix: `aaaf114032cf288dd235123e7ae49a6ae8cfb108`
- Focused smoke: `aca8c1d154d4c8785f6cc09331e28cc0d5026b38`

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

The shared-string-index and worksheet-row-capacity claims are completed. No independent `XlsxHandleReader` relationship/metadata owner was found before registration. This claim remained limited to partial workbook metadata.

## Completion condition

Completed: partial workbook metadata fails closed, both valid resolution modes remain covered, focused regression source is present on current `main`, and exact integration SHAs are recorded above.
