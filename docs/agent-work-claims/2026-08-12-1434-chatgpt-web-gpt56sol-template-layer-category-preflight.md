# Template layer-mapping category preflight canonicality

- Status: ACTIVE
- Coordination state: ACTIVE
- Owner: ChatGPT Web / GPT-5.6 Sol
- Baseline: `main@cbeec56a08be2962df8ff27d9d46bde55db8e73a`
- Scope:
  - `src/QS3D.Core/Templates/TemplateProfileStore.cs`
  - `tests/QS3D.Core.SmokeTests/TemplateLayerMappingCategoryCanonicalitySmoke.cs`
- Validation: exact commit diff/read-back plus focused Core smoke source coverage. No GitHub Actions dispatch; no local .NET/BricsCAD runtime PASS claimed.

## Defect

The persisted template contract already requires layer-mapping category tokens to be the exact canonical `ElementCategory.ToString()` representation. `Load()` enforces that contract through `RequiredCanonicalLayerMappingCategory`, and the serializer writes the in-memory `LayerMappings` value verbatim. However, the in-memory `Validate(TemplateProfile)` preflight still uses case-insensitive `Enum.TryParse`, so a profile containing a token such as `beam` can pass preflight, be serialized to a temp file, and only then be rejected by the store's own defensive `Load(temp)` canonicality check. This creates an avoidable read/write contract mismatch and allows filesystem side effects before a deterministically invalid profile is rejected.

## Planned change

Require each in-memory layer-mapping category to be a defined, exact canonical enum-name token during `Validate(TemplateProfile)` while preserving the existing `ProjectRecognitionService.ValidateLayerMappings` checks for mapping-pattern/ambiguity semantics. Extend the existing `TemplateLayerMappingCategoryCanonicalitySmoke` to prove canonical values remain accepted and case-variant/padded/numeric aliases are rejected by `Save()` before its target directory/temp-file path is created.

No changes to project-recognition tolerance, template XML loader semantics, Template Apply scoring, or native/runtime behavior are in scope.