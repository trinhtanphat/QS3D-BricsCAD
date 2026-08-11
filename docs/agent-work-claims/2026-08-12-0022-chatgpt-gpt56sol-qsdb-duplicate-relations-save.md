# Work claim — QSDB duplicate relation/source save guard

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-qsdb-duplicate-relations-save`
- Registered: `2026-08-12T00:22:51+07:00`
- Last Updated: `2026-08-12T00:22:51+07:00`
- Baseline main SHA: `7d97d9281f3b28f57ce6beabcb0a317e5afa0a73`
- Priority: writer/reader contract mismatch found during owner-requested continue-all audit
- Task Key: `PERSISTENCE-QSDB-DUPLICATE-RELATION-SAVE`

## Confirmed defect

Current QSDB read validation rejects same-element duplicate source handles and dependency ids case-insensitively, but `QsdbProjectStore.ValidateProject(...)` only validates that in-memory `SourceHandles` and `DependsOn` values are nonblank and trim-canonical. `ProjectElement` exposes both collections as mutable lists, so duplicate canonical values can be inserted directly.

`SaveCore(...)` then serializes those duplicate lists and its post-serialization check only validates the basic root/schema/project identity envelope. A project can therefore be accepted by `Save(...)` and publish a current QSDB file that the current repo's own `Load(...)` rejects. That violates the persistence roundtrip/fail-closed contract.

## Reserved scope

Reject same-element duplicate `SourceHandles` and `DependsOn` values case-insensitively during `QsdbProjectStore` save preflight, before temp/publication work. Preserve order and values of unique canonical entries. The same textual value remains allowed on different elements where the read schema permits it.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs`
- this claim file

## Explicit exclusions / coordination

- Do **not** modify `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`; persisted relation/source read canonicality and duplicate-read work own that surface and already establish the reader contract.
- Do not modify `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs`; the separate Save/SaveAs identity-preflight claim owns that surface and explicitly excludes QSDB serialization internals.
- No dependency graph algorithm, generated-handle ownership, source reconcile, schema migration/version, quantity rules/UI/reporting/interchange changes.
- No BricsCAD runtime mutation and no new LOCAL_ONLY gate.
- No GitHub Actions/build/release dispatch.

## Validation plan

- Exact duplicate source handles fail `Save(...)` before any QSDB file is created.
- Case-only duplicate source handles fail the same way.
- Exact duplicate dependency ids fail before publication.
- Case-only duplicate dependency ids fail before publication.
- Existing nonblank/trim-canonical checks remain intact.
- Unique source/dependency lists continue to serialize and remain loadable.
- Re-fetch exact current source/test after claim publication, inspect final PR diff, and read back the merge commit/source. Do not claim local build/smoke execution unless actually run.

## Completion condition

Current `main` cannot publish same-element duplicate source/dependency lists that its own reader rejects, focused deterministic regression source is present, and this claim is closed `COMPLETED` with exact implementation evidence.
