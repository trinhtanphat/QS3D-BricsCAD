# Work claim — Curtain wall schedule collision-free grouping identity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:46:00+07:00`
- Baseline main SHA: `6f08b169e50e51d2a401c7d2a45b354049992a9c`
- Priority: evidence-driven remote-safe reporting integrity

## Confirmed defect

`CurtainWallScheduleBuilder` groups rows with `floorId + "\u001f" + familyId`. Accepted floor/family IDs are trimmed but are not contractually forbidden from containing U+001F internally. Distinct tuples such as `(A<US>B, C)` and `(A, B<US>C)` therefore serialize to the same dictionary key and are incorrectly merged, corrupting wall counts, quantities and provenance.

## Reserved scope

Replace the ambiguous two-token delimiter grouping identity with deterministic collision-free encoding while preserving existing case-insensitive grouping semantics, row ordering, quantities and provenance behavior.

## Expected surfaces

- `src/QS3D.Core/Reporting/CurtainWallSchedule.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallScheduleGroupKeyCollisionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallScheduleGroupKeyCollisionRegistration.cs`
- this claim file

## Excluded scope

- No curtain geometry/layout/fingerprint/regeneration changes.
- No schedule field/business-rule changes.
- No new restrictions on accepted floor/family ID characters.
- No XLSX export changes.
- No GitHub Actions dispatch.

## Validation plan

- Preserve normal grouping for identical floor/family tuples.
- Prove `(A<US>B, C)` and `(A, B<US>C)` produce distinct rows rather than one merged row.
- Verify row counts and representative quantities remain independent.
- Use length-prefixed token encoding, a dedicated module initializer, target re-fetch before source write, exact diff review and ancestry verification.
- No .NET/V25 runtime PASS will be claimed unless actually executed.

## Coordination

Recent searches found no active/recent claim reserving `CurtainWallSchedule.cs`. Geometry/native curtain lanes are excluded.

## Completion condition

Distinct accepted curtain schedule grouping tuples cannot alias through delimiter injection, regression source is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.