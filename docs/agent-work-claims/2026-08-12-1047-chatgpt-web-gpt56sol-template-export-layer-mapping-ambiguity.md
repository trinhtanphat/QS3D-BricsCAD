# Work claim — template export layer-mapping ambiguity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-export-layer-mapping-ambiguity-20260812-1047`
- Registered: `2026-08-12T10:47:00+07:00`
- Baseline main SHA: `2f981b82ea778b0ed1351c959f0616d728b05602`
- Priority: owner-requested continue-all Core integrity hardening

## Confirmed defect

`TemplateProfileStore.ExportProject(...)` currently trims each `QS3D.LayerMapping:` metadata-key suffix and assigns it directly into the case-insensitive `TemplateProfile.LayerMappings` dictionary. Distinct persisted project mappings that normalize to the same recognition pattern can therefore overwrite each other during export, producing an apparently valid template that silently omits an ambiguity which `ProjectRecognitionService` rejects at runtime.

## Reserved scope

- Validate the raw project layer-mapping metadata set before export projection can trim/collapse duplicate-normalized patterns.
- Preserve canonical export ordering and existing exported values for valid project mappings.
- Add focused Core smoke coverage proving ambiguous project recognition mappings cannot be silently exported.

## Expected surfaces

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateExportLayerMappingAmbiguitySmoke.cs`
- this claim file

## Excluded scope

- No `ProjectRecognitionService` runtime behavior changes.
- No template apply behavior changes beyond the already-completed sibling claim `2026-08-12-1042-chatgpt-web-gpt56sol-template-apply-layer-mapping-ambiguity.md`.
- No XML/schema/text, BQ layout, category-token, family-property, UI/native, release, or unrelated active-claim changes.
- No GitHub Actions, force push, release publication, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current `main` and `TemplateProfileStore.cs` after claim registration before editing.
- Materialize raw project layer mappings in export, run `ProjectRecognitionService.ValidateLayerMappings(...)`, then copy validated mappings into the profile using the existing canonical trim/order behavior.
- Add a module-initializer smoke that constructs duplicate-normalized persisted mappings and verifies `ExportProject` fails closed; include a canonical mapping export control.
- Re-fetch final source/test from current `main`, verify ancestry, then mark this claim `COMPLETED` with exact integration SHAs.

## Completion condition

Completed only when template export no longer hides ambiguous persisted recognition mappings through dictionary overwrite, focused regression coverage is committed, and this claim is closed on `main` with exact commit evidence.
