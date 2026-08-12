# Door Opening XLSX numeric round-trip

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: ACTIVE
- Registered: 2026-08-12 13:26 +07:00
- Baseline main: `a3bcfe9c39e44ace69b9183b68ef27b4a58b243d`

## Reserved scope

- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs` — numeric SpreadsheetML cell serialization only.
- `tests/QS3D.Core.SmokeTests/DoorOpeningXlsxSmoke.cs` — focused numeric round-trip regression only.
- This claim file.

## Defect

`DoorOpeningXlsxExporter.NumberCell` serializes the stored SpreadsheetML `<v>` value with `0.########`. That is not merely display formatting: finite values beyond eight decimal places are rounded in the workbook payload, and a non-zero value such as `1e-9` becomes `0`.

## Intended fix

Serialize finite doubles with invariant round-trip formatting while preserving existing finite-value preflight, worksheet styles/schema, row snapshotting, package validation, and atomic publication. Extend the existing Door/Opening XLSX smoke to prove a small finite non-zero value round-trips through the worksheet payload.

## Validation boundary

Remote source/readback and focused regression source only. No GitHub Actions, full .NET build/smoke, or BricsCAD V25/V26 runtime PASS is claimed by this lane.
