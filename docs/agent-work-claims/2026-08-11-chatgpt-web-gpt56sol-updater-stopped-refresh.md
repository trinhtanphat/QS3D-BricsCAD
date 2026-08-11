# Work claim — updater stopped-refresh lifecycle

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-updater-stopped-refresh`
- Registered: `2026-08-11T22:02:00+07:00`
- Completed: `2026-08-11T22:06:00+07:00`
- Baseline main SHA: `41b30b04c5bed23c163ad643798c21cfe0b58d5f`
- Priority: make `UpdateCoordinator.Stop()` a real lifecycle boundary for manual refresh/check entry points as well as result publication.

## Confirmed defect

Before this fix, `UpdateCoordinator.Stop()` marked `_started = false`, advanced generation and dropped current single-flight ownership, but `RefreshAsync()` still routed directly to `CheckAsync(false)`. `CheckAsync(...)` started a new `CheckCoreAsync(...)` even while stopped, and `CheckCoreAsync(...)` immediately published `Checking` before any generation/current-lifecycle guard. A stale caller could therefore restart GitHub/network work and leave `LastResult` at a stale Checking state after the updater lifecycle had been stopped.

## Completed changes

- Reservation: `f7cefccf06e5bae85e7d008d37e89fc110039862` — `chore(agent): claim updater stopped-refresh lifecycle`.
- Source fix: `00c7ca0d5a9630d78e1513003c7f5231ce09a749` — `fix(updater): stop manual checks after lifecycle stop`.
  - `CheckAsync(...)` now tests `_started` inside the existing `_sync` critical section before taking generation ownership or launching `CheckCoreAsync(...)`;
  - stopped calls return `Task.FromResult(_last)` and therefore do not create a GitHub request or publish a new Checking result;
  - normal `Start()` behavior is unchanged because Start sets `_started = true` before invoking the automatic check;
  - existing manifest probe, same-generation single-flight reuse, stale-result publication guards and generation-aware scheduling authorization were preserved exactly.
- Regression gate: `d44a56b6669601817fb4584ee609b5b2320e00b8` — `test(updater): guard stopped manual check boundary`.
  - requires the stopped guard/last-result return before generation snapshot, reuse, ownership and `CheckCoreAsync` launch;
  - requires exactly one guarded `CheckCoreAsync(automatic, generation)` launch path inside `CheckAsync`;
  - preserves the restart single-flight and scheduling-lifecycle checks from the two completed predecessor lanes.

## Integration verification

Exact implementation/preflight commit diffs were re-read. Current-main comparison from `d44a56b6...` reported `behind_by: 0` with that commit as merge base. Later concurrent changes touched separate rebar/xref/update-worker preflight surfaces and did not overwrite `UpdateCoordinator.cs` or this lifecycle gate. No reset, rebase or force-push was used.

## Validation boundary

The focused source/static guard is committed, but it was **not executed in a full repository checkout in this connector-only lane**. No GitHub Actions, BricsCAD V25 launch, live GitHub network test, signed update execution, installer, signing or release was dispatched.

The existing signed clean-machine qualification remains `LOCAL-009 / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`; this source change does not manufacture a runtime PASS.

## Coordination / exclusions respected

The active updater worker-readiness claim remains untouched (`SecureUpdateLauncher.cs` and its dedicated readiness gate). No `GitHubReleaseClient.cs`, `UpdateManifestProbe`, Update Center UI, PluginEntry/Ribbon, updater PowerShell/manifest, Quantity/BQ, Workspace, Direct Draw, Core or LOCAL inbox files were edited.

## Completion condition

Satisfied for remote/source scope: after `Stop()`, manual/internal check entry cannot launch new network work or publish Checking; started behavior and existing scheduling/security guards remain intact; focused regression coverage is merged and native signed-update qualification stays local-only.