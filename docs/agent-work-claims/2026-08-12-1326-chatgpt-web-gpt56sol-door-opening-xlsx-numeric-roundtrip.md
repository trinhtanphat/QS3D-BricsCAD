# Door Opening XLSX numeric round-trip

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: COMPLETED
- Registered: 2026-08-12 13:26 +07:00
- Baseline main: `a3bcfe9c39e44ace69b9183b68ef27b4a58b243d`
- Claim commit: `f74d0d5b0d7a9b9584a4def7f7d00c33d98177f0`
- Source fix: `e88ac714a1ec8abf7b9d7174f6f47859ab8b3aa9`
- Regression: `fcfddbb71d41e7fe356a8c2490fa2a561880765c`

## Reserved scope

- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs` — numeric SpreadsheetML cell serialization only.
- `tests/QS3D.Core.SmokeTests/DoorOpeningXlsxSmoke.cs` — focused numeric round-trip regression only.
- This claim file.

## Defect

`DoorOpeningXlsxExporter.NumberCell` serialized the stored SpreadsheetML `<v>` value with `0.########`. That was not merely display formatting: finite values beyond eight decimal places were rounded in the workbook payload, and a non-zero value such as `1e-9` became `0`.

## Resolution

Numeric cells now use invariant round-trip (`R`) serialization while preserving finite-value preflight, worksheet styles/schema, row snapshotting, package validation, and atomic publication. The existing Door/Opening XLSX smoke now writes `WidthM = 1e-9`, reads worksheet cell `E2` directly, parses the stored numeric lexeme with invariant culture, and requires exact double round-trip while retaining the prior package/provenance/XML-sanitization assertions.

## Validation boundary

Current-main source and regression readback confirmed the exact change and focused assertion. No GitHub Actions, full .NET build/smoke, or BricsCAD V25/V26 runtime PASS is claimed by this lane.
