# Work claim — Template structural canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-structure-canonicality-20260812-0822`
- Registered: `2026-08-12T08:22:00+07:00`
- Completed: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `63b06496c6996bd769a44ef88b88afb7b13c2203`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`
- Integration PR: `#649`
- Main integration commit: `7b35024e2181e46d1faf654b68e7aece2a193000`

## Confirmed defect

`TemplateProfileStore.Serialize(...)` always emits exactly one root section in fixed order: `families`, `rules`, `layerMappings`, `bqColumns`; every serialized `family` also always contains exactly one `properties` container. `TemplateProfileXmlSchemaValidator` required only *at most one* of these singleton containers and did not enforce root child order. Missing or reordered persisted structure could therefore load successfully and be silently rewritten into a different XML shape on the next save.

## Implemented scope

- `src/QS3D.Core/Templates/TemplateProfileXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/TemplateStructureCanonicalitySmoke.cs`
- this claim file for close-out

## Completed contract

- Exactly one `families`, `rules`, `layerMappings`, and `bqColumns` root section is required.
- Root sections must appear in serializer order: `families → rules → layerMappings → bqColumns`.
- Every persisted family must contain exactly one `properties` container.
- Whitespace-only XML formatting tolerance and canonical valid templates are preserved.
- Template data semantics, family/rule categories, BQ-column semantics, layer mappings, BricsCAD runtime and release behavior remain unchanged.

## Validation evidence

- PR `#649` squash-merged to `main` as `7b35024e2181e46d1faf654b68e7aece2a193000`.
- Post-merge readback confirms the schema validator uses `RequireExactlyOne(...)` for all four root sections and each family `properties` container, and checks the exact root element order.
- Post-merge readback confirms isolated smoke source covers missing root section, reordered root sections, missing family properties, and canonical acceptance.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source was re-read, and exact integration SHA/evidence is recorded above.
