# Work claim — Auto Room previous Family category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-auto-room-previous-family-category`
- Registered: `2026-08-12T11:01:00+07:00`
- Baseline main SHA: `a3d4633e213d8cc422f839a7e4872a865cecf6fc`
- Priority: P1 — fail-closed Family provenance before Auto Room mutation planning.

## Confirmed defect

`AutoRoomLifecycle.SyncFamilyDefaults(...)` validates that the target Family is `Room`, and it rejects a missing previous Family, but when the room already references an existing previous Family of another category it still snapshots that Family's properties as prior Room defaults. Those unrelated defaults can then participate in inherited-property detection/removal before the room is reassigned to the target Room Family.

The canonical single/bulk Family assignment contracts now reject a previous Family whose category differs from the element. Auto Room synchronization is a separate mutation path and is missing the same integrity guard.

## Reserved scope

- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`
- `tests/QS3D.Core.SmokeTests/AutoRoomPreviousFamilyCategoryIntegritySmoke.cs`
- this claim file

## Intended contract

- If a Room's existing nonblank `FamilyId` resolves to a Family whose category is not `Room`, fail with `InvalidOperationException` before any project/room/metadata mutation.
- Preserve dangling-Family, global identity, malformed-default, bootstrap, topology, finish and valid Room-to-Room synchronization semantics.
- Add a focused failure-atomicity regression plus valid Room-to-Room control.
- No GitHub Actions dispatch; no native BricsCAD qualification claim.
