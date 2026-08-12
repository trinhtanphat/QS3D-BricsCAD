# Work claim — Material XLSX row snapshot integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-xlsx-row-snapshot-20260812-1034`
- Registered: `2026-08-12T10:34:00+07:00`
- Baseline main SHA: `e7c5e5fbb5b6cccfeff910b0e94a867ed556a177`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`MaterialUsageXlsxExporter.Export(...)` validated the caller-owned `IReadOnlyList<MaterialUsageRow>` before filesystem mutation, but later `BuildSheet(rows)` re-read the same external list and mutable row objects after destination-directory/temp-file creation. A list/indexer or row mutated between those phases could therefore serialize data that was never preflighted, or throw only after filesystem side effects had started.

## Reserved scope

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/MaterialUsageXlsxRowSnapshotSmoke.cs`
- this claim file for close-out

## Implemented contract

- Capture the bounded row count once.
- Read each caller-owned row index once and copy every worksheet field into a detached `MaterialUsageRow` before any path/directory/temp-file mutation.
- Validate the detached row snapshot and serialize only that snapshot.
- Do not re-read or enumerate caller-owned rows after preflight.
- Preserve the existing Excel row/text limits, numeric finite checks, XML escaping, package validation, atomic replace and workbook format.

## Exclusions

- No UI/command changes, schedule-builder changes, quantity semantics or XLSX schema changes.
- No changes to other exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim.

## Completion evidence

- Claim registration: `4c23efdcd3ce26e9185228e7308082808fa929de`.
- Source branch fix: `c6b857d296ef40afcc7624374815237c0129ce6e`.
- Focused smoke source: `aefe223ca7b2ccaff67dc517f0f7227352ac43a6`.
- PR: `#763`.
- Squash integration on `main`: `7b6bcfdc7e8030eef2fe84540d437f1f584917f8`.
- Post-merge readback confirmed `main` builds and validates a detached `snapshot` before `Path.GetFullPath` / `Directory.CreateDirectory`, and `BuildSheet(snapshot)` never receives the external row collection.
- Post-merge readback confirmed `MaterialUsageXlsxRowSnapshotSmoke` uses a one-read caller list and requires a successful XLSX export from the detached snapshot.

## Validation boundary

Focused smoke coverage was added and read back from `main`, but it was not executed in this remote session. No GitHub Actions, local .NET build, BricsCAD V25/V26 runtime, release or signing PASS is claimed.