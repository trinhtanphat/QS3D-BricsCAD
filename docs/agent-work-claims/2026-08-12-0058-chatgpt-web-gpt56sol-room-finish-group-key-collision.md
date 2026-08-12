# Work claim — Room Finish schedule collision-free grouping identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish-group-key-collision`
- Registered: `2026-08-12T00:58:00+07:00`
- Completed: `2026-08-12T01:19:00+07:00`
- Baseline main SHA: `892651bcf8aaeb452a554b5cde7a64b7f3647b35`
- Priority: P2 evidence-driven remote-safe reporting integrity

## Confirmed defect

`RoomFinishScheduleBuilder.Build(...)` grouped finish rows with `string.Join("\u001f", floorId, roomKey, category, familyId, material, unitHint)`. Those accepted string tokens were not encoded or escaped before concatenation. Distinct accepted grouping tuples could therefore serialize to the same dictionary key when a token contained U+001F, silently merging counts, finish quantities, element ids, room ids and source provenance.

This was the same composite-key failure class already corrected independently in Door/Opening, Curtain Wall and Material Usage schedules.

## Implemented

- `fc4eb7f55f711db601422aa6a91a7c1593f00673` — replaced delimiter-only Room Finish grouping identity with deterministic length-prefixed `GroupKey(...)` encoding while retaining the existing `StringComparer.OrdinalIgnoreCase` dictionary. Group tokens and first-seen ordering are unchanged: floor id, stable room id/unlinked sentinel, category, family id, material and unit hint.
- `06d822b5185dd0ffc381131cecf07ced66ae6d0c` — final regression fixture proves the historical U+001F collision using two valid linked Room Finish rows with different rooms, and separately proves identical valid unlinked tuples still group. Quantity, element-id, room-id and source-handle provenance are asserted independently.
- `9ba3e052d5dfff6099cb3cfacba758baa0778708` — registered the focused smoke through `ModuleInitializer`.
- `5af48810b454f5f8c0b9bb7ca635469e4369c845` — final focused static preflight guards the exact group-token order, length-prefixed invariant-culture encoding, case-insensitive dictionary behavior, absence of the old U+001F delimiter key, valid Room Finish identity fixture and provenance assertions.

The earlier draft smoke/preflight commits (`bbb780c201f64cbd9a35965c75283da92784f2fa`, `c78eb1a1502685cbdb8277f52aa67a92b7d87543`) were superseded before closure after auditing `RoomFinishIdentityService`: multiple same-category finishes linked to one Room are intentionally invalid, so the final fixture uses two distinct Rooms for the collision proof and unlinked rows for the ordinary grouping proof.

## Preserved behavior

- Existing case-insensitive grouping remains intact.
- Existing floor/room/family/material/unit grouping semantics, quantity formulas, element ordering and provenance append behavior are unchanged.
- No accepted identifier/text character was newly forbidden.
- No edit was made to `RoomFinishGenerator`, Room identity lifecycle, XLSX/UI/native BricsCAD, persistence, legacy `QuantityReportBuilder`, `ProjectQuantityReportBuilder` or BLT quantity arithmetic.

## Validation performed

- Re-fetched current `RoomFinishSchedule.cs`; it uses the length-prefixed helper and no longer uses delimiter-only U+001F grouping.
- Re-fetched the final smoke, registration and focused preflight from `main` and inspected the final tokens/fixtures.
- Audited `RoomFinishIdentityService.ValidateProject()` and corrected the draft test so the linked collision rows remain valid under the one-finish-per-Room/category invariant.
- Verified the historical serialized keys are actually identical for `(floor=A\u001fB, room=C, ...)` and `(floor=A, room=B\u001fC, ...)`; the new length-prefixed encoding separates them.
- Compared final preflight commit `5af48810b454f5f8c0b9bb7ca635469e4369c845` to later `main` `3ade04e6da54598dd6dd4e69221c92576e160d54`: later main is 25 commits ahead with no changes to this lane's source/test/registration/preflight files.
- No GitHub Actions workflow was dispatched. This remote lane does **not** claim the smoke, .NET build, focused preflight or BricsCAD runtime was executed in a real checkout.

## LOCAL_ONLY disposition

This is a Core reporting-identity fix and requires no new local-only queue item. Existing licensed BricsCAD/native/UI qualification boundaries remain unchanged; no remote V25/V26 runtime PASS is claimed.

## Completion evidence

Distinct accepted Room Finish grouping tuples can no longer alias through U+001F delimiter injection, while identical tuples continue grouping with the existing case-insensitive semantics. Final source commit: `fc4eb7f55f711db601422aa6a91a7c1593f00673`; final regression fixture: `06d822b5185dd0ffc381131cecf07ced66ae6d0c`; registration: `9ba3e052d5dfff6099cb3cfacba758baa0778708`; final preflight: `5af48810b454f5f8c0b9bb7ca635469e4369c845`.