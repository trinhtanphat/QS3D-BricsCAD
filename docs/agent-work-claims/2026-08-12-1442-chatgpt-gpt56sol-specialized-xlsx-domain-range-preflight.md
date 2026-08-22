# Work claim — Specialized XLSX domain-range preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-specialized-xlsx-domain-range-preflight-20260812-1442`
- Registered: `2026-08-12T14:42:00+07:00`
- Baseline main SHA: `3c78ab29bdbcc7807133cbb0b0e07fa3588e4ac4`
- Priority: P2 deterministic export / fail-closed physical range integrity

## Confirmed defect

The public Material Usage, Door/Opening, and Curtain Wall XLSX exporters snapshotted caller-provided mutable row DTOs and preflighted text/finite numeric representation before publication, but they did not enforce the non-negative/positive physical range contracts already enforced by their corresponding schedule builders.

As a result a caller could bypass the schedule builders and publish negative element/count values or negative physical lengths/areas through these public exporter APIs. Door/opening rows could additionally publish non-positive width/height, and curtain rows could publish an inverted clear-panel range (`minimum > maximum`). Standard BQ/ED2 XLSX publication already rejected negative physical values, so these specialized exporters were inconsistent at the same public publication boundary.

## Reserved scope

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/SpecializedXlsxDomainRangePreflightSmoke.cs`
- this claim file

## Completed fix

- Material Usage rejects negative `ElementCount` and non-finite/negative primary and physical metric values before publication.
- Door/Opening rejects negative `Count`/`HostCount`, requires finite positive width/height, and requires finite non-negative sill, thickness, and opening area.
- Curtain Wall rejects negative count/physical values and inverted clear-panel width/height ranges.
- All checks occur on snapshotted rows before destination-directory/temp publication work; worksheet schema, numeric formatting, package writer/validator, and prior round-trip behavior remain unchanged.

## Integration evidence

- Claim commit: `e0f740499d6e278ff9d532eee6e1a0ec7cd29709`
- Material Usage source fix: `7ec9a3d3d644a37d549eab747b1335409d359062`
- Door/Opening source fix: `82e52a707bd0ebdd12fd7732f9f9a3bbffd2778e`
- Curtain Wall source fix: `97f4b89ba81bf292ed9cc953ea8ab479bfc6c2c5`
- Focused smoke: `8b5ef912fa3be73dac601c09b7083c7699d5c00d`
- Source diffs and smoke were re-read after integration; no out-of-scope source changes were observed in these commits.

## Validation boundary

Static/source verification only in this hosted session. GitHub Actions were not dispatched or rerun. No local `dotnet`/Core smoke execution and no BricsCAD V25/V26 runtime PASS are claimed.

## Excluded scope

- `RoomFinishXlsxExporter.cs` while its numeric round-trip claim remains unresolved in current history;
- no reporting-builder calculation changes;
- no BricsCAD/native/UI changes;
- no GitHub Actions or licensed runtime qualification.
