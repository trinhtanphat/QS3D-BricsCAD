# Work claim — template apply layer-mapping ambiguity preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-apply-layer-mapping-ambiguity-20260812-1042`
- Registered: `2026-08-12T10:42:00+07:00`
- Baseline main SHA: `1fc1a279f71c7a31e514f97ae75c11116d7f4ac7`
- Priority: owner-requested continue-all Core integrity hardening

## Confirmed defect

`TemplateProfileStore.ValidateApply(...)` projects existing project layer mappings into a case-insensitive dictionary after trimming each metadata-key suffix, then calls `ProjectRecognitionService.ValidateLayerMappings(...)`. If two distinct persisted metadata keys normalize to the same recognition pattern (for example `QS3D.LayerMapping:A-WALL` and `QS3D.LayerMapping: A-WALL `), the projection overwrites one entry before validation. The apply preflight therefore accepts project recognition state that `ProjectRecognitionService` itself rejects as an ambiguous normalized layer mapping.

## Reserved scope

- Validate the raw existing project layer-mapping metadata set before it is collapsed into the projected mapping dictionary used for template overlay validation.
- Preserve existing template-overrides-project projection semantics after the existing project state has passed recognition mapping validation.
- Add focused Core smoke coverage proving ambiguous persisted project mappings fail before template apply mutation/audit revision.

## Expected surfaces

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateApplyLayerMappingAmbiguitySmoke.cs`
- this claim file

## Excluded scope

- No `ProjectRecognitionService` runtime behavior changes.
- No `TemplateProfileStore.ExportProject` behavior changes in this claim.
- No XML/schema/text, BQ layout, category-token, family-property, UI/native, release, or unrelated active-claim changes.
- No GitHub Actions, force push, release publication, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current `main` and `TemplateProfileStore.cs` after claim registration before editing.
- Materialize raw project layer mappings, validate them with the same recognition validator, then preserve the current projected overlay validation.
- Add a module-initializer smoke that constructs duplicate-normalized persisted mapping keys and verifies apply fails without changing `ChangeVersion` or audit history; include a canonical control where useful.
- Re-fetch final source/test from current `main`, verify ancestry, then mark this claim `COMPLETED` with exact integration SHAs.

## Completion condition

Completed only when template apply cannot silently collapse ambiguous existing recognition mappings before preflight, focused regression coverage is committed, and this claim is closed on `main` with exact commit evidence.
