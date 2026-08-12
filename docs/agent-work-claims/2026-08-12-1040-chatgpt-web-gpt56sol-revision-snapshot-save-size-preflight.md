# Work claim — Revision snapshot save-size preflight

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:40:00+07:00`
- Baseline main SHA: `8583e783369fa87bf76551d3edbc31abe8c82ce1`
- Priority: evidence-driven Core persistence preflight / filesystem atomicity

## Confirmed defect

`RevisionSnapshotStore` declares a hard 64 MiB supported revision-file limit and `Load(...)` rejects a file whose byte length exceeds that limit. `Save(...)` validates semantic/XML content first, but it does not validate the serialized byte length until after it has resolved the destination, created the destination directory, created/written a temp file, and then calls `ValidateSerializedFile(temp)` / `Load(temp)`.

A snapshot whose serialized representation is guaranteed to be unsupported can therefore mutate the filesystem before the save fails. This is inconsistent with the store's existing validate-before-write pattern and with other persistence/export preflight boundaries in Core.

## Intended scope

- serialize the validated revision snapshot once before filesystem mutation;
- measure the exact UTF-8/XML persisted byte representation against the existing 64 MiB limit before `Path.GetFullPath`, directory creation, or temp-file creation;
- write that already-preflighted document to the temp file and retain the existing post-write `Load(temp)` validation, backup-preservation and atomic replace semantics;
- reject oversized snapshots with `InvalidDataException` using the existing 64 MiB contract;
- add focused Core smoke coverage proving oversized output fails before destination directory mutation and a normal snapshot still round-trips.

## Reserved surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs`
- `tests/QS3D.Core.SmokeTests/RevisionSnapshotSaveSizePreflightSmoke.cs`
- this claim file

## Excluded scope

Do not modify revision numeric/XML canonicality, snapshot schema, backup fallback/preservation semantics, revision compare/review behavior, QSDB persistence, UI/CAD adapters, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim executable .NET smoke/build or BricsCAD V25/V26 runtime PASS without actual execution.
