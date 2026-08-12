# Work claim — Template family property key canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-family-property-key-canonicality-20260812-0931`
- Registered: `2026-08-12T09:31:00+07:00`
- Completed: `2026-08-12T09:35:00+07:00`
- Baseline main SHA: `4721cc060f242edc67e4d2ec14cb2981ce8e6f60`
- Claim commit: `1ac4bd102309cc91bfa1fe2e4adeea390599503e`
- Source fix commit: `9a5d2e1bce33a2cbc251fd12577098d9dc4ae449`
- Regression commit: `f1f8e8e2e647db4d67ea9da7703e3cbc289ec98f`

## Completed scope

`TemplateProfileStore.Validate(profile)` now fails closed when any programmatic Template Family property key is blank or contains leading/trailing whitespace. Because validation runs before `ValidateApply`, rollback capture and project mutation, malformed profiles can no longer leave a project with Family property keys that `QsdbProjectStore.ValidateProject` would reject at persistence time.

## Implemented surfaces

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateFamilyPropertyKeyCanonicalitySmoke.cs`
- this claim file

## Regression coverage

The focused ModuleInitializer smoke requires blank and padded Family property keys to throw `InvalidDataException` while preserving target Family count, audit count, `ChangeVersion` and `UpdatedUtc`. A canonical `WidthM` key must still apply successfully with the original value.

## Validation actually performed

- Re-read the integrated `TemplateProfileStore` from current `main`; family property-key validation occurs inside `Validate(profile)` before any Apply planning or mutation.
- Re-read the focused regression source from current `main` and confirmed blank/padded/no-mutation plus canonical-success coverage.
- Verified regression commit `f1f8e8e2e647db4d67ea9da7703e3cbc289ec98f` is an ancestor of main snapshot `309ae8e7d42f877014f6443e3d6cf41acf763bce` with `behind_by: 0`; the one intervening commit touched only `src/QS3D.Core/Domain/WallPropertySet.cs`.
- No GitHub Actions were dispatched. No local .NET build/smoke execution or BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope honored

Family property values, canonical keys, persisted XML structure/order, propagation semantics, rollback, quantity rules, layer mappings, BQ columns and BricsCAD adapters were not redesigned.

## Completion

Completed. Programmatic Template Apply/Save now rejects non-persistable Family property keys before project mutation.