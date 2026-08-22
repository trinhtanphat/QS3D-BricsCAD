# Work claim — Template category token canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-category-token-canonicality-20260812-0812`
- Registered: `2026-08-12T08:12:00+07:00`
- Baseline main SHA: `e6b4f50de81cec00813857f946bca48e9a699c14`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`

## Confirmed defect

`TemplateProfileStore.Load(...)` parses persisted family/rule `category` values with case-insensitive `Enum.TryParse(...)` after `Required(...)` trims the XML attribute. The serializer writes `ElementCategory` using its canonical enum name. Therefore inputs such as lowercase, padded, or a numeric token for a defined enum value can be accepted and then silently rewritten to a different representation on the next save.

The earlier template category-definedness lane only rejects undefined numeric enum values; it does not require the exact serializer representation.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- one isolated Core smoke file for family/rule category token canonicality
- this claim file for close-out

## Contract

- Persisted family and quantity-rule categories must equal the exact canonical `ElementCategory.ToString()` token emitted by `Serialize(...)`.
- Reject leading/trailing whitespace, case variants, and defined numeric enum tokens instead of normalizing them during load.
- Preserve valid canonical template round-trip behavior.
- Do not change layer-mapping semantics, template import UI/lifecycle, recognition rules, or BricsCAD runtime behavior.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source is re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
