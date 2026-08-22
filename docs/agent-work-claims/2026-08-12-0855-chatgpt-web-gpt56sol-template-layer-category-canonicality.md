# Work claim — Template layer-mapping category canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-layer-category-canonicality-20260812-0855`
- Registered: `2026-08-12T08:55:00+07:00`
- Completed: `2026-08-12T08:59:00+07:00`
- Baseline main SHA: `816e9cc7a0141749c818e315713a1fdbc8d33e15`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`
- Integration PR: `#668`
- Main integration commit: `ffc293e6b9b1c872bdadb26bdcdcef354a83ea7c`

## Confirmed defect

Persisted template layer mappings are semantic `ElementCategory` values. `TemplateProfileStore.Load(...)` read each `<map category="...">` through `Required(...)`, which trimmed the XML token, while `Validate(...)` / `ProjectRecognitionService.ValidateLayerMappings(...)` parse categories case-insensitively and accept defined numeric enum tokens. `Apply(...)` then stores `category.ToString()` in project metadata. Padded, lowercase, or numeric persisted category tokens could therefore be accepted and later normalized to a different representation.

The completed family/rule category-token lane intentionally excluded layer-mapping semantics, so this exact persisted surface was still unowned.

## Implemented scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateLayerMappingCategoryCanonicalitySmoke.cs`
- this claim file for close-out

## Completed contract

- Persisted layer-mapping category tokens must equal the exact canonical `ElementCategory.ToString()` representation.
- Padded, case-variant, and defined numeric persisted category tokens fail closed instead of being normalized during Load/Apply.
- Canonical layer mappings, existing normalized-pattern ambiguity checks, collection ordering, and programmatic recognition behavior outside persisted template loading remain unchanged.
- Family/rule category and BQ-column semantics, template UI/lifecycle, BricsCAD runtime and release behavior remain unchanged.

## Validation evidence

- Claim registration: `9b0596a298a1ed56b2432092a02cd12e802e8b76`.
- Branch source commit: `cb86836c391e87099c9e999e8d320b1207efee59`.
- Branch smoke commit: `2d4979530dd5b7673232e907371a69835f116675`.
- Branch was synchronized with moving `main` without force-push and PR `#668` squash-merged to `main` as `ffc293e6b9b1c872bdadb26bdcdcef354a83ea7c`.
- Post-merge readback confirms persisted map categories flow through `RequiredCanonicalLayerMappingCategory(...)`, which requires an exact defined enum-name token, and isolated smoke coverage is present.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, source was re-read, and exact integration SHA/evidence is recorded above.
