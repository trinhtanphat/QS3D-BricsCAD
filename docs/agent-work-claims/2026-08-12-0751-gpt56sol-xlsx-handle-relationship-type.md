# Work claim — XLSX Handle reader worksheet relationship type

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-relationship-type-20260812-0751`
- Registered: `2026-08-12T07:51:00+07:00`
- Baseline main SHA: `3a766aeb9192ae12d42fc4f9bd2d27b05baaae37`
- Priority: P2 evidence-driven remote-safe XLSX input-integrity hardening

## Confirmed defect

`XlsxHandleReader.ResolveWorksheet(...)` selects the relationship matching a workbook sheet's `r:id`, but it does not verify that the matched relationship is actually a worksheet relationship. A malformed workbook can bind a `<sheet>` id to another internal part type and the reader will still load that relationship target as if it were worksheet XML.

## Reserved scope

Require the selected workbook relationship to use the SpreadsheetML worksheet relationship type before resolving its target. Accept the repository's existing `http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet` form and the equivalent `https://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet` form documented by Microsoft Learn. Preserve relationship-id matching, TargetMode checks, target-path guards, legacy metadata-free fallback and all handle parsing behavior.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleWorksheetRelationshipTypeSmoke.cs`
- this claim file

## Excluded scope

- No relationship target normalization redesign.
- No XLSX exporter changes.
- No BLT/ED2 handle parsing semantics.
- No UI/native BricsCAD/runtime, persistence or GitHub Actions work.

## Validation plan

- A matching `r:id` whose relationship Type is styles/sharedStrings/other must fail closed even if its target XML resembles a worksheet.
- The existing `http` worksheet relationship type must remain accepted.
- The documented `https` worksheet relationship type must also be accepted.
- Preserve metadata-free fallback and external-relationship rejection.
- Re-read source/test after SHA-guarded integration and preserve concurrent history.

## Evidence

Microsoft Learn's SpreadsheetML structure documentation shows workbook-to-worksheet relationships using the `schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet` relationship type.

## Coordination

The shared-string-index, worksheet-row-capacity, optional-row-index and partial-workbook-metadata claims are completed. Recent current-main searches found no independent `XlsxHandleReader` relationship-type owner.

## Completion condition

Completed only when non-worksheet workbook relationships fail closed, both official worksheet URI forms are covered, focused regression source is on current `main`, exact integration SHAs are recorded and this claim is marked `COMPLETED`.
