# Work claim — Template collection order canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-collection-order-20260812-0825`
- Registered: `2026-08-12T08:25:00+07:00`
- Completed: `2026-08-12T08:30:00+07:00`
- Baseline main SHA: `67fdb2ad2190c7ccbd472172f9fd123ddcb73534`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`
- Integration PR: `#651`
- Main integration commit: `fdcecc6685b85e6832d48bdf4700527528ad2970`

## Confirmed defect

`TemplateProfileStore.Serialize(...)` deterministically sorts families by id, quantity rules by id, layer mappings by pattern, and each family's properties by property key using `StringComparer.OrdinalIgnoreCase`. `Load(...)` accepted these collections in arbitrary persisted order and later `Save(...)` rewrote them into serializer order. This permitted lossy representation changes at a persisted-format boundary.

## Implemented scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateCollectionOrderCanonicalitySmoke.cs`
- this claim file for close-out

## Completed contract

- Persisted family collections must already be ordered by family id as emitted by `Serialize(...)`.
- Persisted quantity rules must already be ordered by rule id.
- Persisted layer mappings must already be ordered by pattern.
- Persisted family properties must already be ordered by property key.
- Existing duplicate validation, category/BQ-column/structural strictness and canonical valid templates remain intact.
- No new id/name whitespace policy was introduced and template apply semantics were not changed.

## Validation evidence

- PR `#651` squash-merged to `main` as `fdcecc6685b85e6832d48bdf4700527528ad2970`.
- Post-merge readback confirms `RequireCanonicalOrder(...)` is applied to family properties, families, quantity rules, and layer mappings using the same `StringComparer.OrdinalIgnoreCase` ordering as the serializer.
- Post-merge readback confirms isolated smoke source covers all four reversed collection variants plus canonical acceptance.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source was re-read, and exact integration SHA/evidence is recorded above.
