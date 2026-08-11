# Work claim — updater stopped-refresh lifecycle

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-updater-stopped-refresh`
- Registered: `2026-08-11T22:02:00+07:00`
- Baseline main SHA: `41b30b04c5bed23c163ad643798c21cfe0b58d5f`
- Priority: make `UpdateCoordinator.Stop()` a real lifecycle boundary for manual refresh/check entry points as well as result publication.

## Confirmed defect

`UpdateCoordinator.Stop()` marks `_started = false`, advances generation and drops current single-flight ownership. However, `RefreshAsync()` still routes directly to `CheckAsync(false)`, and `CheckAsync(...)` currently starts a new `CheckCoreAsync(...)` even while `_started == false`. `CheckCoreAsync(...)` immediately calls `Publish(Checking, ...)` before any generation/current-lifecycle guard, so a stale caller can restart network work and mutate `LastResult` to `Checking` after the updater lifecycle was stopped. Completion is suppressed by the later generation guard, leaving the coordinator potentially stuck in a stale Checking state until another valid lifecycle begins.

The scheduling side effect is already generation-gated by the completed scheduling-lifecycle lane; this claim is narrower: stopped coordinator instances must not start or publish a new release check at all.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs`
- `scripts/preflight-update-coordinator-lifecycle.py`
- this claim file

## Intended contract

- `CheckAsync(...)` must return an existing snapshot/result without launching `CheckCoreAsync(...)` when `_started == false`.
- A normal `Start()` still sets `_started = true` before its automatic check, so startup behavior remains unchanged.
- Existing same-generation single-flight reuse, generation-aware scheduling authorization, manifest preflight and stale completion guards remain intact.
- No stopped manual refresh may publish `Checking` or start a new GitHub request.

## Excluded scope

- Active updater worker-readiness claim (`SecureUpdateLauncher.cs` / worker readiness gate) is untouched.
- No `GitHubReleaseClient.cs`, `UpdateManifestProbe`, Update Center UI, PluginEntry/Ribbon, PowerShell updater/manifest, signing, release, Quantity/BQ, Workspace, Direct Draw, Core or LOCAL inbox edits.
- No GitHub Actions dispatch or native/signed updater PASS claim.

## Validation plan

Re-fetch current coordinator and focused lifecycle preflight before writes. Add a stopped-state guard inside the same `_sync` critical section used by single-flight ownership. Extend the static gate to require stopped guard ordering before generation/task launch and reject a generation-blind stopped launch path. Re-read exact diffs and verify ancestry on current `main` without force-push.

## Completion condition

After `Stop()`, `RefreshAsync()`/internal check entry cannot create new network work or publish `Checking`; current started behavior and scheduling guards remain intact; focused source coverage is merged and this claim is completed.