# Work claim — Current QSDB changeVersion regression

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-current-schema-changeversion-regression-20260812-0846`
- Registered: `2026-08-12T08:46:00+07:00`
- Completed: `2026-08-12T08:48:00+07:00`
- Baseline main SHA: `7b16210497167156599a3e4f9080817511054182`
- Priority: P1 — current-schema persistence must fail closed on missing required state.

## Confirmed regression

`ProjectSchemaMigrator.MigrateToCurrent(...)` synthesized `changeVersion="0"` after the migration loop for every document, including a document already declaring current schema 3. That masked a malformed current QSDB missing the required `changeVersion` attribute before `ValidateCurrentPersistenceState(...)` could reject it.

This behavior regressed earlier repository fix `a10060088aad60e46c9e8ed812e7ca0eef15d042` (`fix(core): require current QSDB change version`), which materialized version `0` only during legacy v2→v3 migration and rejected a missing `changeVersion` on schema-3 input.

Current smoke coverage had also drifted from `RejectsMissingCurrentChangeVersion()` to only `RejectsBlankCurrentChangeVersion()`, allowing the missing-attribute regression to return.

## Implemented scope

- `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbTimestampValidationSmoke.cs`
- this claim file for close-out

## Completed contract

- Current-schema QSDB no longer receives a synthesized `changeVersion`; the existing required-value validation now rejects a missing attribute.
- Legacy v1/v2 files still materialize `changeVersion="0"` specifically during `MigrateV2ToV3(...)`.
- Existing blank-current coverage is preserved.
- Missing-current coverage is restored alongside existing legacy migration smoke paths.
- Unrelated persistence format behavior was left unchanged.

## Validation evidence

- Claim registration: `4601218af86e01f1909cc7bf688bc87315e59e88`.
- Source restoration: `7af2f65e1bd3ed717384d676e2510655d87bd60c`.
- Regression smoke restoration: `fd04b20dcf71812673512ef0e0036df5884866b8`.
- Post-write source readback blob: `d6e67d5c7ac7dcc755e2dea67cc7d315a7dd601a`.
- Post-write smoke readback blob: `54f59773076919c8cc3c812fa5b7baca3ddfc494`.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: the current-schema regression is restored on `main`, source and smoke were re-read after integration, and exact commit evidence is recorded above.
