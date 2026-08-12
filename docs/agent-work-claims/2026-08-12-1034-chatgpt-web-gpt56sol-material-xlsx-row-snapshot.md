# Work claim — Material XLSX row snapshot integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-xlsx-row-snapshot-20260812-1034`
- Registered: `2026-08-12T10:34:00+07:00`
- Baseline main SHA: `e7c5e5fbb5b6cccfeff910b0e94a867ed556a177`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`MaterialUsageXlsxExporter.Export(...)` validates the caller-owned `IReadOnlyList<MaterialUsageRow>` before filesystem mutation, but later `BuildSheet(rows)` re-reads the same external list and mutable row objects after destination-directory/temp-file creation. A list/indexer or row mutated between those phases can therefore serialize data that was never preflighted, or throw only after filesystem side effects have started.

## Reserved scope

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- focused Core smoke coverage for stable row snapshot behavior
- this claim file for close-out

## Contract

- Capture the bounded row count once.
- Materialize detached `MaterialUsageRow` values for every worksheet field before any path/directory/temp-file mutation.
- Validate the detached snapshot and serialize only that snapshot.
- Do not re-read caller-owned rows/indexers after preflight.
- Preserve the existing Excel row/text limits, numeric finite checks, XML escaping, atomic replace and workbook format.

## Exclusions

- No UI/command changes, schedule-builder changes, quantity semantics or XLSX schema changes.
- No changes to other exporters in this claim.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim.

## Validation plan

Add a focused smoke using a caller-owned `IReadOnlyList<MaterialUsageRow>` that permits only one indexed read. Export must succeed from the validated detached snapshot, proving the serializer does not access the external list again after preflight. Existing invalid-cell/finite preflight behavior remains source-preserved.

## Completion condition

The snapshot fix and focused regression source are integrated on current `main`, read back after merge, and this claim is marked `COMPLETED` with exact SHA/PR evidence and remote validation boundaries.