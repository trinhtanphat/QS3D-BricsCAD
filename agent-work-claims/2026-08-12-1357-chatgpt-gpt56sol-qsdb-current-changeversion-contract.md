# Work claim — QSDB current-schema changeVersion contract sync

- Status: `COMPLETED`
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
- `QsdbSaveAtomicitySmoke` was registered earlier in `SmokeTestRegistration.RunAll()` but still called `LegacyFileDefaultsChangeVersion()`, removed `changeVersion` from a real schema-3 file, and expected load success/default zero. That contradicted the current strict schema-3 contract.
- `scripts/preflight-qsdb-schema.py` still required the removed same-schema backfill and stale save-smoke compatibility tokens.

## Validation plan

- Replace the contradictory save smoke case with a missing-current-changeVersion rejection regression while preserving successful round-trip and invalid-value coverage.
- Update schema preflight to require strict current-schema rejection plus legacy v1 migration coverage rather than same-schema missing-field compatibility.
- Read back exact diffs and confirm no production source changed.
- Do not claim executable full smoke/preflight/build/Actions/runtime PASS unless actually executed.

## Completion

- Smoke correction: `c9b6b52fb64ca332dd828815716e94eefb3a75ed` (`test(persistence): reject missing current changeVersion`).
- Schema preflight correction: `e36c5a6d5bd9997388294e7b6fea426448ac5be3` (`test(preflight): align QSDB current schema strictness`).
- Readback confirms the save smoke now removes `changeVersion` from a real current schema-3 file and requires `InvalidDataException`; successful round-trip and malformed-value coverage remain intact.
- Readback confirms the preflight guards strict current-schema validation and scopes `changeVersion=0` synthesis to `MigrateV2ToV3`, while retaining legacy-v1 migration coverage.
- Production `ProjectSchemaMigrator.cs`, `QsdbProjectStore.cs`, and timestamp validation source were not changed.
- Two attempted atomic ref updates were rejected as non-fast-forward because `main` advanced concurrently; no force-push occurred. The final two file writes used exact existing blob SHA guards and landed safely on current `main`.
- No GitHub Actions were dispatched. No executable full smoke/preflight/build or BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied by pushed commits `c9b6b52fb64ca332dd828815716e94eefb3a75ed` and `e36c5a6d5bd9997388294e7b6fea426448ac5be3`, followed by this completion record on `main`.
