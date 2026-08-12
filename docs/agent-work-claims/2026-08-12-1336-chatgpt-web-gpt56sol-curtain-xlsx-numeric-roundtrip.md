# Curtain XLSX numeric round-trip

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: ACTIVE
- Registered: 2026-08-12 13:36 +07:00
- Baseline main: `5bdd38c0b16337ad24a63c19ae4a2cf51da765af`

## Reserved scope

- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs` — numeric SpreadsheetML cell serialization only.
- `tests/QS3D.Core.SmokeTests/CurtainWallXlsxSmoke.cs` — focused numeric round-trip regression only.
- This claim file.

## Defect

`CurtainWallXlsxExporter.AppendNumberCell` serializes stored SpreadsheetML `<v>` values with `0.########`. Finite curtain quantities beyond eight decimal places are rounded in the workbook payload, including non-zero values such as `1e-9` becoming `0`.

## Intended fix

Use invariant round-trip formatting for numeric worksheet payloads while preserving existing finite preflight, styles/schema, row snapshotting, validation, and atomic publication. Extend the existing Curtain XLSX smoke with exact round-trip coverage for a small finite non-zero quantity.

## Validation boundary

Remote source/readback and focused regression source only. No GitHub Actions, full .NET build/smoke, or BricsCAD V25/V26 runtime PASS is claimed by this lane.
