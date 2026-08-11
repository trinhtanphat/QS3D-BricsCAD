# Work claim — Room Finish schedule collision-free grouping identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-group-key-collision`
- Registered: `2026-08-12T00:58:00+07:00`
- Baseline main SHA: `892651bcf8aaeb452a554b5cde7a64b7f3647b35`
- Priority: P2 evidence-driven remote-safe reporting integrity

## Confirmed defect

`RoomFinishScheduleBuilder.Build(...)` groups finish rows with `string.Join("\u001f", floorId, roomKey, category, familyId, material, unitHint)`. These accepted string tokens are not encoded or escaped before concatenation. Distinct accepted grouping tuples can therefore serialize to the same dictionary key when a token contains U+001F, silently merging counts, finish quantities, element ids, room ids and source provenance.

This is the same composite-key failure class already corrected independently in Door/Opening, Curtain Wall and Material Usage schedules, but `RoomFinishSchedule.cs` still uses the delimiter-only form on current `main`.

## Reserved scope

Replace only the Room Finish grouped schedule composite identity with deterministic collision-free token encoding while preserving case-insensitive grouping, first-seen row ordering, existing room/family/material/unit semantics, quantity formulas and provenance behavior.

## Expected surfaces

- `src/QS3D.Core/Reporting/RoomFinishSchedule.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishScheduleGroupKeyCollisionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishScheduleGroupKeyCollisionRegistration.cs`
- `scripts/preflight-room-finish-group-key-collision.py`
- this claim file

## Explicit exclusions

- No edits to the active legacy or project quantity-report collision lanes.
- No `RoomFinishGenerator`, `ElementInstance`, Room identity lifecycle, XLSX/UI/native BricsCAD or persistence changes.
- No new character restrictions and no BLT/native quantity arithmetic inference.
- No GitHub Actions dispatch.

## Validation plan

- Preserve normal grouping for identical tuples.
- Prove two accepted U+001F-bearing Room Finish grouping tuples remain distinct and keep independent counts/quantities/provenance.
- Use length-prefixed tokens rather than forbidding accepted text.
- Add focused source/smoke/static coverage, re-fetch exact files and commits after writes, and preserve concurrent `main` history.
- Do not claim .NET, BricsCAD V25/V26 or preflight execution unless actually run.

## Coordination

Recent claim/commit review found active collision lanes for `QuantityReportBuilder.cs` and `ProjectQuantityReportBuilder.cs`; this claim does not touch either. The current Room Finish smoke-alignment claim is limited to `RoomFinishGeneratorNumericSafetySmoke.cs` and explicitly does not own `RoomFinishSchedule.cs`.

## Completion condition

Distinct accepted Room Finish grouping tuples cannot alias through delimiter injection; focused regression/preflight sources are present on current `main`; exact implementation SHAs are recorded; the claim is marked `COMPLETED` without overwriting concurrent work.