# Work claim — Specialized XLSX domain-range preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-specialized-xlsx-domain-range-preflight-20260812-1442`
- Registered: `2026-08-12T14:42:00+07:00`
- Baseline main SHA: `3c78ab29bdbcc7807133cbb0b0e07fa3588e4ac4`
- Priority: P2 deterministic export / fail-closed physical range integrity

## Confirmed defect

The public Material Usage, Door/Opening, and Curtain Wall XLSX exporters snapshot caller-provided mutable row DTOs and preflight text/finite numeric representation before publication, but they do not enforce the non-negative/positive physical range contracts already enforced by their corresponding schedule builders.

As a result a caller can bypass the schedule builders and publish negative element/count values or negative physical lengths/areas through these public exporter APIs. Door/opening rows can additionally publish non-positive width/height, and curtain rows can publish an inverted clear-panel range (`minimum > maximum`). Standard BQ/ED2 XLSX publication already rejects negative physical values, so these specialized exporters are inconsistent at the same public publication boundary.

## Reserved scope

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- one focused Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Expected fix

Preflight the snapshotted rows before any destination directory/temp publication work:

- Material Usage: `ElementCount` and all physical metric/primary-quantity values must be non-negative and finite.
- Door/Opening: `Count` and `HostCount` must be non-negative; width/height must be finite and strictly positive; sill, thickness, and opening area must be finite and non-negative.
- Curtain Wall: all count fields and physical metrics must be non-negative; clear-panel min/max ranges must be ordered.

Preserve current row limits, cell-text limits, snapshot semantics, numeric round-trip formatting, and package validation.

## Regression plan

- each exporter rejects representative negative caller-supplied counts/metrics before publishing a missing destination;
- Door/Opening rejects zero/non-positive width or height while accepting normal positive values;
- Curtain rejects inverted clear-panel width/height ranges;
- valid representative rows still reach normal publication behavior in existing round-trip coverage.

## Excluded scope

- `RoomFinishXlsxExporter.cs` while its numeric round-trip claim remains unresolved in current history;
- no reporting-builder calculation changes;
- no BricsCAD/native/UI changes;
- no GitHub Actions or licensed runtime qualification.

## Completion condition

Source and focused smoke are integrated to `main`, current source/test are re-read, and this claim is marked `COMPLETED` with exact integration SHA(s).
