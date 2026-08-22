# Work claim — QSDB duplicate relation/source save guard

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-qsdb-duplicate-relations-save`
- Registered: `2026-08-12T00:22:51+07:00`
- Last Updated: `2026-08-12T00:28:30+07:00`
- Baseline main SHA: `7d97d9281f3b28f57ce6beabcb0a317e5afa0a73`
- Priority: writer/reader contract mismatch found during owner-requested continue-all audit
- Task Key: `PERSISTENCE-QSDB-DUPLICATE-RELATION-SAVE`
- Implementation PR: `#571`
- Implementation commit on `main`: `798de2b7f2888e1ade9be2156420a33a1e2a428b`

## Confirmed defect

Current QSDB read validation rejected same-element duplicate source handles and dependency ids case-insensitively, but `QsdbProjectStore.ValidateProject(...)` only validated that in-memory `SourceHandles` and `DependsOn` values were nonblank and trim-canonical. `ProjectElement` exposes both collections as mutable lists, so duplicate canonical values could be inserted directly.

`SaveCore(...)` could therefore serialize duplicate lists and publish a current QSDB file that the current repo's own `Load(...)` rejected. That violated the persistence roundtrip/fail-closed contract.

## Implemented scope

`ValidateCanonicalStringList(...)` now maintains a `HashSet<string>` using `StringComparer.OrdinalIgnoreCase` after the existing nonblank/trim-canonical checks and rejects a duplicate before serialization/temp-file publication begins. The same helper covers both per-element source handles and dependencies.

Focused existing smoke coverage now exercises:

- exact duplicate source handles;
- case-only duplicate source handles;
- exact duplicate dependency ids;
- case-only duplicate dependency ids;
- existing padded/blank relation validation remains in the same smoke.

The existing `RejectSave(...)` helper also asserts that rejected preflight creates no QSDB file.

## Surfaces changed

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs`
- this claim file

## Coordination / exclusions preserved

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs` was not modified; reader-side relation/source claims retained that surface.
- `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs` was not modified; its separate Save/SaveAs identity-preflight claim remained isolated.
- No dependency graph, generated-handle ownership, source reconcile, migration/version, quantity/reporting/interchange, BricsCAD runtime or LOCAL_ONLY surface changed.
- No GitHub Actions/build/release workflow was dispatched.

## Validation evidence

- Claim was published on `main` before source edits at commit `7998e3b619e797f94dbc707439786606a6b9037b`.
- Re-fetched current source and smoke after claim publication; target blobs were `aaff9c017421a6f017328aafe6335980eecc5dd6` and `9c932b43686123c323fcbe5c228a3b6aca6ced6f`.
- PR `#571` diff was reviewed before merge and contained exactly two implementation files with `+27/-0`.
- Server-side squash merge produced `798de2b7f2888e1ade9be2156420a33a1e2a428b`.
- Read-back of that merge commit confirms only the duplicate-list guard and four focused regression cases were added.
- Local build/smoke execution is **not** claimed because this connector-only environment does not provide the project checkout/build runner.

## Completion

`COMPLETED`: current `main` can no longer publish same-element duplicate source/dependency lists that its own reader rejects, and focused regression source covers exact and case-only duplicates before publication.
