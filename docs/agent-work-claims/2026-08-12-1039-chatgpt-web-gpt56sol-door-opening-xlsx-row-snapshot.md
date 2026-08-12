# Work claim — Door/Opening XLSX row snapshot integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-door-opening-xlsx-row-snapshot-20260812-1039`
- Registered: `2026-08-12T10:39:00+07:00`
- Baseline main SHA: `3aa33193e8cf5a6141a795c074ef222cd64a0854`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`DoorOpeningXlsxExporter.Export(...)` preflighted a caller-owned `IReadOnlyList<DoorOpeningScheduleRow>` through multiple passes, then re-read the same external rows and mutable `ElementIds` / `HostIds` after destination-directory/temp-file creation in `BuildSheet(rows)`. A mutating or hostile list/row could therefore serialize data that was not preflighted, or fail only after filesystem side effects had begun.

## Reserved scope

- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/DoorOpeningXlsxRowSnapshotSmoke.cs`
- this claim file for close-out

## Implemented contract

- Capture the bounded row count once.
- Read each caller-owned row index once before any filesystem mutation.
- Deep-copy all worksheet scalar fields plus `ElementIds` and `HostIds` into detached rows before I/O.
- Bound and copy joined-ID cells using the existing 32,767-character Excel cell contract.
- Validate and serialize only the detached snapshot.
- Preserve current max-row, text, numeric finite, XML/package and atomic-replace semantics.

## Exclusions

- No schedule-builder, semantic host, physical opening, UI/command or quantity behavior changes.
- No changes to other XLSX exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion evidence

- Claim registration: `ed59e2f940813d2f98fdc2535bc0f465f44abe9c`.
- Source branch fix: `09cd47e92a73543837493c00fdd4f030d6f77515`.
- Focused smoke source: `b7b5c372de21d8fe38555556485b499731043938`.
- PR: `#767`.
- Squash integration on `main`: `6577333d645910687c08199343026fa5f6c2f3af`.
- Post-merge readback confirmed `main` constructs a detached `snapshot`, deep-copies `ElementIds` / `HostIds` with bounded indexed reads, validates that snapshot, and passes only `snapshot` to `BuildSheet`.
- Post-merge readback confirmed `DoorOpeningXlsxRowSnapshotSmoke` rejects any second caller-row index read or caller-list enumeration.

## Validation boundary

Focused smoke coverage was added and read back from `main`, but it was not executed in this remote session. No GitHub Actions, local .NET build, BricsCAD V25/V26 runtime, release or signing PASS is claimed.