# Work claim — Curtain XLSX row snapshot integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-xlsx-row-snapshot-20260812-1043`
- Registered: `2026-08-12T10:43:00+07:00`
- Baseline main SHA: `ef760d184956ef2a1aa178403f2bd6cb0a8823f7`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`CurtainWallXlsxExporter.Export(...)` validated caller-owned `IReadOnlyList<CurtainWallScheduleRow>` values before filesystem mutation, but `BuildSheet(rows)` later re-read the same mutable external rows after destination-directory/temp-file creation. A changing or hostile caller could therefore serialize data that was never validated or fail only after side effects had begun.

## Reserved scope

- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallXlsxRowSnapshotSmoke.cs`
- this claim file for close-out

## Implemented contract

- Capture row count once and enforce the existing Excel worksheet bound.
- Read each caller-owned row index once and copy every worksheet field to a detached row before any path/directory/temp-file mutation.
- Validate and serialize only that detached snapshot.
- Preserve current cell-text, finite-number, workbook/package and atomic replace behavior.

## Exclusions

- No Curtain geometry/planning/materialization/Health changes.
- No schedule-builder or UI/command changes.
- No changes to other exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion evidence

- Claim registration: `59575b23bdba50159a70120c67c67bcca5d0b558`.
- Source branch fix: `ff08c5a9a650360bc3a12af639cdb9fb6bacfd7c`.
- Focused smoke source: `3f3b45df4362facc7df00a8161f4f28b9224dba1`.
- PR: `#770`.
- Squash integration on `main`: `aab6449b8b02fb1fc5b0eb504942bb58a1f80bd5`.
- Post-merge readback confirmed `main` builds and validates a detached `snapshot` before filesystem work and passes only that snapshot to `BuildSheet`.
- Post-merge readback confirmed `CurtainWallXlsxRowSnapshotSmoke` requires exactly one caller-row indexed read and rejects caller-list enumeration.

## Validation boundary

Focused smoke coverage was added and read back from `main`, but it was not executed in this remote session. No GitHub Actions, local .NET build, BricsCAD V25/V26 runtime, release or signing PASS is claimed.