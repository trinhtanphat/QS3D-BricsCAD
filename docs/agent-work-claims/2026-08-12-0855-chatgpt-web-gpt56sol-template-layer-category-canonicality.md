# Work claim — Template layer-mapping category canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-layer-category-canonicality-20260812-0855`
- Registered: `2026-08-12T08:55:00+07:00`
- Baseline main SHA: `816e9cc7a0141749c818e315713a1fdbc8d33e15`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`

## Confirmed defect

Persisted template layer mappings are semantic `ElementCategory` values. `TemplateProfileStore.Load(...)` reads each `<map category="...">` through `Required(...)`, which trims the XML token, while `Validate(...)` / `ProjectRecognitionService.ValidateLayerMappings(...)` parse categories case-insensitively and accept defined numeric enum tokens. `Apply(...)` then stores `category.ToString()` in project metadata. Padded, lowercase, or numeric persisted category tokens can therefore be accepted and later normalized to a different representation.

The completed family/rule category-token lane intentionally excluded layer-mapping semantics, so this exact persisted surface remains unowned.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- one isolated Core smoke file for layer-mapping category-token canonicality
- this claim file for close-out

## Contract

- Persisted layer-mapping category tokens must equal the exact canonical `ElementCategory.ToString()` representation.
- Reject padded, case-variant, and defined numeric persisted category tokens instead of normalizing them during Load/Apply.
- Preserve canonical layer mappings, existing normalized-pattern ambiguity checks, collection ordering, and programmatic recognition behavior outside persisted template loading.
- Do not change family/rule category or BQ-column semantics, template UI/lifecycle, BricsCAD runtime, or release behavior.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, source is re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
