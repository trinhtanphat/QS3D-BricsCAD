# Work claim — Material XLSX cell text limit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-xlsx-text-20260812-0103`
- Registered: `2026-08-12T01:03:00+07:00`
- Baseline main SHA: `198df88b4ee48bb977f1e1dc0f4292cd035624ea`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defect

`MaterialUsageXlsxExporter` enforces the worksheet row limit but writes every inline-string cell directly through `XlsxXmlText.Escape(...)` without enforcing Excel's 32,767-character cell-content limit. `MaterialUsageRow` exposes unrestricted string properties, so a caller can provide a valid row whose Floor, MaterialName, UnitHint, Component, Category or FamilyName exceeds the XLSX cell limit. The exporter can then publish a package that its structural ZIP/XML validator accepts but Excel cannot represent as a valid cell value.

## Reserved scope

Fail closed before filesystem mutation when any Material Usage XLSX inline-string value exceeds 32,767 characters. Preserve the existing row bound, XML sanitization, numeric formatting, worksheet shape and atomic publish behavior.

## Expected surfaces

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- one focused Core smoke file under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- No `XlsxXmlText` shared-policy changes.
- No Door/Opening or Curtain XLSX exporter changes; Door has an independent active cell-limit lane.
- No Material Usage schedule grouping/catalog/business-rule changes.
- No UI/native BricsCAD/runtime or GitHub Actions work.

## Validation plan

- Accept exactly 32,767 characters in a material string cell.
- Reject 32,768 characters before destination directory/file creation.
- Preserve normal one-row export and numeric finite checks.
- Use current-file SHA guards, re-read current `main` after integration and preserve concurrent history.
- Source/smoke review only; no .NET or BricsCAD runtime PASS unless actually executed.

## Coordination

The completed Material Usage group-key collision lane owned `MaterialUsageSchedule.cs`, not this exporter. The active Door XLSX cell-limit claim explicitly excludes other exporters. This claim reserves only Material Usage XLSX serialization and its dedicated smoke.

## Completion condition

Completed only when the Material Usage exporter enforces the XLSX cell text limit before filesystem mutation, focused regression source is present on current `main`, exact commits are recorded here and this claim is marked `COMPLETED`.
