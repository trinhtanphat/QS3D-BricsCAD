# Material XLSX numeric round-trip

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: ACTIVE
- Registered: 2026-08-12 13:32 +07:00
- Baseline main: `a054e87f37c70c8ca1a5a02878b94665369c8b48`

## Reserved scope

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs` — numeric SpreadsheetML cell serialization only.
- `tests/QS3D.Core.SmokeTests/MaterialUsageXlsxSmoke.cs` — focused numeric round-trip regression only.
- This claim file.

## Defect

`MaterialUsageXlsxExporter.NumberCell` serializes the stored SpreadsheetML `<v>` value with `0.########`. Finite values beyond eight decimal places are rounded in the workbook payload; small non-zero material quantities such as `1e-9` can be persisted as `0`.

## Intended fix

Use invariant round-trip formatting for numeric worksheet payloads while preserving finite-value preflight, styles/schema, row snapshotting, validation, and atomic publication. Extend the existing Material XLSX smoke to prove exact round-trip for a small finite non-zero source value.

## Validation boundary

Remote source/readback and focused regression source only. No GitHub Actions, full .NET build/smoke, or BricsCAD V25/V26 runtime PASS is claimed by this lane.
