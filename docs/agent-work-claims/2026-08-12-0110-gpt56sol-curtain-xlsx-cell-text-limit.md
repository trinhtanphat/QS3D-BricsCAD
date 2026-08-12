# Work claim — Curtain XLSX cell text limit

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-xlsx-text-20260812-0110`
- Registered: `2026-08-12T01:10:00+07:00`
- Completed: `2026-08-12T01:13:00+07:00`
- Baseline main SHA: `fbc53d6b26757fdb736e5b7806bd741da0d23712`
- Claim commit: `42582bb6a84f14e2c64d037438448c25e58cdf9e`
- Source fix commit: `39442858f3eafce73d11c4883c695debb56aa984`
- Regression commit: `4590d6f6cc028319eb54135ba3c393473dae530f`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defect

`CurtainWallXlsxExporter` enforced the worksheet row limit and sanitized XML text, but wrote `CurtainWallScheduleRow.Floor` and `.FamilyName` directly as inline strings without enforcing Excel's 32,767-character cell-content limit. Both row properties are publicly mutable and unrestricted, so a caller could supply a structurally valid row with an oversized cell and the exporter could publish a ZIP/XML package exceeding the worksheet cell-value contract.

## Implemented

- Added `MaxCellTextCharacters = 32767` to `CurtainWallXlsxExporter`.
- Both exported row string fields are validated before path resolution, destination directory creation, temp-file creation or package writing.
- Runtime null strings retain existing empty-string serialization behavior.
- Exactly 32,767 characters remain accepted; values longer than the limit fail with `ArgumentOutOfRangeException` before filesystem mutation.
- Existing row bound, `XlsxXmlText` sanitization, numeric finite checks, worksheet/package structure and atomic publication remain unchanged.
- Added module-initializer smoke `CurtainWallXlsxCellTextLimitSmoke` proving exact-limit publication and 32,768-character side-effect-free rejection.

## Implemented surfaces

- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallXlsxCellTextLimitSmoke.cs`
- this claim file

## Excluded scope honored

- No `CurtainWallSchedule.cs` grouping or quantity semantics changes.
- No `XlsxXmlText` shared-policy changes.
- No Door/Opening or Material XLSX exporter changes.
- No Curtain geometry/generated-health/UI/native/runtime or GitHub Actions work.

## Validation actually performed

- Claim-first commit was present at current `main` before implementation; exact current exporter blob was re-fetched and SHA-guarded before the source write.
- Current `main` was re-read after implementation and contains `MaxCellTextCharacters` plus pre-filesystem validation for Floor/FamilyName.
- Focused smoke source was re-read and contains both exact-limit acceptance and oversized side-effect-free rejection.
- Regression commit `4590d6f6cc028319eb54135ba3c393473dae530f` was verified as an ancestor of later current `main` `b085e99a1d58a3e414537db3b932e0ce37efd532`; subsequent commits touched only a Floor offset claim and Room Finish registration.
- No force push/reset/revert was used.
- No local .NET smoke execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime or GitHub Actions execution is claimed.

## Coordination

Active Curtain runtime-health/geometry work did not own this exporter. Door and Material XLSX cell-limit lanes remained separate and were not modified.

## Completion condition

Completed. Curtain XLSX now enforces the 32,767-character cell text limit before filesystem mutation, focused regression source is present on current `main`, exact commits are recorded above and concurrent history was preserved.
