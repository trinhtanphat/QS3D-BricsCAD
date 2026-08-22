# Work claim — Room Finish XLSX row snapshot integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish-xlsx-row-snapshot-20260812-1048`
- Registered: `2026-08-12T10:48:00+07:00`
- Baseline main SHA: `f81f916fede7735d9bd35fd0bd6de0ff5ffae69d`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`RoomFinishXlsxExporter.Export(...)` validated caller-owned `IReadOnlyList<RoomFinishScheduleRow>` values plus mutable `ElementIds` / `RoomIds` before filesystem mutation, but later `BuildSheet(rows)` re-read the same external rows and joined the original nested lists after directory/temp-file creation. Mutated or hostile inputs could therefore serialize data not covered by preflight, or fail only after filesystem side effects began.

## Reserved scope

- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishXlsxRowSnapshotSmoke.cs`
- this claim file for close-out

## Implemented contract

- Capture bounded row count once.
- Read each caller-owned row index once before filesystem mutation.
- Deep-copy all worksheet scalar fields plus `ElementIds` and `RoomIds` into detached rows.
- Bound joined-ID cells during indexed copy using the existing 32,767-character Excel cell contract.
- Validate and serialize only the detached snapshot.
- Preserve current XML sanitization, worksheet limits, finite-number validation, package validation and atomic replacement.

## Coordination / exclusions

- The separately reserved legacy fixture `tests/QS3D.Core.SmokeTests/RoomFinishXlsxSmoke.cs` was not edited.
- No Room Finish schedule-builder, identity, Health, UI/command or quantity changes.
- No changes to other exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion evidence

- Claim registration: `bc2301b29fb858fce1db0a085a2d9d67505a9589`.
- Source branch fix: `3bcea0c569be61839ff73e84ab633e2848c689ec`.
- Focused smoke source: `9d7d2008493af6ae6b0790c6515c82795074fbd2`.
- PR: `#782`.
- Squash integration on `main`: `4c7c8a9258c062cf2ff7c06868ba3ba39e107cea`.
- Post-merge readback confirmed `main` deep-copies worksheet scalars plus `ElementIds` / `RoomIds`, validates the detached row, and passes only `snapshot` to `BuildSheet`.
- Post-merge readback confirmed the new `RoomFinishXlsxRowSnapshotSmoke` requires exactly one caller-row indexed read without touching the legacy reserved fixture.

## Validation boundary

Focused smoke coverage was added and read back from `main`, but it was not executed in this remote session. No GitHub Actions, local .NET build, BricsCAD V25/V26 runtime, release or signing PASS is claimed.