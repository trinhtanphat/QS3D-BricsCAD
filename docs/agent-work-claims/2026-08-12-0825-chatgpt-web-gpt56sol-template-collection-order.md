# Work claim — Template collection order canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-collection-order-20260812-0825`
- Registered: `2026-08-12T08:25:00+07:00`
- Baseline main SHA: `67fdb2ad2190c7ccbd472172f9fd123ddcb73534`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`

## Confirmed defect

`TemplateProfileStore.Serialize(...)` deterministically sorts families by id, quantity rules by id, layer mappings by pattern, and each family's properties by property key using `StringComparer.OrdinalIgnoreCase`. `Load(...)` currently accepts these collections in arbitrary persisted order and later `Save(...)` rewrites them into serializer order. This permits lossy representation changes at a persisted-format boundary.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- one isolated Core smoke file for collection-order canonicality
- this claim file for close-out

## Contract

- Persisted family, quantity-rule, layer-mapping, and family-property collections must already be in the order emitted by `Serialize(...)`.
- Preserve existing duplicate validation, category/BQ-column/structural strictness and canonical valid templates.
- Do not broaden into id/name whitespace policy, template apply semantics, BricsCAD runtime, or release behavior.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source is re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
