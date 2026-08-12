# Work claim — Door/Opening XLSX row snapshot integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-door-opening-xlsx-row-snapshot-20260812-1039`
- Registered: `2026-08-12T10:39:00+07:00`
- Baseline main SHA: `3aa33193e8cf5a6141a795c074ef222cd64a0854`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`DoorOpeningXlsxExporter.Export(...)` preflights a caller-owned `IReadOnlyList<DoorOpeningScheduleRow>` through multiple passes, then re-reads the same external rows and mutable `ElementIds` / `HostIds` after destination-directory/temp-file creation in `BuildSheet(rows)`. A mutating or hostile list/row can therefore serialize data that was not preflighted, or fail only after filesystem side effects have begun.

## Reserved scope

- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- focused Core smoke coverage for stable row snapshot behavior
- this claim file for close-out

## Contract

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

## Validation plan

Add focused smoke coverage with a caller-owned `IReadOnlyList<DoorOpeningScheduleRow>` that permits exactly one indexed row read and rejects enumeration. Export must succeed from the detached validated snapshot. Existing cell-text/numeric guards remain source-preserved.

## Coordination

Current concurrent physical-opening host freshness and Health handle-canonicality lanes are separate scopes. Historical Door/Opening XLSX cell-text and finite-number hardening is preserved rather than reimplemented.

## Completion condition

The detached row/list snapshot fix and focused regression source are integrated on current `main`, read back after merge, and this claim is updated to `COMPLETED` with exact SHA/PR evidence and remote validation boundaries.