# Work claim — BBS XLSX row snapshot integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bbs-xlsx-row-snapshot-20260812-1053`
- Registered: `2026-08-12T10:53:00+07:00`
- Baseline main SHA: `971053e76f04298ccae1fe0440ffe09c2775cc2e`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`XlsxRebarScheduleExporter.Export(...)` validated every caller-owned `IReadOnlyList<RebarScheduleRow>` item before filesystem mutation, but later re-indexed and serialized the same mutable external rows after destination-directory/temp-file creation. A changing or hostile caller could therefore serialize values that were never preflighted, or fail only after filesystem side effects had begun.

## Reserved scope

- `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
- `tests/QS3D.Core.SmokeTests/BbsXlsxRowSnapshotSmoke.cs`
- this claim file for close-out

## Implemented contract

- Preserve and validate the existing worksheet row bound once.
- Read each caller-owned row index exactly once before any path/directory/temp-file mutation.
- Copy all 15 worksheet-emitted `RebarScheduleRow` fields into detached rows.
- Apply the existing text-length and finite-number guards to detached rows.
- Serialize only the detached snapshot; do not re-read caller-owned rows after preflight.
- Preserve current XML sanitization, worksheet limits, package validation and atomic replacement semantics.

## Exclusions

- No Rebar schedule-builder, planner, quantity, Health, UI/command, ED2 or fabrication-policy changes.
- No existing BBS regression files were edited; the regression is a new focused smoke file.
- No changes to other exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion evidence

- Claim registration: `401c353a8c4975bdd41ab60bdee3abbeb620d7ab`.
- Source branch fix: `c267a8e94351c61418e2957f9d5796357790a3c1`.
- Focused smoke source: `3ed57dda435a079bb4e2fc1afe6af9b7e1de758e`.
- PR: `#787`.
- Squash integration on `main`: `c43285b4da7dc199b23fbfc8bffb40dcdd6bab2b`.
- Post-merge readback confirmed `main` constructs `snapshot = SnapshotRows(rows)` before path/directory/temp-file work and passes only the detached snapshot into `BuildSheet`.
- Post-merge readback confirmed `BbsXlsxRowSnapshotSmoke` rejects any second caller-row indexed read or caller-list enumeration.

## Validation boundary

Focused smoke coverage was added and read back from `main`, but it was not executed in this remote session. No GitHub Actions, local .NET build, BricsCAD V25/V26 runtime, release or signing PASS is claimed.