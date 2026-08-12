# Work claim — Door/opening schedule collision-free grouping identity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:40:00+07:00`
- Completed: `2026-08-12T00:44:00+07:00`
- Baseline main SHA: `38fb4bb143a6d7b704d9c85e590a7e7e8a6f4d86`
- Claim commit: `cb0a2b6c540678a54b75f1252ae64472287aec48`
- Priority: evidence-driven remote-safe reporting integrity

## Confirmed defect

`DoorOpeningScheduleBuilder` grouped rows with an unescaped U+001F delimiter across floor/category/family/numeric/material tokens. Accepted strings can contain U+001F internally, so distinct grouping tuples could serialize to the same dictionary key and be incorrectly merged, corrupting row count, provenance and accumulated opening area.

## Completed scope

The grouping identity now uses deterministic length-prefixed tokens. No accepted ID/material characters were banned, and the existing case-insensitive grouping comparer, ordering, numeric validation, quantity accumulation and provenance semantics remain unchanged.

## Product/test commits

- `c910f6c1c61c0ddc8cb5c5e81adb35c4be7956c1` — `fix(reporting): make door schedule grouping collision-free`
- `10485d5be8e877467c456174f0d4db2f7b202442` — `test(reporting): cover door schedule group key collision`
- `d5e62f67f2bcd7df1ee7147e370ff5e0d476e005` — `test(reporting): register door schedule group key smoke`

## Validation

- Re-fetched the exact target blob after claim publication before the source write.
- Product diff only replaces the ambiguous delimiter key with a length-prefixed `GroupKey` helper and adds `System.Text`.
- Regression uses a concrete old-key collision: family `X<US>1` with dimensions `2/3/4/5`, material `M`, versus family `X` with dimensions `1/2/3/4`, material `5<US>M`. It also adds an identical third row to prove equal tuples still group.
- Expected behavior is two rows: the identical pair groups to Count=2/area=12, while the formerly colliding tuple remains Count=1/area=2 with its original family/material text.
- Registration uses a dedicated module initializer.
- After registration, observed `main` at `4fce6a653e5438fe21bb18a8841b6d619284f0d5`; comparison from `d5e62f67f2bcd7df1ee7147e370ff5e0d476e005` reported `status=ahead`, `behind_by=0`, merge base equal to the registration commit. Concurrent changes touched unrelated geometry/local handoff surfaces.
- GitHub Actions were not dispatched.
- No .NET SDK or BricsCAD V25 runtime PASS is claimed from this hosted session.

## Excluded scope

- No schedule field/business-rule changes.
- No door/opening geometry, family inheritance, quantity formula or XLSX export changes.

## Completion

Distinct accepted schedule tuples no longer alias through delimiter injection on current `main`; claim released as completed.