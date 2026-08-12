# Work claim — BBS XLSX row snapshot integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bbs-xlsx-row-snapshot-20260812-1053`
- Registered: `2026-08-12T10:53:00+07:00`
- Baseline main SHA: `971053e76f04298ccae1fe0440ffe09c2775cc2e`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`XlsxRebarScheduleExporter.Export(...)` validates every caller-owned `IReadOnlyList<RebarScheduleRow>` item in `ValidateRows(rows)` before filesystem mutation, but after destination-directory/temp-file creation it calls `BuildSheet(rows, rowCount)`, which indexes and serializes the same mutable external rows again. A changing or hostile caller can therefore serialize values that were never preflighted, or fail only after filesystem side effects have begun.

## Reserved scope

- `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
- a new focused Core smoke file for BBS XLSX row-snapshot integrity
- this claim file for close-out

## Contract

- Capture and validate the existing worksheet row bound once.
- Read each caller-owned row index exactly once before any path/directory/temp-file mutation.
- Copy all 15 worksheet-emitted `RebarScheduleRow` fields into detached rows.
- Apply the existing text-length and finite-number guards to detached rows.
- Serialize only the detached snapshot; do not re-read caller-owned rows after preflight.
- Preserve current XML sanitization, worksheet limits, package validation and atomic replacement semantics.

## Exclusions

- No Rebar schedule-builder, planner, quantity, Health, UI/command, ED2 or fabrication-policy changes.
- No edits to existing BBS regression files unless required; prefer a new focused smoke file.
- No changes to other exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Validation plan

Add a focused smoke with a caller-owned `IReadOnlyList<RebarScheduleRow>` that permits one indexed read and rejects enumeration/second reads. BBS XLSX export must succeed from the detached validated snapshot and leave the external index read count at one.

## Coordination

Historical BBS XLSX row-limit/null/text/numeric/XML lanes are completed. Current open Project Interchange and Rebar Health work are separate scopes.

## Completion condition

Source fix and new smoke source are integrated on current `main`, read back after merge, and this claim is marked `COMPLETED` with exact SHA/PR evidence and remote validation boundaries.