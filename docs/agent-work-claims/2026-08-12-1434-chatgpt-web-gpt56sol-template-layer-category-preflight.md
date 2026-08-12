# Template layer-mapping category preflight canonicality

- Status: COMPLETED
- Coordination state: COMPLETED
- Owner: ChatGPT Web / GPT-5.6 Sol
- Baseline: `main@cbeec56a08be2962df8ff27d9d46bde55db8e73a`
- Product fix: `c6239572789a0b70c680f2d4db82718f0df29a5f`
- Regression: `806060be55e02cfdc9eb066454eb85e6eab58c89`
- Scope:
  - `src/QS3D.Core/Templates/TemplateProfileStore.cs`
  - `tests/QS3D.Core.SmokeTests/TemplateLayerMappingCategoryCanonicalitySmoke.cs`
- Validation: exact commit diff/read-back plus focused auto-registered Core smoke source coverage. No GitHub Actions dispatch; no local .NET/BricsCAD runtime PASS claimed.

## Defect

The persisted template contract already requires layer-mapping category tokens to be the exact canonical `ElementCategory.ToString()` representation. `Load()` enforces that contract through `RequiredCanonicalLayerMappingCategory`, and the serializer writes the in-memory `LayerMappings` value verbatim. However, the in-memory `Validate(TemplateProfile)` preflight still used case-insensitive `Enum.TryParse`, so a profile containing a token such as `beam` could pass preflight, be serialized to a temp file, and only then be rejected by the store's own defensive `Load(temp)` canonicality check. This created an avoidable read/write contract mismatch and allowed filesystem side effects before a deterministically invalid profile was rejected.

## Completed change

`Validate(TemplateProfile)` now requires every layer-mapping category value to be nonblank, a defined `ElementCategory`, and exactly equal to the canonical enum-name token under ordinal comparison. The existing `ProjectRecognitionService.ValidateLayerMappings` call remains in place for mapping-pattern and ambiguity semantics; project-recognition tolerance itself was not changed.

`TemplateLayerMappingCategoryCanonicalitySmoke` now covers the in-memory Save path in addition to persisted Load: lowercase, padded, and numeric aliases are rejected before the target directory can be created, while canonical `Beam` continues to Save/Load round-trip.

No changes were made to project-recognition scoring, template XML loader semantics beyond the already-existing canonical contract, native/runtime behavior, or GitHub Actions.