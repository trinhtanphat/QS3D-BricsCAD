# Work claim — QSDB save-size preflight

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:57:00+07:00`
- Baseline main SHA: `9ea748b2fde921248287e0eeaae3e86aca1beb3b`
- Priority: evidence-driven Core QSDB persistence filesystem atomicity

## Confirmed defect

`QsdbProjectStore` enforces a hard 64 MiB load limit and validates the written temp file through that same bounded loader. On save, however, `SaveCore(...)` currently validates semantic/XML content, resolves the destination, creates the destination directory/temp path, mutates the in-memory persistence stamp (`SchemaVersion` / `Touch()`), serializes and writes the whole temp file, and only then discovers that an oversized serialized QSDB cannot be loaded.

The failed save rolls the project persistence stamp back, but an output that is guaranteed to exceed the supported 64 MiB contract can still create the destination directory and temp file before failing. The completed read-side `qsdb stream size bound` lane guards parsing; this lane is the distinct write-side preflight.

## Intended scope

- preserve destination-path validation before any project persistence-stamp mutation;
- preserve existing project/XML semantic validation before `Touch()`;
- after establishing the exact post-`Touch()` document that would be written, bounded-count the same `XDocument.Save(Stream, SaveOptions.DisableFormatting)` byte stream against the existing 64 MiB limit before destination-directory/temp-file mutation;
- use the already-preflighted document for the actual temp write;
- on preflight failure, restore `SchemaVersion`, `UpdatedUtc` and `ChangeVersion` exactly as current failed-save semantics require;
- preserve Save / SaveNew / SavePreservingValidatedBackup publication, backup and post-write validation semantics;
- add focused Core smoke coverage with a small private test limit so no 64+ MiB fixture is allocated.

## Reserved surfaces

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `tests/QS3D.Core.SmokeTests/QsdbSaveSizePreflightSmoke.cs`
- this claim file

## Excluded scope

Do not modify the completed QSDB read-stream size bound, schema/canonicality/relation/reference policy, save lifecycle coordination outside this store, backup fallback semantics, Project Interchange/Revision stores, CAD/UI adapters, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim executable .NET smoke/build or BricsCAD V25/V26 runtime PASS without actual execution.
