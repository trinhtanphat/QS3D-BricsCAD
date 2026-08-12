# Curtain XLSX numeric round-trip

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: COMPLETED
- Registered: 2026-08-12 13:36 +07:00
- Baseline main: `5bdd38c0b16337ad24a63c19ae4a2cf51da765af`
- Claim commit: `0db144fde525c1d673c7597ac231693f9d7a3eaf`
- Source fix: `b222c12e93c4f773f9b9bebf301e7ea0d91faa33`
- Regression: `eb758840b2b21581a961b7acc2dc0acc0a6b5227`

## Reserved scope

- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs` — numeric SpreadsheetML cell serialization only.
- `tests/QS3D.Core.SmokeTests/CurtainWallXlsxSmoke.cs` — focused numeric round-trip regression only.
- This claim file.

## Defect

`CurtainWallXlsxExporter.AppendNumberCell` serialized stored SpreadsheetML `<v>` values with `0.########`. Finite curtain quantities beyond eight decimal places were rounded in the workbook payload, including non-zero values such as `1e-9` becoming `0`.

## Resolution

Numeric cells now use invariant round-trip (`R`) serialization while preserving existing finite preflight, styles/schema, row snapshotting, validation, and atomic publication. The Curtain XLSX smoke now stores `TotalWallLengthM = 1e-9`, reads worksheet cell `D2`, parses the stored numeric lexeme with invariant culture, and requires exact double round-trip while retaining prior package/header assertions.

## Validation boundary

Current-main source and regression readback confirmed the exact change and focused assertion. No GitHub Actions, full .NET build/smoke, or BricsCAD V25/V26 runtime PASS is claimed by this lane.
