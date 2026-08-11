# Work claim — Material usage schedule collision-free grouping identity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:50:00+07:00`
- Baseline main SHA: `b5d91728e369b98931b2bf456302e0a237bf4039`
- Priority: evidence-driven remote-safe reporting integrity

## Confirmed defect

`MaterialUsageScheduleBuilder` groups rows with an unescaped U+001F delimiter across floor/material/component/category/family tokens. Accepted IDs/material text can contain U+001F internally, so distinct tuples can serialize to the same dictionary key. A concrete collision is floor `A<US>B` + material `C` versus floor `A` + material `B<US>C` when component/category/family are equal. This can incorrectly merge element counts, quantities and provenance.

## Reserved scope

Replace the ambiguous delimiter-only material schedule group identity with deterministic collision-free token encoding while preserving case-insensitive grouping, row ordering, metrics and provenance behavior.

## Expected surfaces

- `src/QS3D.Core/Reporting/MaterialUsageSchedule.cs`
- `tests/QS3D.Core.SmokeTests/MaterialUsageScheduleGroupKeyCollisionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/MaterialUsageScheduleGroupKeyCollisionRegistration.cs`
- this claim file

## Excluded scope

- No material catalog/unit policy changes.
- No room finish lifecycle changes.
- No quantity formula/business-rule changes.
- No XLSX export or native BricsCAD changes.
- No new restrictions on accepted text characters.
- No GitHub Actions dispatch.

## Validation plan

- Preserve normal grouping for identical tuples.
- Prove the concrete floor/material U+001F collision yields two distinct rows.
- Verify element counts and representative LengthM quantities remain independent.
- Use length-prefixed token encoding, a dedicated module initializer, target re-fetch before product write, exact diff review and ancestry verification.
- No .NET/V25 runtime PASS will be claimed unless actually executed.

## Coordination

Recent searches found no active/recent claim reserving `MaterialUsageSchedule.cs`. Material catalog and room-finish policies are explicitly excluded.

## Completion condition

Distinct accepted material schedule tuples cannot alias through delimiter injection, focused regression source is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.