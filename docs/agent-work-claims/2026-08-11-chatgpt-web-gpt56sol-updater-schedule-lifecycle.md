# Work claim — updater scheduling lifecycle boundary

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-updater-schedule-lifecycle`
- Registered: `2026-08-11T21:34:00+07:00`
- Baseline main SHA: `2c43f0b27c64d89d0c332cc5d9d7d1db38ac07ba`
- Priority: prevent a stale manual update request from launching the detached updater after the coordinator lifecycle has been stopped or replaced.

## Confirmed defect

`ScheduleLatestAsync()` awaits `CheckAsync(false)` and then calls `SecureUpdateLauncher.TrySchedule(...)` based only on the returned release. If `UpdateCoordinator.Stop()` runs while that release check is in flight, the old check correctly stops publishing because its generation is stale, but the awaiting `ScheduleLatestAsync()` still receives the result and can launch the detached updater anyway. Thus lifecycle invalidation currently protects notifications but not the actual scheduling side effect.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs`
- `scripts/preflight-update-coordinator-lifecycle.py`
- this claim file

## Intended contract

Capture the scheduling lifecycle generation before awaiting freshness. After the await, authorize `SecureUpdateLauncher.TrySchedule(...)` only while holding the coordinator lock and only if `_started` and the captured generation still match. A Stop/restart that wins first must make the stale scheduling request return a non-scheduling result. Existing single-flight generation isolation, release/SemVer/security checks and stale publish guards stay intact.

## Excluded scope

No `SecureUpdateLauncher.cs`, GitHub release client, PowerShell updater/manifest, Update Center UI, Ribbon/PluginEntry, Quantity/BQ, Workspace, Direct Draw, Core, signing, release, CI or LOCAL inbox edits.

## Validation plan

Re-fetch current coordinator/preflight before writes; add a generation snapshot and lock-linearized schedule authorization; extend the focused lifecycle gate to require the check-await-authorize order and reject a generation-blind `TrySchedule` call. Re-fetch exact diffs and verify ancestry after integration. No Actions dispatch or remote signed-update PASS.

## Completion condition

Stop/restart can invalidate a pending `ScheduleLatestAsync()` before it launches the updater, current-generation scheduling remains functional, static coverage is merged, and signed runtime qualification remains LOCAL-009.