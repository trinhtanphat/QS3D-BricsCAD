# Work claim — updater scheduling lifecycle boundary

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-updater-schedule-lifecycle`
- Registered: `2026-08-11T21:34:00+07:00`
- Completed: `2026-08-11T21:37:00+07:00`
- Baseline main SHA: `2c43f0b27c64d89d0c332cc5d9d7d1db38ac07ba`
- Priority: prevent a stale manual update request from launching the detached updater after the coordinator lifecycle has been stopped or replaced.

## Confirmed defect

Before this fix, `ScheduleLatestAsync()` awaited `CheckAsync(false)` and then called `SecureUpdateLauncher.TrySchedule(...)` based only on the returned release. If `UpdateCoordinator.Stop()` ran while that release check was in flight, the old check correctly stopped publishing because its generation was stale, but the awaiting scheduling call still received the result and could launch the detached updater. Lifecycle invalidation protected notifications but not the scheduling side effect.

## Completed changes

- Reservation: `0877355123da14134b32cc9ed2acc623b3677914` — `chore(agent): claim updater schedule lifecycle`.
- Source fix: `ab0f76d292908183035dfddd99ecb37b1aeb8a6d` — `fix(updater): bind scheduling to active lifecycle`.
  - `ScheduleLatestAsync()` captures the coordinator generation before awaiting the fresh release check;
  - detached updater launch now routes through `TryScheduleCurrentGeneration(...)`;
  - that helper holds `_sync` while checking `_started && generation == _generation` and while calling `SecureUpdateLauncher.TrySchedule(...)`, giving Stop/restart and scheduling one explicit ordering boundary;
  - a stale lifecycle returns an error result without publishing stale state or launching the updater;
  - successful scheduling is published only while the captured generation is still current.
- Regression guard: `4ea67d5456787fae282356c5f09d93bb156a8d7c` — `test(updater): guard schedule lifecycle authorization`.
  - requires lifecycle capture before the await and authorization afterward;
  - requires lock-linearized current-generation validation before the detached launcher call;
  - rejects a direct `SecureUpdateLauncher.TrySchedule(...)` call from `ScheduleLatestAsync()`;
  - retains the earlier restart/single-flight generation guards.

## Integration verification

Exact source/preflight commit diffs were inspected. Current-main comparison from `4ea67d54...` reported `behind_by: 0` with that commit as merge base; later concurrent work was in Curtain/Direct Draw/theme/reporting surfaces and did not overwrite the updater coordinator/preflight. No force-push, reset or concurrent-file overwrite was used.

## Validation boundary

The source and static preflight are merged, but the Python preflight was **not executed in this connector-only lane**. No GitHub Actions, local checkout/build, live GitHub network race test, BricsCAD V25 launch, signed updater execution, installer, signing or release was dispatched.

The production/runtime update boundary remains `LOCAL-009 / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`; no native signed-update PASS is inferred from source review.

## Coordination / exclusions respected

No `SecureUpdateLauncher.cs`, GitHub release client, update/manifest PowerShell, Update Center UI, Ribbon/PluginEntry, Quantity/BQ, Workspace, Direct Draw, Core or LOCAL inbox files were edited.

## Completion condition

Satisfied for remote/source scope: Stop/restart can invalidate a pending scheduling request before updater launch, current-generation scheduling remains functional by source contract, focused regression coverage is merged, and signed runtime qualification remains local-only.