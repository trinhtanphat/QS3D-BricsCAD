# Work claim — Template category token canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-category-token-canonicality-20260812-0812`
- Registered: `2026-08-12T08:12:00+07:00`
- Last Updated: `2026-08-12T08:15:00+07:00`
- Baseline main SHA: `e6b4f50de81cec00813857f946bca48e9a699c14`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`
- Integration PR: `#639`
- Main integration commit: `e21d9a81a21d9f669b1beb993ccc7094795ef72d`

## Confirmed defect

`TemplateProfileStore.Load(...)` parsed persisted family/rule `category` values with case-insensitive `Enum.TryParse(...)` after `Required(...)` trimmed the XML attribute. The serializer writes `ElementCategory` using its canonical enum name. Therefore inputs such as lowercase, padded, or a numeric token for a defined enum value could be accepted and then silently rewritten to a different representation on the next save.

The earlier template category-definedness lane only rejected undefined numeric enum values; it did not require the exact serializer representation.

## Implemented scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateCategoryTokenCanonicalitySmoke.cs`
- this claim file for close-out

## Contract

- Persisted family and quantity-rule categories must equal the exact canonical `ElementCategory.ToString()` token emitted by `Serialize(...)`.
- Reject leading/trailing whitespace, case variants, and defined numeric enum tokens instead of normalizing them during load.
- Preserve valid canonical template round-trip behavior.
- Do not change layer-mapping semantics, template import UI/lifecycle, recognition rules, or BricsCAD runtime behavior.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Validation evidence

- PR `#639` was squash-merged with expected head `299a594f242c162bbef6305271e1b2bbe14eab72` to `main` as `e21d9a81a21d9f669b1beb993ccc7094795ef72d`.
- Post-merge readback confirms both family and rule paths call `RequiredCanonicalCategory(...)` and the helper requires a defined enum token whose raw XML text exactly equals `category.ToString()`.
- Post-merge readback confirms the focused smoke source covers lowercase, padded and defined numeric tokens for both family and quantity-rule categories plus canonical round-trip acceptance.
- Layer-mapping parsing remains unchanged and outside this lane.
- No GitHub Actions were dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source was re-read, and exact integration SHA/evidence is recorded above.
