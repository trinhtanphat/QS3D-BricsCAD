# Work claim — Template family property key canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-family-property-key-canonicality-20260812-0931`
- Registered: `2026-08-12T09:31:00+07:00`
- Baseline main SHA: `4721cc060f242edc67e4d2ec14cb2981ce8e6f60`

## Confirmed defect

`TemplateProfileStore.Apply` accepts programmatic `TemplateProfile` instances directly. `Validate(profile)` checks family/rule identity and layer mappings but does not validate `ProjectFamily.Properties` keys. A public in-memory profile can therefore contain an empty/padded Family property key and Apply it into `ProjectState`. `QsdbProjectStore.ValidateProject` explicitly requires Family property keys to be non-empty and unpadded, so the successful Apply can leave a project that the next QSDB save must reject.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- one focused Core smoke for programmatic Apply family-property key validation
- this claim file

Fail closed during template validation for blank or leading/trailing-whitespace Family property keys, before Apply captures/mutates project state. Preserve Family property values, canonical keys, XML persistence behavior, propagation semantics, rollback, quantity rules, layer mappings and BQ columns.

## Completion

Complete only after source + focused regression are on current `main`, exact SHAs are recorded here, and this claim is marked `COMPLETED`. No GitHub Actions, local .NET build or BricsCAD runtime qualification is claimed by this remote lane.