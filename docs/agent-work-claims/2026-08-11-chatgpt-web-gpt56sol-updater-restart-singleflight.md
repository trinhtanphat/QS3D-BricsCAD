# Work claim — updater restart single-flight lifecycle

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-updater-restart-singleflight`
- Registered: `2026-08-11T21:29:00+07:00`
- Completed: `2026-08-11T21:32:00+07:00`
- Baseline main SHA: `d77afad0332ef00d1b3e5d4bf65b18baf9ec4770`
- Priority: keep automatic update discovery reliable when the updater lifecycle is stopped and restarted while an older GitHub check is still in flight.

## Confirmed defect

Before the fix, `UpdateCoordinator.Stop()` advanced `_generation` but left `_inFlight` pointing at the old unfinished task. A subsequent `Start()` advanced generation again and called `CheckAsync(true)`, but `CheckAsync` returned any unfinished `_inFlight` without checking which generation owned it. The restarted coordinator could therefore adopt the stale pre-stop task; when that task completed its generation was no longer current, so it published nothing and the restarted lifecycle never launched its own automatic check.

## Completed changes

- Reservation: `a99d1e4b907f2ae81aebaf464acc7a61e90885e0` — `chore(agent): claim updater restart single-flight`.
- Source fix: `eae4b91afb6e4f4337eccea461d64538140d83de` — `fix(updater): isolate single-flight checks by lifecycle`.
  - added `_inFlightGeneration` ownership;
  - `Stop()` still advances the generation and now releases the old single-flight task reference/owner without cancelling or trusting its eventual result;
  - `CheckAsync(...)` reuses an unfinished task only when `_inFlightGeneration == _generation`;
  - otherwise the current generation owns and launches a fresh check;
  - the pre-existing `IsGenerationCurrent(...)` guard still prevents stale successful/error results from publishing after lifecycle change.
- Regression gate: `b291c23cec325497dfd551bfb5eb3cdec25e5d62` — `test(updater): guard restart generation isolation`.
  - requires Stop generation invalidation and single-flight ownership release;
  - requires generation-aware reuse/order in `CheckAsync`;
  - rejects the old generation-blind `_inFlight` reuse expression;
  - preserves stale-result publish guards.

## Integration verification

The implementation and static gate commit diffs were inspected. Current-main comparison from `b291c23c...` reported `behind_by: 0` with that commit as merge base. Later concurrent work modified `GitHubReleaseClient.cs` and unrelated reporting/quantity surfaces, not `UpdateCoordinator.cs`; no force-push, reset or overwrite was used.

## Validation boundary

The source and focused preflight are committed but the Python preflight was **not executed in this connector-only lane**. No GitHub Actions, local checkout/build, live GitHub network timing test, BricsCAD V25 launch, signed updater execution, installer or release was dispatched.

The existing signed clean-machine update qualification remains `LOCAL-009 / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`; this source fix does not manufacture a native update PASS.

## Coordination / exclusions respected

No release/update PowerShell, manifest schema, `SecureUpdateLauncher.cs`, Update Center UI, Ribbon, PluginEntry, Quantity/BQ, Workspace, Direct Draw, Core or LOCAL inbox files were edited. Concurrent GitHub release-client hardening remained intact.

## Completion condition

Satisfied for remote/source scope: restart cannot reuse an unfinished previous-generation check as the current lifecycle single-flight, stale results remain publish-blocked, focused regression coverage is on `main`, and native signed-update qualification remains local-only.