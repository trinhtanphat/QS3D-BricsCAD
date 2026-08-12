# Work claim — Semantic schedule upsert canonical id

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-semantic-schedule-upsert-canonical-id`
- Registered: `2026-08-12T13:46:00+07:00`
- Baseline main SHA: `6db6f88ff0b5daa516aaf4e6f39d1ccac1d4c521`
- Claim commit: `11c267129c5ee75bfbb686e63f0e2fb36d99658f`
- Source fix: `2db9f9370fd08e9552b99f9fe57074e32e1fee5e`
- Regression smoke: `9f70598a5683394e55dcd245a23a567870aec024`
- Priority: P1 — canonical-equivalent schedule ids could fail replacement and become duplicate-id save errors.

## Confirmed defect

`SemanticScheduleCatalog.Upsert()` looked up an existing schedule with raw `definition.Id`, while `Remove()`, validation, and serialization use the trimmed/canonical schedule id. An incoming replacement such as `" schedule-1 "` therefore missed existing `"schedule-1"`, was appended, and later failed catalog validation as a duplicate instead of replacing the existing definition.

## Completed change

- `Upsert()` still loads the project/catalog before incoming-id validation, preserving existing project/catalog error precedence.
- The incoming schedule id is then normalized through the same `Required(..., "schedule id", 80)` boundary used by catalog validation/removal.
- Existing ids are compared case-insensitively against that normalized id before replacement/addition.
- No other schedule validation, serialization, build, or catalog semantics were changed.

## Regression coverage

`SemanticScheduleCatalogUpsertCanonicalIdSmoke` exercises the production `Save -> Upsert -> Load` path:

- seeds canonical `schedule-1`;
- upserts `" SCHEDULE-1 "` with a different payload;
- asserts exactly one schedule remains;
- asserts the replacement payload wins;
- asserts the persisted id is trimmed/canonical after round-trip.

The smoke project uses the SDK default compile glob and the test is registered with `[ModuleInitializer]`, so no separate registration file is required.

## Readback verification

Readback on `main` after regression commit confirmed `Upsert()` contains `normalizedId = Required(definition.Id, "schedule id", 80)` and compares existing ids against `normalizedId`. Comparing source fix `2db9f9370fd08e9552b99f9fe57074e32e1fee5e` to regression HEAD `9f70598a5683394e55dcd245a23a567870aec024` returned `behind_by: 0`, confirming the source fix remains in ancestry.

## Validation boundary

No GitHub Actions, local build/smoke execution, or BricsCAD runtime PASS is claimed. Verification in this lane is source/test readback plus Git ancestry only.
