# Work claim — QSDB current-schema changeVersion contract sync

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T13:57:10+07:00`
- Baseline main SHA: `a4abd6deb170c4332db72f659814b9852a6f764c`
- Priority: `P0 deterministic smoke/static regression — current schema-3 changeVersion semantics are contradictory across registered tests and schema preflight`

## Reserved scope

Align the registered QSDB save smoke and schema preflight with the current strict schema-3 persistence contract already enforced by `ProjectSchemaMigrator` and `QsdbTimestampValidationSmoke`: missing `changeVersion` on current schema 3 is invalid; legacy schema migration remains responsible for synthesizing required persistence state.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/QsdbSaveAtomicitySmoke.cs`
- `scripts/preflight-qsdb-schema.py`
- Read-only verification of `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- Read-only verification of `tests/QS3D.Core.SmokeTests/QsdbTimestampValidationSmoke.cs`
- Read-only verification of `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`

## Excluded scope

- Production parser/migrator behavior changes.
- Timestamp-offset preflight (completed separately), project-id, relation-token, active-context, save-size, XML text, or other Persistence lanes.
- LOCAL-003 runtime/fixture work beyond reading the already-landed strict contract.
- GitHub Actions dispatch, BricsCAD runtime qualification, packaging/release.

## Evidence / intended correction

- `ProjectSchemaMigrator.ValidateCurrentPersistenceState` requires nonblank `changeVersion` after version migration.
- `QsdbTimestampValidationSmoke` explicitly registers `RejectsMissingCurrentChangeVersion()` and `RejectsBlankCurrentChangeVersion()`.
- `QsdbSaveAtomicitySmoke` is registered earlier in `SmokeTestRegistration.RunAll()` but still calls `LegacyFileDefaultsChangeVersion()`, removes `changeVersion` from a real schema-3 file, and expects load success/default zero. That contradicts the current strict schema-3 contract.
- `scripts/preflight-qsdb-schema.py` still requires the removed same-schema backfill and the stale save-smoke compatibility tokens.

## Validation plan

- Replace the contradictory save smoke case with a missing-current-changeVersion rejection regression while preserving successful round-trip and invalid-value coverage.
- Update schema preflight to require strict current-schema rejection plus legacy v1 migration coverage rather than same-schema missing-field compatibility.
- Read back exact diffs and confirm no production source changed.
- Do not claim executable full smoke/preflight/build/Actions/runtime PASS unless actually executed.

## Completion condition

Pushed `main` commits make the registered test surfaces and schema preflight agree with the already-enforced current schema-3 contract, followed by this claim marked `COMPLETED` with exact SHAs.
