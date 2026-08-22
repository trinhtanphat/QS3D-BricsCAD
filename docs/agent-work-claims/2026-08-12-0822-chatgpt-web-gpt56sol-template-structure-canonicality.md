# Work claim — Template structural canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-structure-canonicality-20260812-0822`
- Registered: `2026-08-12T08:22:00+07:00`
- Baseline main SHA: `63b06496c6996bd769a44ef88b88afb7b13c2203`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`

## Confirmed defect

`TemplateProfileStore.Serialize(...)` always emits exactly one root section in fixed order: `families`, `rules`, `layerMappings`, `bqColumns`; every serialized `family` also always contains exactly one `properties` container. `TemplateProfileXmlSchemaValidator` currently requires only *at most one* of these singleton containers and does not enforce root child order. Missing or reordered persisted structure can therefore load successfully and be silently rewritten into a different XML shape on the next save.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileXmlSchemaValidator.cs`
- one isolated Core smoke file for template structural canonicality
- this claim file for close-out

## Contract

- Require exactly one of each serializer-owned root section.
- Require those root sections in the serializer's fixed order.
- Require exactly one `properties` container for each persisted family.
- Preserve whitespace-only XML formatting tolerance and canonical valid templates.
- Do not change template data semantics, family/rule categories, BQ-column semantics, layer mappings, BricsCAD runtime, or release behavior.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source is re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
