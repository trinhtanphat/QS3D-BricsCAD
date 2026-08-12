# Work claim — XLSX Handle reader worksheet relationship type

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-relationship-type-20260812-0751`
- Registered: `2026-08-12T07:51:00+07:00`
- Baseline main SHA: `3a766aeb9192ae12d42fc4f9bd2d27b05baaae37`
- Priority: P2 evidence-driven remote-safe XLSX input-integrity hardening

## Confirmed defect

`XlsxHandleReader.ResolveWorksheet(...)` selected the relationship matching a workbook sheet's `r:id`, but did not verify that the matched relationship was actually a worksheet relationship. A malformed workbook could bind a `<sheet>` id to another internal part type and the reader would load that relationship target as if it were worksheet XML.

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

## Validation implemented

- The relationship selected by workbook sheet `r:id` must now use an accepted worksheet relationship Type before TargetMode/Target resolution.
- Focused smoke covers the repository's `http` worksheet Type and Microsoft-documented `https` form as accepted.
- A styles relationship Type pointing at XML that otherwise looks exactly like a worksheet is rejected, proving the guard validates package relationship semantics instead of trusting target XML shape.
- Source commit readback confirms only relationship-type constants/checks were added.
- Regression commit remains an ancestor of current `main`; subsequent commits touched only Curtain preflight and WallJunction geometry.

## Integration commits

- Claim: `b14fe814b0af763301fc6dc1dc1d024f545930b5`
- Source fix: `89b358b40b70bad70695c8d768733c905f9154fa`
- Focused smoke: `9eccb7f33bee617890af2417b7502dc148670e86`

## Evidence

Microsoft Learn's SpreadsheetML structure documentation shows workbook-to-worksheet relationships using the `schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet` relationship type.

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

The shared-string-index, worksheet-row-capacity, optional-row-index and partial-workbook-metadata claims are completed. No independent `XlsxHandleReader` relationship-type owner was found before registration.

## Completion condition

Completed: non-worksheet workbook relationships fail closed, both accepted worksheet URI forms are covered, focused regression source is on current `main`, and exact integration SHAs are recorded above.
