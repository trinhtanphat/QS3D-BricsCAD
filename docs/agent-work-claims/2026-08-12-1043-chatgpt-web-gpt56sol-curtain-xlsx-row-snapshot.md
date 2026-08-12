# Work claim — Curtain XLSX row snapshot integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-xlsx-row-snapshot-20260812-1043`
- Registered: `2026-08-12T10:43:00+07:00`
- Baseline main SHA: `ef760d184956ef2a1aa178403f2bd6cb0a8823f7`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`CurtainWallXlsxExporter.Export(...)` validates caller-owned `IReadOnlyList<CurtainWallScheduleRow>` values before filesystem mutation, but `BuildSheet(rows)` later re-reads the same mutable external rows after destination-directory/temp-file creation. A changing or hostile caller can therefore serialize data that was never validated or fail only after side effects have begun.

## Reserved scope

- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- focused Core smoke coverage for stable row snapshot behavior
- this claim file for close-out

## Contract

- Capture row count once and enforce the existing Excel worksheet bound.
- Read each caller-owned row index once and copy every worksheet field to a detached row before any path/directory/temp-file mutation.
- Validate and serialize only that detached snapshot.
- Preserve current cell-text, finite-number, workbook/package and atomic replace behavior.

## Exclusions

- No Curtain geometry/planning/materialization/Health changes.
- No schedule-builder or UI/command changes.
- No changes to other exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Validation plan

Add focused smoke coverage using a caller-owned row list that allows one indexed access and rejects enumeration/second reads. Export must succeed from the detached snapshot.

## Completion condition

The source fix and smoke source are integrated on current `main`, read back after merge, and this claim is marked `COMPLETED` with exact SHA/PR evidence and remote validation boundaries.