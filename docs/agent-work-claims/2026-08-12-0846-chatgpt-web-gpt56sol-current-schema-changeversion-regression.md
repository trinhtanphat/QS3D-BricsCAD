# Work claim — Current QSDB changeVersion regression

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-current-schema-changeversion-regression-20260812-0846`
- Registered: `2026-08-12T08:46:00+07:00`
- Baseline main SHA: `7b16210497167156599a3e4f9080817511054182`
- Priority: P1 — current-schema persistence must fail closed on missing required state.

## Confirmed regression

`ProjectSchemaMigrator.MigrateToCurrent(...)` currently synthesizes `changeVersion="0"` after the migration loop for every document, including a document already declaring current schema 3. That masks a malformed current QSDB missing the required `changeVersion` attribute before `ValidateCurrentPersistenceState(...)` can reject it.

This behavior regressed an earlier repository fix, `a10060088aad60e46c9e8ed812e7ca0eef15d042` (`fix(core): require current QSDB change version`), which materialized version `0` only during the legacy v2→v3 migration and rejected a missing `changeVersion` on schema-3 input.

Current smoke coverage has also drifted from `RejectsMissingCurrentChangeVersion()` to only `RejectsBlankCurrentChangeVersion()`, allowing the missing-attribute regression to return.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbTimestampValidationSmoke.cs`
- this claim file for close-out

## Contract

- A QSDB that already declares the current schema must include a nonblank canonical `changeVersion`; absence fails closed with `InvalidDataException`.
- Legacy v1/v2 QSDB files without `changeVersion` remain backward-compatible by materializing version `0` during migration to v3.
- Preserve existing blank, malformed, negative/overflow, timestamp, section-shape and numeric-state validation.
- Keep the patch minimal; do not alter unrelated persistence format behavior.
- Add/restore deterministic Core smoke coverage for both missing-current rejection and legacy migration compatibility.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim from this remote lane.

## Completion condition

The regression is restored on current `main`, source and smoke are re-read after integration, exact commit evidence is recorded, and this claim is marked `COMPLETED`.
