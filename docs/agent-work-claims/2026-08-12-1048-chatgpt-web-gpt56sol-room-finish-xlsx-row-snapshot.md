# Work claim — Room Finish XLSX row snapshot integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-xlsx-row-snapshot-20260812-1048`
- Registered: `2026-08-12T10:48:00+07:00`
- Baseline main SHA: `f81f916fede7735d9bd35fd0bd6de0ff5ffae69d`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`RoomFinishXlsxExporter.Export(...)` validates caller-owned `IReadOnlyList<RoomFinishScheduleRow>` values plus mutable `ElementIds` / `RoomIds` before filesystem mutation, but later `BuildSheet(rows)` re-reads the same external rows and joins the original nested lists after directory/temp-file creation. Mutated or hostile inputs can therefore serialize data not covered by preflight, or fail only after filesystem side effects begin.

## Reserved scope

- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- a new focused smoke file for row-snapshot integrity
- this claim file for close-out

## Contract

- Capture bounded row count once.
- Read each caller-owned row index once before filesystem mutation.
- Deep-copy all worksheet scalar fields plus `ElementIds` and `RoomIds` into detached rows.
- Bound joined-ID cells during indexed copy using the existing 32,767-character Excel cell contract.
- Validate and serialize only the detached snapshot.
- Preserve current XML sanitization, worksheet limits, finite-number validation, package validation and atomic replacement.

## Coordination / exclusions

- Do not edit the separately reserved legacy fixture `tests/QS3D.Core.SmokeTests/RoomFinishXlsxSmoke.cs`.
- No Room Finish schedule-builder, identity, Health, UI/command or quantity changes.
- No changes to other exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Validation plan

Add a new smoke using a caller-owned row list that allows one indexed row read and rejects enumeration/second reads. Export must succeed from the detached snapshot while preserving existing text/numeric guards in source.

## Completion condition

Source fix and new smoke source are integrated on current `main`, read back after merge, and this claim is marked `COMPLETED` with exact SHA/PR evidence and remote validation boundaries.