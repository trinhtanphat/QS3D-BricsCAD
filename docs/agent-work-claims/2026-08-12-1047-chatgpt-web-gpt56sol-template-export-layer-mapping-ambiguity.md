# Work claim — template export layer-mapping ambiguity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-export-layer-mapping-ambiguity-20260812-1047`
- Registered: `2026-08-12T10:47:00+07:00`
- Completed: `2026-08-12T10:50:00+07:00`
- Baseline main SHA: `2f981b82ea778b0ed1351c959f0616d728b05602`
- Priority: owner-requested continue-all Core integrity hardening

## Confirmed defect

`TemplateProfileStore.ExportProject(...)` trimmed each `QS3D.LayerMapping:` metadata-key suffix and assigned it directly into the case-insensitive `TemplateProfile.LayerMappings` dictionary. Distinct persisted project mappings that normalized to the same recognition pattern could therefore overwrite each other during export, producing an apparently valid template that silently omitted an ambiguity which `ProjectRecognitionService` rejects at runtime.

## Implemented scope

- Materialize raw project layer-mapping metadata before export projection.
- Validate the raw set through `ProjectRecognitionService.ValidateLayerMappings(...)` before trim/copy can collapse duplicate-normalized patterns.
- Preserve the existing canonical trim/order copy behavior for valid mappings.
- Add focused module-initializer smoke coverage for ambiguity fail-closed plus a canonical mapping export control.

## Integration evidence

- Claim registration: `f9eb0b91b56b8535862bc2d7f5e57564fcd7c65d`
- Product fix: `5f3a936178885872b64a2111d77a03541977acda`
- Regression: `2eb3b2942420d9433573fc7af141f2e703174dff`
- Verified current-main descendant before close: `c2f7f6aacfd35d4e51441e9d9ba1b38e8227cd22`
- `compare_commits(2eb3b29..., c2f7f6a...)` reported current main ahead by 1 and behind by 0; the only later file change was an unrelated room-finish XLSX claim close-out.

## Validation

- Re-fetched `TemplateProfileStore.cs` from current `main` and confirmed export validates raw `layerMappings` before populating `profile.LayerMappings`.
- Re-fetched `TemplateExportLayerMappingAmbiguitySmoke.cs` from current `main`; it asserts ambiguous mappings fail closed/read-only and canonical `A-WALL` still exports unchanged.
- No GitHub Actions dispatched.
- No force push, release publication, or BricsCAD runtime PASS claim.

## Excluded scope

- No `ProjectRecognitionService` runtime behavior changes.
- No template apply behavior changes beyond the already-completed sibling claim `2026-08-12-1042-chatgpt-web-gpt56sol-template-apply-layer-mapping-ambiguity.md`.
- No XML/schema/text, BQ layout, category-token, family-property, UI/native, release, or unrelated active-claim changes.

## Completion condition

Completed: template export no longer hides ambiguous persisted recognition mappings through dictionary overwrite, focused regression coverage is committed, and the lane is closed on `main` with exact commit evidence.
