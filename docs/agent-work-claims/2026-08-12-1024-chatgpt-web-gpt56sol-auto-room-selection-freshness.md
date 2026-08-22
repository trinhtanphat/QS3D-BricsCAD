# Work claim — Auto Room selection freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-auto-room-selection-freshness-20260812-1024`
- Registered: `2026-08-12T10:24:00+07:00`
- Last Updated: `2026-08-12T10:28:00+07:00`
- Baseline main SHA: `e1134b3b15e912a73ca7e7ddf5f5a9be9b988612`
- Source fix SHA: `6d9bc8d19fb9318818f7ec5032c7b94390eba6b0`
- Regression SHA: `16ee32b663c17894de02cc3cc1f9b2fe9a309819`
- Regression API-fix SHA: `b45dff3598388f68fc615c0e0ec26fa8c7ee4fb2`
- Priority: P1 — stale-room planning must not commit against a project revision changed during caller input enumeration.
- Task Key: `CORE-AUTO-ROOM-SELECTION-FRESHNESS`

## Confirmed defect

`AutoRoomLifecycle.MarkStaleForSelection(...)` materialized caller-supplied `ISet<string>` values for active room IDs and selected source handles, then computed and mutated stale rooms without comparing `ProjectState.ChangeVersion` before and after those external enumerations. A custom set could mutate the project while enumerated and stale-room planning would continue against a changed project revision.

## Completed implementation

- Capture `project.ChangeVersion` immediately before materializing active/selected caller sets.
- After both sets are materialized, require the project revision to be unchanged before resolving/planning stale rooms.
- Revision drift fails before `ResolveProjectElements`, stale-room metadata/dirty mutation, or Auto Room's own `project.Touch()`.
- Caller-side project changes are not rolled back; only the stale-selection operation is refused.
- Stable set behavior, UTC validation, scope/signature matching, stale metadata and no-target no-op semantics remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/AutoRoomSelectionFreshnessSmoke.cs` is auto-registered and covers:

- a custom `ISet<string>` whose enumerator touches the project causes freshness rejection;
- rejection preserves the room's active boundary state, dirty state and absence of stale metadata while retaining the caller-side ChangeVersion increment;
- stable HashSet inputs still mark the matching room stale, write `TopologyChanged`, and advance ChangeVersion exactly once.

The first regression commit used a constructor call that omitted the required `familyId` argument; this was detected during source/API readback before claim closure and corrected in `b45dff3598388f68fc615c0e0ec26fa8c7ee4fb2`. Source and corrected regression were re-read directly from `main`.

## Validation boundary

No GitHub Actions were dispatched. No executable full smoke/build or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only session.

## Completion condition

Completed: Auto Room stale-selection planning now rejects project revision drift caused during caller-set enumeration before committing stale-room state, while stable inputs preserve existing behavior.
