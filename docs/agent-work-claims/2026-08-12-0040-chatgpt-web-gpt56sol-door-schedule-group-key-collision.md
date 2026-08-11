# Work claim — Door/opening schedule collision-free grouping identity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:40:00+07:00`
- Baseline main SHA: `38fb4bb143a6d7b704d9c85e590a7e7e8a6f4d86`
- Priority: evidence-driven remote-safe reporting integrity

## Confirmed defect

`DoorOpeningScheduleBuilder` groups rows with `string.Join("\u001f", ...)` over floor/category/family/numeric/material tokens. Project/family/material identifiers are not contractually forbidden from containing U+001F internally. Because the delimiter is unescaped, distinct grouping tuples can serialize to the same dictionary key and be incorrectly merged, corrupting row count, provenance and accumulated opening area.

A concrete collision exists between:

- family `X<US>1`, width/height/sill/thickness `2/3/4/5`, material `M`; and
- family `X`, width/height/sill/thickness `1/2/3/4`, material `5<US>M`;

where `<US>` is U+001F. Both flatten to the same old delimiter-separated token sequence despite representing distinct schedule rows.

## Reserved scope

Replace the ambiguous delimiter-only schedule group identity with a deterministic collision-free encoding while preserving grouping semantics, row ordering, numeric validation, quantities and provenance behavior.

## Expected surfaces

- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs`
- `tests/QS3D.Core.SmokeTests/DoorOpeningScheduleGroupKeyCollisionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/DoorOpeningScheduleGroupKeyCollisionRegistration.cs`
- this claim file

## Excluded scope

- No schedule field/business-rule changes.
- No changes to door/opening geometry, family inheritance, quantity formulas or XLSX export.
- No new restrictions on valid ID/material characters; grouping must remain correct for existing accepted strings.
- No GitHub Actions dispatch.

## Validation plan

- Preserve normal grouping for identical rows.
- Prove the concrete U+001F boundary-shift collision yields two distinct rows, each Count=1 with its own area/material/family values.
- Use length-prefixed token encoding rather than banning currently accepted data.
- Use a dedicated module initializer, re-fetch the target blob immediately before source write, review exact diffs and verify ancestry.
- No .NET/V25 runtime PASS will be claimed unless actually executed.

## Coordination

Recent searches found no active/recent claim reserving `DoorOpeningSchedule.cs` or this grouping-key collision. Quantity-preview and native opening lanes are excluded.

## Completion condition

Distinct accepted schedule grouping tuples cannot alias through delimiter injection, focused regression source is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.