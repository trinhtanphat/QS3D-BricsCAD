# Work claim — template apply layer-mapping ambiguity preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-apply-layer-mapping-ambiguity-20260812-1042`
- Registered: `2026-08-12T10:42:00+07:00`
- Completed: `2026-08-12T10:46:00+07:00`
- Baseline main SHA: `1fc1a279f71c7a31e514f97ae75c11116d7f4ac7`
- Priority: owner-requested continue-all Core integrity hardening

## Confirmed defect

`TemplateProfileStore.ValidateApply(...)` projected existing project layer mappings into a case-insensitive dictionary after trimming each metadata-key suffix, then called `ProjectRecognitionService.ValidateLayerMappings(...)`. If two distinct persisted metadata keys normalized to the same recognition pattern (for example `QS3D.LayerMapping:A-WALL` and `QS3D.LayerMapping: A-WALL `), the projection overwrote one entry before validation. The apply preflight therefore accepted project recognition state that `ProjectRecognitionService` itself rejects as an ambiguous normalized layer mapping.

## Implemented scope

- Materialize the raw existing project layer-mapping metadata set before projection.
- Validate that raw set with `ProjectRecognitionService.ValidateLayerMappings(...)` before any trim/dictionary collapse can hide ambiguity.
- Preserve the existing template-overrides-project projected mapping validation after the existing project state passes recognition validation.
- Add focused module-initializer smoke coverage proving duplicate-normalized persisted mappings fail before project revision, audit history, or metadata mutation.

## Integration evidence

- Claim registration: `0e87fd9a5848c5b818dbda27529fa132da813361`
- Product fix: `26d9739684a1fd246a99a753cc56368d72891ef9`
- Regression: `9bb555764a1d8096350c61da9bd69746c218ec3c`
- Verified current-main descendant before close: `97c74699d7f47d7bac8aa2c51c40ae07023f4c8a`
- `compare_commits(9bb5557..., 97c7469...)` reported current main ahead by 5 and behind by 0, with neither reserved source nor regression file modified after the regression commit.

## Validation

- Re-fetched `TemplateProfileStore.cs` from `main` and confirmed raw `projectMappings` are validated before `projectedMappings` is built.
- Re-fetched `TemplateApplyLayerMappingAmbiguitySmoke.cs` from `main`; it asserts the ambiguity exception and unchanged `ChangeVersion`, `AuditEvents`, and metadata.
- No GitHub Actions dispatched.
- No force push, release publication, or BricsCAD runtime PASS claim.

## Excluded scope

- No `ProjectRecognitionService` runtime behavior changes.
- No `TemplateProfileStore.ExportProject` behavior changes in this claim.
- No XML/schema/text, BQ layout, category-token, family-property, UI/native, release, or unrelated active-claim changes.

## Completion condition

Completed: template apply can no longer silently collapse ambiguous existing recognition mappings before preflight, focused regression coverage is committed, and the lane is closed on `main` with exact commit evidence.
