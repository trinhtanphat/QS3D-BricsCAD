# Work claim — Revision parsed-stream size bound

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:12:00+07:00`
- Baseline main SHA: `c300c2db59663b11961fa1b49418d504e763aa58`
- Priority: evidence-driven Core revision input/resource integrity

## Confirmed defect

`RevisionSnapshotStore.LoadDocument(...)` enforces the 64 MiB revision limit through `FileInfo.Length`, then opens and parses a separate `FileStream`. The file can be replaced or grow between those operations, so the byte-size decision is not bound to the exact stream consumed by `XmlReader`. `MaxCharactersInDocument` remains a character limit and is not a substitute for the persisted byte contract.

This is distinct from the completed Revision save-size preflight (#771), which protects write-side filesystem atomicity. It mirrors the completed QSDB and License parsed-stream size patterns.

## Intended scope

- retain full-path resolution and existing missing-file/parse behavior;
- check `stream.Length` on the exact `FileStream` that will be passed to `XmlReader` before parsing;
- preserve the existing 64 MiB diagnostic and XML security/schema/canonicality/load-with-backup behavior;
- use a private bounded-load overload for focused smoke coverage with a small test limit, avoiding a 64+ MiB fixture;
- leave save-size preflight and backup publication unchanged.

## Reserved surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs`
- `tests/QS3D.Core.SmokeTests/RevisionParsedStreamSizeSmoke.cs`
- this claim file

## Excluded scope

Do not modify revision save-size preflight, numeric/XML canonicality, snapshot schema/compare/review semantics, QSDB/License/Template stores, CAD/UI adapters, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim executable .NET smoke/build or BricsCAD V25/V26 runtime PASS without actual execution.
