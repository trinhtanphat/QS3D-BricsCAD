# Material XLSX numeric round-trip

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: COMPLETED
- Registered: 2026-08-12 13:32 +07:00
- Baseline main: `a054e87f37c70c8ca1a5a02878b94665369c8b48`
- Claim commit: `04845723660a7421a7cfe66b8edbd0e413988513`
- Source fix: `fcbdacea774ec09879b7a0ccb7ed023d6415e1f2`
- Regression: `a7838749c7a1a379f8640dc94a4e0cbe56bcd2e0`

## Reserved scope

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs` — numeric SpreadsheetML cell serialization only.
- `tests/QS3D.Core.SmokeTests/MaterialUsageXlsxSmoke.cs` — focused numeric round-trip regression only.
- This claim file.

## Defect

`MaterialUsageXlsxExporter.NumberCell` serialized the stored SpreadsheetML `<v>` value with `0.########`. Finite values beyond eight decimal places were rounded in the workbook payload; small non-zero material quantities such as `1e-9` could be persisted as `0`.

## Resolution

Numeric cells now use invariant round-trip (`R`) serialization while preserving finite-value preflight, styles/schema, row snapshotting, validation, and atomic publication. The Material XLSX smoke now stores `LengthM = 1e-9`, reads worksheet cell `I2`, parses the stored numeric lexeme with invariant culture, and requires exact double round-trip while retaining prior package/XML sanitization checks.

## Validation boundary

Current-main source and regression readback confirmed the exact change and focused assertion. No GitHub Actions, full .NET build/smoke, or BricsCAD V25/V26 runtime PASS is claimed by this lane.
