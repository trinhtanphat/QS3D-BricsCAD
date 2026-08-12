# Room Finish XLSX numeric round-trip

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: ACTIVE
- Registered: 2026-08-12 13:45 +07:00
- Baseline main: `518b474f329e361549c2cbf761c9ec19509b3cf6`

## Reserved scope

- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs` — numeric SpreadsheetML cell serialization only.
- `tests/QS3D.Core.SmokeTests/RoomFinishXlsxNumericRoundTripSmoke.cs` — new focused auto-registered regression only.
- This claim file.

## Coordination

The long-lived LOCAL-003 expansion at `01409579e764852489ecc6b20a02dc3521f869a4` reserves only the legacy `tests/QS3D.Core.SmokeTests/RoomFinishXlsxSmoke.cs` fixture and explicitly excludes exporter edits. This lane does not modify that reserved fixture.

## Defect

`RoomFinishXlsxExporter.NumberCell` serializes stored SpreadsheetML `<v>` values with `0.########`. Finite Room Finish quantities beyond eight decimal places are rounded in the workbook payload, including non-zero values such as `1e-9` becoming `0`.

## Intended fix

Use invariant round-trip formatting for numeric worksheet payloads while preserving existing finite preflight, styles/schema, row snapshotting, validation, and atomic publication. Add a separate auto-registered smoke that proves exact round-trip for a small finite non-zero quantity without touching the LOCAL-003 fixture.

## Validation boundary

Remote source/readback and focused regression source only. No GitHub Actions, full .NET build/smoke, or BricsCAD V25/V26 runtime PASS is claimed by this lane.
