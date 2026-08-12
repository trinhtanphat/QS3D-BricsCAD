# Work claim — Auto Room selection freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-auto-room-selection-freshness-20260812-1024`
- Registered: `2026-08-12T10:24:00+07:00`
- Baseline main SHA: `e1134b3b15e912a73ca7e7ddf5f5a9be9b988612`
- Priority: P1 — stale-room planning must not commit against a project revision changed during caller input enumeration.
- Task Key: `CORE-AUTO-ROOM-SELECTION-FRESHNESS`

## Confirmed defect

`AutoRoomLifecycle.MarkStaleForSelection(...)` materializes caller-supplied `ISet<string>` values for active room IDs and selected source handles, then computes and mutates stale rooms. Unlike other callback/lazy-input hardened Core paths, it does not compare `ProjectState.ChangeVersion` before and after those external enumerations. A custom `ISet` implementation can mutate the project while either set is enumerated; stale-room planning then proceeds against the changed project and may call `project.Touch()` / mark rooms stale using input gathered across revisions.

## Reserved scope

- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`
- `tests/QS3D.Core.SmokeTests/AutoRoomSelectionFreshnessSmoke.cs`
- this claim file

## Intended contract

- Capture `project.ChangeVersion` immediately before materializing active/selected caller sets.
- After both caller-supplied sets are materialized, require the project revision to be unchanged before resolving/planning stale rooms.
- On revision drift, fail before stale-room property/dirty/project mutation; preserve caller-side effects themselves.
- Preserve stable-set behavior, UTC validation, scope/signature matching, stale metadata and no-target no-op semantics.
- Do not alter Auto Room topology generation, Family synchronization, UI/native BricsCAD or unrelated freshness lanes.

## Validation plan

Focused auto-registered Core smoke supplies a custom `ISet<string>` whose enumerator touches the project while active IDs are read, requires `MarkStaleForSelection(...)` to reject without marking the candidate room stale or adding stale metadata beyond the caller's deliberate project-side effect, and includes a stable HashSet control proving normal stale marking still works. Re-fetch exact source/claim before writes. No force-push, GitHub Actions dispatch, executable full-smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
