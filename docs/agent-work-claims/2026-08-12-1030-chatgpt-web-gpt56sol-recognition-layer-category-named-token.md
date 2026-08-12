# Work claim — Recognition layer-mapping category named-token canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-layer-category-named-token`
- Registered: `2026-08-12T10:30:00+07:00`
- Baseline main SHA: `534f5c4e39ab0f509acf7fbc13c851c0f766925b`
- Priority: P2 — persisted project recognition mappings must use named `ElementCategory` tokens rather than numeric enum aliases.

## Confirmed defect

`ProjectRecognitionService.ValidateLayerMappings(...)` and the exact-layer read path use `Enum.TryParse(..., true)` plus `Enum.IsDefined(...)`. .NET enum parsing also accepts numeric strings, so a persisted project mapping value such as `"3"` can be treated as a valid category when it maps to a defined `ElementCategory` value.

This conflicts with repository writer/schema behavior: template application persists `category.ToString()` names into `QS3D.LayerMapping:*`, and template-file loading already rejects non-canonical layer-mapping category tokens. The historical `reject undefined project mapping categories` fix closed undefined enum values but did not close numeric aliases.

## Reserved scope

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectRecognitionLayerCategoryNamedTokenSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectRecognitionLayerCategoryNamedTokenRegistration.cs`
- this claim file

## Intended contract

- Project recognition mapping categories must parse to a defined `ElementCategory` **and** correspond to that enum member's named token; numeric aliases fail closed.
- Preserve current case-insensitive named-token acceptance and existing layer-pattern normalization/ambiguity rules.
- Exact-layer recognition and projected/template validation share the same category-token rule.

## Excluded scope

- No recognition scoring/confidence/entity-type changes.
- No Template XML serialization/schema changes.
- No layer-pattern normalization changes.
- No native V25/V26 recognition UI/runtime changes.
- No GitHub Actions dispatch or runtime qualification claim.

## Validation plan

- Verify claim ancestry and re-fetch exact source blob before write.
- Add a shared named-category parser used by validation and exact-layer resolution.
- Add focused module-initializer smoke proving a numeric alias is rejected while a lowercase named category remains accepted and recognized.
- Review exact pushed diff/read-back, close claim with exact SHAs, and verify ancestry.
- No local compile/runtime PASS will be claimed unless actually executed.
