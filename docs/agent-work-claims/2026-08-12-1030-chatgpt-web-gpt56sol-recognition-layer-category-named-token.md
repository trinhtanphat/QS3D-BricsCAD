# Work claim — Recognition layer-mapping category named-token canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-recognition-layer-category-named-token`
- Registered: `2026-08-12T10:30:00+07:00`
- Completed: `2026-08-12T10:32:00+07:00`
- Baseline main SHA: `534f5c4e39ab0f509acf7fbc13c851c0f766925b`
- Claim commit: `9a1549fcb99fe59c6ffb52107b0cadf0326a2499`
- Source fix commit: `cd5d614a81314be4d53997eeb2c514357af90d75`
- Regression commit: `9c8c22c8094cecc7a110836b9f6a605e85b24982`
- Registration commit: `50f3ed49436614fd2f0aeac9f8b87d444c484bf9`
- Priority: P2 — persisted project recognition mappings must use named `ElementCategory` tokens rather than numeric enum aliases.

## Confirmed defect

`ProjectRecognitionService.ValidateLayerMappings(...)` and the exact-layer read path used `Enum.TryParse(..., true)` plus `Enum.IsDefined(...)`. .NET enum parsing also accepts numeric strings, so a persisted project mapping value such as a numeric `ArchitecturalWall` ordinal could be treated as a valid category when it maps to a defined `ElementCategory` value.

This conflicted with repository writer/schema behavior: template application persists `category.ToString()` names into `QS3D.LayerMapping:*`, and template-file loading already rejects non-canonical layer-mapping category tokens. The historical `reject undefined project mapping categories` fix closed undefined enum values but did not close numeric aliases.

## Implemented surfaces

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectRecognitionLayerCategoryNamedTokenSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectRecognitionLayerCategoryNamedTokenRegistration.cs`
- this claim file

## Implemented contract

- Added one shared `TryParseNamedCategory(...)` helper.
- A mapping token must parse to a defined `ElementCategory` and match that member's enum name case-insensitively; numeric enum aliases fail closed.
- Existing trimming/case-insensitive named-token compatibility is preserved.
- Both mapping validation and exact-layer recognition now use the same rule.
- Scoring, confidence, entity compatibility and layer-pattern normalization are unchanged.

## Excluded scope honored

- No recognition scoring/confidence/entity-type changes.
- No Template XML serialization/schema changes.
- No layer-pattern normalization changes.
- No native V25/V26 recognition UI/runtime changes.
- No GitHub Actions dispatch or runtime qualification claim.

## Validation actually performed

- Direct path history was inspected. The prior relevant fix `6038a20622a8eacb005462d9432e0b8e4948ece8` only added `Enum.IsDefined(...)`, confirming numeric aliases remained possible.
- Template writer/loader contract was inspected: project mappings are written with `category.ToString()`, while template file loading requires canonical named category tokens.
- Claim was published before substantive writes.
- Source was re-fetched after claim publication at exact blob `9d5de0b157dadbc83abc8a4249bdb0cb5d586429` and updated with that blob SHA guard.
- Exact source diff was reviewed: `11` additions / `2` deletions, limited to replacing the two enum-parse sites with the shared named-token parser and adding that parser.
- Focused smoke was read back from `main`: it derives the numeric alias from `(int)ElementCategory.ArchitecturalWall` and expects fail-closed `InvalidOperationException`; lowercase named token `architecturalwall` remains accepted and yields the expected `project-layer:A-WALL` candidate at confidence `0.99`.
- Module-initializer registration is committed on `main` as `50f3ed49436614fd2f0aeac9f8b87d444c484bf9`.
- No local .NET compile/test execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime qualification is claimed.
- No GitHub Actions were dispatched and no force-push was used.

## Completion condition

Completed. Persisted project recognition layer mappings no longer accept numeric `ElementCategory` aliases, focused regression source is on `main`, and exact implementation/test SHAs are recorded above.
