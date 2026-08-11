# Work claim — Curtain XLSX cell text limit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-xlsx-text-20260812-0110`
- Registered: `2026-08-12T01:10:00+07:00`
- Baseline main SHA: `fbc53d6b26757fdb736e5b7806bd741da0d23712`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defect

`CurtainWallXlsxExporter` enforces the worksheet row limit and sanitizes XML text, but writes `CurtainWallScheduleRow.Floor` and `.FamilyName` directly as inline strings without enforcing Excel's 32,767-character cell-content limit. Both row properties are publicly mutable and unrestricted, so a caller can supply a structurally valid row with an oversized cell and the exporter can publish a ZIP/XML package that exceeds the worksheet cell-value contract.

## Reserved scope

Fail closed before filesystem mutation when either Curtain XLSX inline-string row value exceeds 32,767 characters. Preserve the existing row bound, XML sanitization, numeric formatting, worksheet shape, package validation and atomic publish behavior.

## Expected surfaces

- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallXlsxCellTextLimitSmoke.cs`
- this claim file

## Excluded scope

- No `CurtainWallSchedule.cs` grouping or quantity semantics changes.
- No `XlsxXmlText` shared-policy changes.
- No Door/Opening or Material XLSX exporter changes.
- No Curtain geometry/generated-health/UI/native/runtime or GitHub Actions work.

## Validation plan

- Accept exactly 32,767 characters in a Curtain text cell.
- Reject 32,768 characters before destination directory/file creation.
- Preserve ordinary one-row export and finite numeric validation.
- Use current-file SHA guards, re-read current `main` after integration, and preserve concurrent history.
- Source/smoke review only; no .NET or BricsCAD runtime PASS unless actually executed.

## Coordination

Recent current-main comparison after the first branch-churned claim attempt showed only README/ProjectMaterialCatalog/other claims changed; `CurtainWallXlsxExporter.cs` remained untouched. Active Curtain runtime-health/geometry work does not own this exporter. Door and Material XLSX cell-limit lanes are separate.

## Completion condition

Completed only when Curtain XLSX enforces the cell text limit before filesystem mutation, focused regression source is present on current `main`, exact commits are recorded here and this claim is marked `COMPLETED`.
